using Nur.Tricenter.Integration.Messages.Catalog;
using Catalog.Contracts.Integration;
using Joseco.Communication.External.Contracts.Message;
using Xunit;

namespace Catalog.Tests.Application
{
    public class RecipePublishedTests
    {
        [Fact]
        public void Constructor_Default_ExposesTheSharedEnvelopeFromTheBaseRecord()
        {
            // Arrange + Act
            var message = new RecipePublished();

            // Assert
            Assert.IsAssignableFrom<IntegrationMessage>(message);
            Assert.NotEqual(Guid.Empty, message.Id);
            Assert.NotEqual(default, message.CreatedAt);
        }

        [Fact]
        public void Constructor_Default_AssignsEveryPayloadMemberASafeDefault()
        {
            // Arrange + Act
            var message = new RecipePublished();

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
            var message = new RecipePublished(correlationId, source);

            // Assert
            Assert.Equal(correlationId, message.CorrelationId);
            Assert.Equal(source, message.Source);
        }

        [Fact]
        public void Constructor_WithCorrelationIdAndSource_StillAssignsSafeDefaults()
        {
            // Arrange + Act
            var message = new RecipePublished("correlation-abc-123", "catalog");

            // Assert
            Assert.Equal(Guid.Empty, message.RecipeId);
            Assert.Equal(string.Empty, message.Name);
            Assert.Equal(string.Empty, message.Instructions);
        }

        [Fact]
        public void EventType_ReturnsTheStableWireDiscriminator()
        {
            // Arrange + Act
            var message = new RecipePublished();

            // Assert
            Assert.Equal("Recipe.Published", message.EventType);
        }

        [Fact]
        public void Members_CarryIdentityAndInstructionsOnly_BecauseIngredientsAreAddedLater()
        {
            // This is the documented limitation of the contract made executable: RecipeCreated fires
            // before any ingredient exists, so there is no ingredient member to assert on. If a
            // future change adds one, this test must be revisited together with the domain model â€”
            // see the remarks on RecipePublished.
            // Arrange + Act
            var properties = typeof(RecipePublished)
                .GetProperties()
                .Select(property => property.Name)
                .Where(name => name != nameof(IntegrationMessage.Id))
                .Where(name => name != nameof(IntegrationMessage.CreatedAt))
                .Where(name => name != nameof(IntegrationMessage.CorrelationId))
                .Where(name => name != nameof(IntegrationMessage.Source))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            // Assert
            Assert.Equal(
                new[] { "EventType", "Instructions", "Name", "RecipeId" },
                properties);
        }
    }
}