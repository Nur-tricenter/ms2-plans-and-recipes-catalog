using Nur.Tricenter.Integration.Messages.Catalog;
using Catalog.Contracts.Integration;
using Joseco.Communication.External.Contracts.Message;
using Xunit;

namespace Catalog.Tests.Application
{
    public class MealPlanPublishedTests
    {
        [Fact]
        public void Constructor_Default_ExposesTheSharedEnvelopeFromTheBaseRecord()
        {
            // Arrange + Act
            var message = new MealPlanPublished();

            // Assert
            Assert.IsAssignableFrom<IntegrationMessage>(message);
            Assert.NotEqual(Guid.Empty, message.Id);
            Assert.NotEqual(default, message.CreatedAt);
        }

        [Fact]
        public void Constructor_Default_AssignsEveryPayloadMemberASafeDefault()
        {
            // Arrange + Act
            var message = new MealPlanPublished();

            // Assert
            Assert.Equal(Guid.Empty, message.PlanId);
            Assert.Equal(string.Empty, message.Name);
            Assert.Equal(string.Empty, message.DurationType);
            Assert.Equal(0, message.TotalDays);
            Assert.Equal(0, message.MealsPerDay);
        }

        [Fact]
        public void Constructor_WithCorrelationIdAndSource_ForwardsThemToTheSharedEnvelope()
        {
            // Arrange
            const string correlationId = "correlation-abc-123";
            const string source = "catalog";

            // Act
            var message = new MealPlanPublished(correlationId, source);

            // Assert
            Assert.Equal(correlationId, message.CorrelationId);
            Assert.Equal(source, message.Source);
        }

        [Fact]
        public void Constructor_WithCorrelationIdAndSource_StillAssignsSafeDefaults()
        {
            // Arrange + Act
            var message = new MealPlanPublished("correlation-abc-123", "catalog");

            // Assert
            Assert.Equal(Guid.Empty, message.PlanId);
            Assert.Equal(string.Empty, message.Name);
        }

        [Fact]
        public void EventType_ReturnsTheStableWireDiscriminator()
        {
            // Arrange + Act
            var message = new MealPlanPublished();

            // Assert
            Assert.Equal("MealPlan.Published", message.EventType);
        }
    }
}
