using Catalog.WorkerService;
using Xunit;

namespace Catalog.Tests.WorkerService
{
    /// <summary>
    /// Guards that <see cref="DefaultQueueRegistrar"/> binds the single catalog queue to EXACTLY
    /// the exchanges listed in the registry. If the two ever drifted apart, either an exchange
    /// would have no queue bound (its messages silently discarded) or a queue binding would point
    /// at an exchange the publishers never publish to (a binding that can never receive anything).
    /// Neither failure is visible at runtime, which is why it is asserted here.
    /// </summary>
    public class DefaultQueueRegistrarTopologyTests
    {
        [Fact]
        public void BoundExchanges_MatchesTheRegistryExactly()
        {
            // Arrange + Act
            var bound = DefaultQueueRegistrar.BoundExchanges;

            // Assert
            Assert.Equal(Catalog.Contracts.Integration.CatalogExchanges.All, bound);
        }

        [Fact]
        public void QueueName_IsTheRegistryDefaultQueue()
        {
            // Arrange + Act
            var queueName = DefaultQueueRegistrar.QueueName;

            // Assert
            Assert.Equal(Catalog.Contracts.Integration.CatalogExchanges.DefaultQueue, queueName);
        }
    }
}