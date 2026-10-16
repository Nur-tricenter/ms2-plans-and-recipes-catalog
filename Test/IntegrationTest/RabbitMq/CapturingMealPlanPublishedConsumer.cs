using Catalog.Application.IntegrationEvents;
using Joseco.Communication.External.Contracts.Services;

namespace Catalog.Tests.IntegrationTest.RabbitMq
{
    /// <summary>
    /// Consumer used by the round-trip test: it captures the first deserialized contract it is
    /// handed and hands it back to the test through a <see cref="TaskCompletionSource{TResult}"/>.
    /// </summary>
    /// <remarks>
    /// Registered as a singleton so the same instance is resolved for every delivery the library
    /// hands it, and so the test never has to guess when the message arrived: it awaits the task,
    /// which fails fast on timeout instead of sleeping for a guessed interval.
    /// </remarks>
    public sealed class CapturingMealPlanPublishedConsumer : IIntegrationMessageConsumer<MealPlanPublished>
    {
        private readonly TaskCompletionSource<MealPlanPublished> _received =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _handledCount;

        /// <summary>Number of contracts this consumer has been asked to handle.</summary>
        public int HandledCount => Volatile.Read(ref _handledCount);

        /// <summary>Completes with the first contract received.</summary>
        public Task<MealPlanPublished> ReceivedAsync => _received.Task;

        public Task HandleAsync(MealPlanPublished message, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _handledCount);
            _received.TrySetResult(message);
            return Task.CompletedTask;
        }
    }
}