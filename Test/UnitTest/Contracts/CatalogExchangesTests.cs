using Catalog.Application.UseCase.DomainEventHandler;
using Catalog.WorkerService;
using Xunit;

namespace Catalog.Tests.Application
{
    /// <summary>
    /// Guards the catalog's RabbitMQ topology.
    /// </summary>
    /// <remarks>
    /// This used to check the publishers against a <c>CatalogExchanges</c> registry. That class is
    /// gone: the reference architecture publishes with the destination as a literal at the point of
    /// use, and keeping a constants class created a third copy of a fact the broker already reads
    /// from two places — the definitions file and <c>DefaultQueueRegistrar.BoundExchanges</c>.
    ///
    /// The guarantee is kept, because it is worth having: an exchange that is not bound to a queue
    /// receives a published message and discards it. No exception, no log line, nothing. So a typo
    /// in a destination fails only as silence, which is exactly the class of bug that is cheapest
    /// to catch here and most expensive to find in production.
    /// </remarks>
    public class CatalogExchangesTests
    {
        /// <summary>
        /// Every publisher's <c>Destination</c>, so one added without a matching exchange fails
        /// here instead of losing its messages.
        /// </summary>
        public static TheoryData<string> PublisherDestinations =>
            new()
            {
                PublishIntegrationEventWhenMealPlanCreated.Destination,
                PublishIntegrationEventWhenFoodNutritionalInfoUpdated.Destination,
                PublishIntegrationEventWhenRecipeCreated.Destination,
                PublishIntegrationEventWhenRecipeUpdated.Destination,
                PublishIntegrationEventWhenFoodCreated.Destination
            };

        [Theory]
        [MemberData(nameof(PublisherDestinations))]
        public void EveryPublisherDestinationIsBoundByTheDefaultQueue(string destination)
        {
            Assert.Contains(
                destination,
                DefaultQueueRegistrar.BoundExchanges);
        }

        [Fact]
        public void BoundExchangesHaveNoDuplicates()
        {
            var all = DefaultQueueRegistrar.BoundExchanges;

            Assert.Equal(all.Count, all.Distinct(StringComparer.Ordinal).Count());
        }

        [Fact]
        public void EveryPublisherHasItsOwnExchange()
        {
            // One exchange per published fact. Sharing one would make a consumer of one message type
            // receive — and fail to deserialise — the others.
            var destinations = new[]
            {
                PublishIntegrationEventWhenMealPlanCreated.Destination,
                PublishIntegrationEventWhenFoodNutritionalInfoUpdated.Destination,
                PublishIntegrationEventWhenRecipeCreated.Destination,
                PublishIntegrationEventWhenRecipeUpdated.Destination,
                PublishIntegrationEventWhenFoodCreated.Destination
            };

            Assert.Equal(destinations.Length, destinations.Distinct(StringComparer.Ordinal).Count());
        }

        [Fact]
        public void BoundExchangesCoverEveryPublisher()
        {
            // The reverse direction: an exchange nobody publishes to is harmless but dead, and one
            // publisher missing from the registrar loses every message it sends.
            var publishers = new[]
            {
                PublishIntegrationEventWhenMealPlanCreated.Destination,
                PublishIntegrationEventWhenFoodNutritionalInfoUpdated.Destination,
                PublishIntegrationEventWhenRecipeCreated.Destination,
                PublishIntegrationEventWhenRecipeUpdated.Destination,
                PublishIntegrationEventWhenFoodCreated.Destination
            };

            foreach (var destination in publishers)
            {
                Assert.Contains(destination, DefaultQueueRegistrar.BoundExchanges);
            }

            Assert.Equal(publishers.Length, DefaultQueueRegistrar.BoundExchanges.Count);
        }
    }
}