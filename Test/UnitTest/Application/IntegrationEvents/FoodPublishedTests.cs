using Catalog.Application.IntegrationEvents;
using Joseco.Communication.External.Contracts.Message;
using Xunit;

namespace Catalog.Tests.Application
{
    public class FoodPublishedTests
    {
        [Fact]
        public void Constructor_Default_ExposesTheSharedEnvelopeFromTheBaseRecord()
        {
            // Arrange + Act
            var message = new FoodPublished();

            // Assert
            Assert.IsAssignableFrom<IntegrationMessage>(message);
            Assert.NotEqual(Guid.Empty, message.Id);
            Assert.NotEqual(default, message.CreatedAt);
        }

        [Fact]
        public void Constructor_Default_AssignsEveryPayloadMemberASafeDefault()
        {
            // Arrange + Act
            var message = new FoodPublished();

            // Assert
            Assert.Equal(Guid.Empty, message.FoodId);
            Assert.Equal(string.Empty, message.Name);
            Assert.Equal(string.Empty, message.Category);
            Assert.Equal(0, message.Quantity);
            Assert.Equal(0, message.Calories);
            Assert.Equal(0, message.Protein);
            Assert.Equal(0, message.Fat);
            Assert.Equal(0, message.Carbs);
            Assert.Equal(string.Empty, message.UnitOfMeasure);
        }

        [Fact]
        public void Constructor_WithCorrelationIdAndSource_ForwardsThemToTheSharedEnvelope()
        {
            // Arrange
            const string correlationId = "correlation-abc-123";
            const string source = "catalog";

            // Act
            var message = new FoodPublished(correlationId, source);

            // Assert
            Assert.Equal(correlationId, message.CorrelationId);
            Assert.Equal(source, message.Source);
        }

        [Fact]
        public void Constructor_WithCorrelationIdAndSource_StillAssignsSafeDefaults()
        {
            // Arrange + Act
            var message = new FoodPublished("correlation-abc-123", "catalog");

            // Assert
            Assert.Equal(Guid.Empty, message.FoodId);
            Assert.Equal(string.Empty, message.Name);
            Assert.Equal(string.Empty, message.Category);
            Assert.Equal(string.Empty, message.UnitOfMeasure);
        }

        [Fact]
        public void EventType_ReturnsTheStableWireDiscriminator()
        {
            // Arrange + Act
            var message = new FoodPublished();

            // Assert
            Assert.Equal("Food.Published", message.EventType);
        }

        [Fact]
        public void EventType_IsDistinctFromTheFoodNutritionalInfoUpdatedDiscriminator()
        {
            // Arrange
            var published = new FoodPublished();
            var updated = new FoodNutritionalInfoUpdated();

            // Act
            var eventTypes = new[] { published.EventType, updated.EventType };

            // Assert
            Assert.Equal(eventTypes.Length, eventTypes.Distinct(StringComparer.Ordinal).Count());
        }
    }
}