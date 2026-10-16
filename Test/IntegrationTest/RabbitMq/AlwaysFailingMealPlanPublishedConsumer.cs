using Catalog.Application.IntegrationEvents;
using Joseco.Communication.External.Contracts.Services;

namespace Catalog.Tests.IntegrationTest.RabbitMq
{
    /// <summary>
    /// Consumer that ALWAYS throws, used by the retry / dead-letter test.
    /// </summary>
    /// <remarks>
    /// There is no failure injection switch on purpose: if this consumer ever started succeeding,
    /// the message would be acked and would never reach <c>{queue}.error</c>, and the test would
    /// fail on the dead-letter assertion instead of passing silently.
    /// <para>
    /// <see cref="Attempts"/> counts every invocation so the test can prove the library really called
    /// the handler <c>MaxRetryAttempts</c> times before giving up.
    /// </para>
    /// </remarks>
    public sealed class AlwaysFailingMealPlanPublishedConsumer : IIntegrationMessageConsumer<MealPlanPublished>
    {
        private int _attempts;

        /// <summary>Number of times the library invoked <see cref="HandleAsync"/>.</summary>
        public int Attempts => Volatile.Read(ref _attempts);

        public Task HandleAsync(MealPlanPublished message, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _attempts);
            throw new InvalidOperationException(
                $"Deliberate failure from AlwaysFailingMealPlanPublishedConsumer for plan {message.PlanId}.");
        }
    }
}