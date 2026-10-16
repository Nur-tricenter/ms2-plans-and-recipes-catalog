using Catalog.Application.IntegrationEvents;
using Joseco.Communication.External.Contracts.Message;
using Xunit;

namespace Catalog.Tests.Application
{
    public class FoodNutritionalInfoUpdatedTests
    {
        [Fact]
        public void Constructor_Default_ExposesTheSharedEnvelopeFromTheBaseRecord()
        {
            // Arrange + Act
            var message = new FoodNutritionalInfoUpdated();

            // Assert
            Assert.IsAssignableFrom<IntegrationMessage>(message);
            Assert.NotEqual(Guid.Empty, message.Id);
            Assert.NotEqual(default, message.CreatedAt);
        }

        [Fact]
        public void Constructor_Default_AssignsEveryPayloadMemberASafeDefault()
        {
            // Arrange + Act
            var message = new FoodNutritionalInfoUpdated();

            // Assert
            Assert.Equal(Guid.Empty, message.FoodId);
            Assert.Equal(0d, message.PreviousCalories);
            Assert.Equal(0d, message.PreviousProtein);
            Assert.Equal(0d, message.PreviousFat);
            Assert.Equal(0d, message.PreviousCarbs);
            Assert.Equal(0d, message.NewCalories);
            Assert.Equal(0d, message.NewProtein);
            Assert.Equal(0d, message.NewFat);
            Assert.Equal(0d, message.NewCarbs);
        }

        [Fact]
        public void Constructor_WithCorrelationIdAndSource_ForwardsThemToTheSharedEnvelope()
        {
            // Arrange
            const string correlationId = "correlation-abc-123";
            const string source = "catalog";

            // Act
            var message = new FoodNutritionalInfoUpdated(correlationId, source);

            // Assert
            Assert.Equal(correlationId, message.CorrelationId);
            Assert.Equal(source, message.Source);
        }

        [Fact]
        public void Constructor_WithCorrelationIdAndSource_StillAssignsSafeDefaults()
        {
            // Arrange + Act
            var message = new FoodNutritionalInfoUpdated("correlation-abc-123", "catalog");

            // Assert
            Assert.Equal(Guid.Empty, message.FoodId);
            Assert.Equal(0d, message.NewCalories);
        }

        [Fact]
        public void EventType_ReturnsTheStableWireDiscriminator()
        {
            // Arrange + Act
            var message = new FoodNutritionalInfoUpdated();

            // Assert
            Assert.Equal("Food.NutritionalInfoUpdated", message.EventType);
        }
    }
}
