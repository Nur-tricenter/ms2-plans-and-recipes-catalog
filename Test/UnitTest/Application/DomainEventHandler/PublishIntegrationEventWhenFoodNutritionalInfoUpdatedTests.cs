using Catalog.Application.UseCase.DomainEventHandler;
using Catalog.Domain.Event.Food;
using Catalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Catalog.Tests.Application
{
    public class PublishIntegrationEventWhenFoodNutritionalInfoUpdatedTests
    {
        private readonly Mock<ILogger<PublishIntegrationEventWhenFoodNutritionalInfoUpdated>> _loggerMock;
        private readonly PublishIntegrationEventWhenFoodNutritionalInfoUpdated _handler;

        public PublishIntegrationEventWhenFoodNutritionalInfoUpdatedTests()
        {
            _loggerMock = new Mock<ILogger<PublishIntegrationEventWhenFoodNutritionalInfoUpdated>>();
            _handler = new PublishIntegrationEventWhenFoodNutritionalInfoUpdated(_loggerMock.Object);
        }

        [Fact]
        public async Task Handle_FoodNutritionalInfoUpdated_LogsPublishedIntegrationEvent()
        {
            // Arrange
            var foodId = Guid.NewGuid();
            var previous = new NutritionalInfo(100, 52, 0.3m, 14, 0.2m);
            var newInfo = new NutritionalInfo(100, 60, 0.5m, 16, 0.3m);
            var notification = new FoodNutritionalInfoUpdated(foodId, previous, newInfo);

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) =>
                        state.ToString()!.Contains("CatalogoV1.FoodNutritionalInfoUpdated") &&
                        state.ToString()!.Contains(foodId.ToString())),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}