using Nur.Tricenter.Integration.Messages.Catalog;
using System.Globalization;
using System.Text;
using Joseco.Communication.External.Contracts.Services;
using Joseco.Communication.External.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Xunit;

namespace Catalog.Tests.IntegrationTest.RabbitMq
{
    /// <summary>
    /// Proves against a REAL RabbitMQ broker that a handler which always throws is retried exactly
    /// <c>MaxRetryAttempts</c> times and then dead-lettered to <c>{queue}.error</c> with the failure
    /// headers the rest of the system relies on to triage poison messages.
    /// </summary>
    /// <remarks>
    /// <c>MaxRetryAttempts</c> is a TOTAL attempt count, not a retry count, so
    /// <see cref="RabbitMqTestSettings.Create"/> is called with 3 and
    /// <c>RetryBaseDelayMilliseconds = 300</c> to keep the exponential back-off (300 ms + 600 ms)
    /// short. The contract asserted here is: the source queue is drained (the failed delivery is
    /// ACKed, not left re-queueing forever), the error queue holds EXACTLY one message, and that
    /// message carries <c>x-attempts</c>, a non-empty <c>x-failure-reason</c> and the originating
    /// queue name, while its body is still the original, deserializable contract.
    /// </remarks>
    public class RabbitMqRetryAndDeadLetterTests
    {
        private const int MaxRetryAttempts = 3;
        private const int RetryBaseDelayMilliseconds = 300;

        private static readonly TimeSpan DeadLetterTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan TopologyTimeout = TimeSpan.FromSeconds(15);

        [RequiresRabbitMqFact]
        public async Task FailingHandler_RealBroker_RetriesThenDeadLettersTheOriginalMessage()
        {
            // Arrange
            var suffix = Guid.NewGuid().ToString("N");
            var exchangeName = $"catalog.plan-published.dlq.{suffix}";
            var queueName = $"catalog.plan-published.dlq.{suffix}.queue";
            var errorQueueName = $"{queueName}.error";

            var settings = RabbitMqTestSettings.Create(MaxRetryAttempts, RetryBaseDelayMilliseconds);
            var failingConsumer = new AlwaysFailingMealPlanPublishedConsumer();

            using var host = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddLogging();
                    // Must precede AddRabbitMQ: the library uses TryAddSingleton for the settings.
                    services.AddSingleton(settings);
                    services.AddRabbitMQ(settings);
                    services.AddRabbitMqConsumer<MealPlanPublished, AlwaysFailingMealPlanPublishedConsumer>(
                        queueName,
                        declareQueue: true,
                        exchangeName: exchangeName);
                    // MUST come after AddRabbitMqConsumer and MUST use the interface as the service
                    // type. AddRabbitMqConsumer registers `Scoped
                    // IIntegrationMessageConsumer<TMessage> -> THandler`, so registering the concrete
                    // class (or registering before the call) leaves the library resolving its OWN
                    // instance; the test would then count invocations on an object the broker never
                    // calls.
                    services.AddSingleton<IIntegrationMessageConsumer<MealPlanPublished>>(failingConsumer);
                })
                .Build();

            await using var admin = await RabbitMqBrokerAdmin.ConnectAsync();

            var published = new MealPlanPublished("correlation-dead-letter", "catalog")
            {
                PlanId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa"),
                Name = "Meal Plan That Always Fails",
                DurationType = "FIFTEEN_DAYS",
                TotalDays = 15,
                MealsPerDay = 3
            };

            try
            {
                // Act
                await host.StartAsync();

                var declared = await admin.WaitForMessageCountAsync(queueName, expectedMessages: 0, TopologyTimeout);
                Assert.NotNull(declared);

                using var scope = host.Services.CreateScope();
                var publisher = scope.ServiceProvider.GetRequiredService<IExternalPublisher>();
                await publisher.PublishAsync(published, exchangeName, declareDestination: true);

                (uint? sourceCount, uint? errorCount) = await WaitForDeadLetterAsync(
                    admin, queueName, errorQueueName);

                // Assert - the failed delivery was ACKed, so the source queue no longer holds it.
                Assert.True(
                    sourceCount == 0,
                    $"Source queue '{queueName}' still reports {sourceCount?.ToString() ?? "no such queue"} " +
                    "message(s): the failed delivery was never dead-lettered and acked.");

                // Assert - exactly one dead-lettered message landed on {queue}.error.
                Assert.True(
                    errorCount == 1,
                    $"Expected exactly 1 dead-lettered message on '{errorQueueName}', found " +
                    $"{errorCount?.ToString() ?? "no such queue"}.");

                var deadLetter = await admin.TakeAsync(errorQueueName);
                Assert.NotNull(deadLetter);

                var headers = deadLetter.BasicProperties.Headers
                    ?? new Dictionary<string, object?>();

                // Full dump of what the broker actually stored, so any header assertion below
                // explains itself with the raw wire values instead of only the expected value.
                var headerDump = string.Join(" | ", headers
                    .Select(pair => $"{pair.Key}='{AmqpHeaders.AsText(pair.Value)}' ({pair.Value?.GetType().Name ?? "null"})"));

                Assert.True(
                    headers.TryGetValue(AmqpHeaders.OriginalQueue, out var originalQueue),
                    $"Dead-letter headers are missing 'x-original-queue'. Present: {headerDump}.");
                Assert.Equal(queueName, AmqpHeaders.AsText(originalQueue));

                Assert.True(
                    headers.TryGetValue(AmqpHeaders.Attempts, out var attempts),
                    $"Dead-letter headers are missing 'x-attempts'. Present: {headerDump}.");
                Assert.True(
                    Convert.ToInt32(attempts, CultureInfo.InvariantCulture) == MaxRetryAttempts,
                    $"Expected dead-letter header 'x-attempts' == {MaxRetryAttempts}, " +
                    $"found '{AmqpHeaders.AsText(attempts)}'. The handler was invoked " +
                    $"{failingConsumer.Attempts} time(s). Headers: {headerDump}.");

                Assert.True(
                    headers.TryGetValue(AmqpHeaders.FailureReason, out var failureReason),
                    $"Dead-letter headers are missing 'x-failure-reason'. Present: {headerDump}.");
                Assert.False(
                    string.IsNullOrWhiteSpace(AmqpHeaders.AsText(failureReason)),
                    $"Dead-letter header 'x-failure-reason' is empty. Headers: {headerDump}.");

                // Assert - the body is still OUR contract, not a re-wrapped or corrupted payload.
                var json = Encoding.UTF8.GetString(deadLetter.Body.ToArray());
                var roundTripped = JsonConvert.DeserializeObject<MealPlanPublished>(json);

                Assert.NotNull(roundTripped);
                Assert.Equal(published.Id, roundTripped.Id);
                Assert.Equal(published.PlanId, roundTripped.PlanId);
                Assert.Equal(published.Name, roundTripped.Name);
                Assert.Equal(published.DurationType, roundTripped.DurationType);
                Assert.Equal(published.TotalDays, roundTripped.TotalDays);
                Assert.Equal(published.MealsPerDay, roundTripped.MealsPerDay);
                Assert.Equal(published.CorrelationId, roundTripped.CorrelationId);

                // Assert - the handler really ran MaxRetryAttempts times before giving up.
                Assert.Equal(MaxRetryAttempts, failingConsumer.Attempts);
            }
            finally
            {
                await host.StopAsync();
                await admin.DeleteQueueAsync(errorQueueName);
                await admin.DeleteQueueAsync(queueName);
                await admin.DeleteExchangeAsync(exchangeName);
            }
        }

        /// <summary>
        /// Polls until BOTH the source queue has been drained AND the dead-letter queue holds exactly
        /// one message. The library re-publishes to <c>{queue}.error</c> BEFORE acking the original
        /// delivery, so a drained source queue is a valid precondition for reading the dead letter.
        /// </summary>
        private static async Task<(uint? SourceCount, uint? ErrorCount)> WaitForDeadLetterAsync(
            RabbitMqBrokerAdmin admin,
            string queueName,
            string errorQueueName)
        {
            var deadline = DateTimeOffset.UtcNow + DeadLetterTimeout;
            uint? sourceCount = null;
            uint? errorCount = null;

            do
            {
                sourceCount = await admin.TryGetMessageCountAsync(queueName);
                errorCount = await admin.TryGetMessageCountAsync(errorQueueName);

                if (sourceCount == 0 && errorCount == 1)
                {
                    return (sourceCount, errorCount);
                }

                await Task.Delay(250);
            }
            while (DateTimeOffset.UtcNow < deadline);

            return (sourceCount, errorCount);
        }
    }
}