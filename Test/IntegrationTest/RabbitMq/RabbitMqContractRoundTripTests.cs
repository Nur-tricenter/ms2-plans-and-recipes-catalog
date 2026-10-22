using Nur.Tricenter.Integration.Messages.Catalog;
using Catalog.Contracts.Integration;
using Joseco.Communication.External.Contracts.Services;
using Joseco.Communication.External.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Catalog.Tests.IntegrationTest.RabbitMq
{
    /// <summary>
    /// Proves against a REAL RabbitMQ broker that a
    /// <see cref="MealPlanPublished"/> published through the real
    /// <see cref="IExternalPublisher"/> comes back out of the real consumer with its payload intact.
    /// </summary>
    /// <remarks>
    /// Everything downstream of
    /// <c>PublishAsync</c> (the fanout exchange the consumer declares, the queue binding, the
    /// Newtonsoft serialization of the record, and the deserialization back into
    /// <see cref="IntegrationMessage"/>'s derived contract) is exercised end to end. The other two
    /// RabbitMQ tests prove the mechanics; this one proves OUR contract survives the wire.
    /// <para>
    /// The exchange and queue are suffixed with a fresh <see cref="Guid"/> per run so parallel runs
    /// and leftovers from a previous run can never collide, and both are deleted in
    /// <c>finally</c>.
    /// </para>
    /// </remarks>
    public class RabbitMqContractRoundTripTests
    {
        private static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan TopologyTimeout = TimeSpan.FromSeconds(15);

        [RequiresRabbitMqFact]
        public async Task PublishThenConsume_RealBroker_DeliversTheContractWithItsPayloadIntact()
        {
            // Arrange
            var suffix = Guid.NewGuid().ToString("N");
            var exchangeName = $"catalog.plan-published.{suffix}";
            var queueName = $"catalog.plan-published.{suffix}.queue";

            var settings = RabbitMqTestSettings.Create();
            var consumer = new CapturingMealPlanPublishedConsumer();

            using var host = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddLogging();
                    // Must precede AddRabbitMQ: the library uses TryAddSingleton for the settings.
                    services.AddSingleton(settings);
                    services.AddRabbitMQ(settings);
                    services.AddRabbitMqConsumer<MealPlanPublished, CapturingMealPlanPublishedConsumer>(
                        queueName,
                        declareQueue: true,
                        exchangeName: exchangeName);
                    // MUST come after AddRabbitMqConsumer and MUST use the interface as the service
                    // type. AddRabbitMqConsumer registers `Scoped
                    // IIntegrationMessageConsumer<TMessage> -> THandler`, so registering the concrete
                    // class (or registering before the call) leaves the library resolving its OWN
                    // instance; the test would then hold a consumer the broker never talks to.
                    services.AddSingleton<IIntegrationMessageConsumer<MealPlanPublished>>(consumer);
                })
                .Build();

            await using var admin = await RabbitMqBrokerAdmin.ConnectAsync();

            var published = new MealPlanPublished("correlation-round-trip", "catalog")
            {
                PlanId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                Name = "Healthy Meal Plan 30 Days",
                DurationType = "THIRTY_DAYS",
                TotalDays = 30,
                MealsPerDay = 4
            };

            try
            {
                // Act - start the real consumer and wait until it has declared its topology,
                // otherwise a fanout published before the binding exists is dropped silently.
                await host.StartAsync();

                var declared = await admin.WaitForMessageCountAsync(queueName, expectedMessages: 0, TopologyTimeout);
                Assert.NotNull(declared);

                using var scope = host.Services.CreateScope();
                var publisher = scope.ServiceProvider.GetRequiredService<IExternalPublisher>();
                await publisher.PublishAsync(published, exchangeName, declareDestination: true);

                var received = await ReceiveAsync(consumer, admin, queueName);

                // Assert
                Assert.Equal(published.PlanId, received.PlanId);
                Assert.Equal(published.Name, received.Name);
                Assert.Equal(published.DurationType, received.DurationType);
                Assert.Equal(published.TotalDays, received.TotalDays);
                Assert.Equal(published.MealsPerDay, received.MealsPerDay);
                Assert.Equal(published.Id, received.Id);
                Assert.Equal(published.CorrelationId, received.CorrelationId);
                Assert.Equal(published.Source, received.Source);
                Assert.Equal("MealPlan.Published", received.EventType);
                Assert.Equal(1, consumer.HandledCount);
            }
            finally
            {
                await host.StopAsync();
                await admin.DeleteQueueAsync(queueName);
                await admin.DeleteExchangeAsync(exchangeName);
            }
        }

        /// <summary>
        /// Awaits the consumed contract, and on timeout reports the actual broker state instead of a
        /// bare <see cref="TimeoutException"/>: whether the message is still sitting on the source
        /// queue, or whether the library moved it to <c>{queue}.error</c> because it could not
        /// deserialize it. That distinction is the whole point of this test.
        /// </summary>
        private static async Task<MealPlanPublished> ReceiveAsync(
            CapturingMealPlanPublishedConsumer consumer,
            RabbitMqBrokerAdmin admin,
            string queueName)
        {
            try
            {
                return await consumer.ReceivedAsync.WaitAsync(ReceiveTimeout);
            }
            catch (TimeoutException)
            {
                var source = await admin.DescribeQueueAsync(queueName);
                var deadLetter = await admin.DescribeQueueAsync($"{queueName}.error");

                Assert.Fail(
                    $"The contract was not consumed within {ReceiveTimeout}. " +
                    $"Source: {source}. Dead-letter: {deadLetter}.");
                throw;
            }
        }
    }
}