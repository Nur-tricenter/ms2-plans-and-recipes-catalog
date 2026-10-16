using Catalog.Application.UseCase.DomainEventHandler;
using Catalog.Domain.Event.MealPlan;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Catalog.Tests.Application
{
    public class PublishIntegrationEventWhenMealPlanCreatedTests
    {
        private readonly Mock<ILogger<PublishIntegrationEventWhenMealPlanCreated>> _loggerMock;
        private readonly PublishIntegrationEventWhenMealPlanCreated _handler;

        public PublishIntegrationEventWhenMealPlanCreatedTests()
        {
            _loggerMock = new Mock<ILogger<PublishIntegrationEventWhenMealPlanCreated>>();
            _handler = new PublishIntegrationEventWhenMealPlanCreated(_loggerMock.Object);
        }

        [Fact]
        public async Task Handle_MealPlanCreated_LogsPublishedIntegrationEvent()
        {
            // Arrange
            var planId = Guid.NewGuid();
            var notification = new MealPlanCreated(planId, "Plan Saludable", "FIFTEEN_DAYS", 15, 3);

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) =>
                        state.ToString()!.Contains("CatalogoV1.PlanPublished") &&
                        state.ToString()!.Contains(planId.ToString()) &&
                        state.ToString()!.Contains("Plan Saludable")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}