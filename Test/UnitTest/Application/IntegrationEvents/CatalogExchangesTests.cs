using Catalog.Contracts.Integration;
using Catalog.Application.UseCase.DomainEventHandler;
using Xunit;

namespace Catalog.Tests.Application
{
    /// <summary>
    /// Guards the single source of truth for the catalog's RabbitMQ topology.
    /// <see cref="CatalogExchanges"/> is what the publishers publish to and what
    /// an exchange that is not in the registry, the message is published to a fanout exchange with
    /// NO queue bound and is silently discarded — no exception, no log entry, just gone. These tests
    /// are the cheapest possible guard against that class of bug.
    /// </summary>
    public class CatalogExchangesTests
    {
        /// <summary>
        /// Every publisher's <c>Destination</c>, paired with the type of contract it publishes, so a
        /// new publisher added without a registry entry fails here instead of losing messages.
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
        public void Contains_EveryPublisherDestination_ReturnsTrue(string destination)
        {
            // Arrange + Act
            var registered = CatalogExchanges.Contains(destination);

            // Assert
            Assert.True(
                registered,
                $"Publisher destination '{destination}' is not in CatalogExchanges.All. " +
                "The published messages would reach a fanout exchange with no bound queue and be " +
                "discarded silently. Add the constant to CatalogExchanges.");
        }

        [Fact]
        public void All_HasNoDuplicates()
        {
            // Arrange
            var all = CatalogExchanges.All;

            // Assert
            Assert.Equal(all.Count, all.Distinct(StringComparer.Ordinal).Count());
        }

        [Fact]
        public void All_CoversEveryDeclaredConstantOfTheCatalog()
        {
            // Arrange
            var declared = typeof(CatalogExchanges)
                .GetFields()
                .Where(field => field.IsLiteral && field.Name != nameof(CatalogExchanges.DefaultQueue))
                .Select(field => (string?)field.GetRawConstantValue())
                .Where(value => !string.IsNullOrEmpty(value))
                .ToArray();

            // Act
            var missing = declared.Where(exchange => !CatalogExchanges.Contains(exchange)).ToArray();

            // Assert
            Assert.Empty(missing);
        }

        [Fact]
        public void All_HasTheExpectedCount()
        {
            // One exchange per published event. Bumping this number is the reminder that a sixth
            // event was added and the registry, the registrar and the tests need to grow with it.
            // Arrange + Act
            var count = CatalogExchanges.All.Count;

            // Assert
            Assert.Equal(5, count);
        }

        [Fact]
        public void All_DoesNotContainTheDefaultQueueName()
        {
            // The queue is bound to the exchanges; it is not one of them. If it ever appears here,
            // the registrar would try to declare a queue as an exchange and fail at boot.
            // Arrange + Act
            var contained = CatalogExchanges.Contains(CatalogExchanges.DefaultQueue);

            // Assert
            Assert.False(contained);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("recipe-publish")]
        [InlineData("unknown-exchange")]
        public void Contains_NonRegistryValue_ReturnsFalse(string? exchange)
        {
            // Arrange + Act
            var contained = CatalogExchanges.Contains(exchange);

            // Assert
            Assert.False(contained);
        }
    }
}
