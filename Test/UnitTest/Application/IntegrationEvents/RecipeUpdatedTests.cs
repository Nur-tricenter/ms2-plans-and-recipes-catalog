using Nur.Tricenter.Integration.Messages.Catalog;
using Joseco.Communication.External.Contracts.Message;
using Xunit;

namespace Catalog.Tests.Application
{
    public class RecipeUpdatedTests
    {
        [Fact]
        public void Constructor_Default_ExposesTheSharedEnvelopeFromTheBaseRecord()
        {
            // Arrange + Act
            var message = new RecipeUpdated();

            // Assert
            Assert.IsAssignableFrom<IntegrationMessage>(message);
            Assert.NotEqual(Guid.Empty, message.Id);
            Assert.NotEqual(default, message.CreatedAt);
        }

        [Fact]
        public void Constructor_Default_AssignsEveryPayloadMemberASafeDefault()
        {
            // Arrange + Act
            var message = new RecipeUpdated();

            // Assert
            Assert.Equal(Guid.Empty, message.RecipeId);
            Assert.Equal(string.Empty, message.Name);
            Assert.Equal(string.Empty, message.Instructions);
        }

        [Fact]
        public void Constructor_WithCorrelationIdAndSource_ForwardsThemToTheSharedEnvelope()
        {
            // Arrange
            const string correlationId = "correlation-abc-123";
            const string source = "catalog";

            // Act
            var message = new RecipeUpdated(correlationId, source);

            // Assert
            Assert.Equal(correlationId, message.CorrelationId);
            Assert.Equal(source, message.Source);
        }

        [Fact]
        public void Constructor_WithCorrelationIdAndSource_StillAssignsSafeDefaults()
        {
            // Arrange + Act
            var message = new RecipeUpdated("correlation-abc-123", "catalog");

            // Assert
            Assert.Equal(Guid.Empty, message.RecipeId);
            Assert.Equal(string.Empty, message.Name);
            Assert.Equal(string.Empty, message.Instructions);
        }

        [Fact]
        public void EventType_ReturnsTheStableWireDiscriminator()
        {
            // Arrange + Act
            var message = new RecipeUpdated();

            // Assert
            Assert.Equal("Recipe.Updated", message.EventType);
        }

        [Fact]
        public void EventType_IsDistinctFromTheRecipePublishedDiscriminator()
        {
            // Arrange
            var updated = new RecipeUpdated();
            var published = new RecipePublished();

            // Act
            var eventTypes = new[] { published.EventType, updated.EventType };

            // Assert
            Assert.Equal(eventTypes.Length, eventTypes.Distinct(StringComparer.Ordinal).Count());
        }
    }
}