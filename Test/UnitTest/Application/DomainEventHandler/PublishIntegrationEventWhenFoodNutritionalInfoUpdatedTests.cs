using Nur.Tricenter.Integration.Messages.Catalog;
using Catalog.Contracts.Integration;
using Catalog.Application.UseCase.DomainEventHandler;
using Catalog.Domain.Event.Food;
using Catalog.Domain.Event.MealPlan;
using Catalog.Domain.ValueObjects;
using Joseco.Communication.External.Contracts.Services;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

// The domain event (Catalog.Domain.Event.Food.FoodNutritionalInfoUpdated) and the integration
// contract (Nur.Tricenter.Integration.Messages.Catalog.FoodNutritionalInfoUpdated) deliberately share
// this name. The alias pins the wire type under test; the bare name means the domain event.
using IntegrationContract = Nur.Tricenter.Integration.Messages.Catalog.FoodNutritionalInfoUpdated;

namespace Catalog.Tests.Application
{
    public class PublishIntegrationEventWhenFoodNutritionalInfoUpdatedTests
    {
        private readonly Mock<ILogger<PublishIntegrationEventWhenFoodNutritionalInfoUpdated>> _loggerMock;
        private readonly Mock<IExternalPublisher> _publisherMock;
        private readonly PublishIntegrationEventWhenFoodNutritionalInfoUpdated _handler;
        private readonly List<IntegrationContract> _published = new();

        public PublishIntegrationEventWhenFoodNutritionalInfoUpdatedTests()
        {
            _loggerMock = new Mock<ILogger<PublishIntegrationEventWhenFoodNutritionalInfoUpdated>>();
            _publisherMock = new Mock<IExternalPublisher>();
            _publisherMock
                .Setup(x => x.PublishAsync(It.IsAny<IntegrationContract>(), It.IsAny<string?>(), It.IsAny<bool>()))
                .Callback<IntegrationContract, string?, bool>((message, _, _) => _published.Add(message))
                .Returns(Task.CompletedTask);
            _handler = new PublishIntegrationEventWhenFoodNutritionalInfoUpdated(
                _loggerMock.Object,
                _publisherMock.Object);
        }

        [Fact]
        public async Task Handle_FoodNutritionalInfoUpdatedContent_PublishesTheAlimentoContract()
        {
            // Arrange
            var notification = new OutboxMessage<Catalog.Domain.Event.Food.FoodNutritionalInfoUpdated>(BuildFoodNutritionalInfoUpdated());

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _publisherMock.Verify(
                x => x.PublishAsync(
                    It.IsAny<IntegrationContract>(),
                    It.IsAny<string?>(),
                    It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_FoodNutritionalInfoUpdatedContent_MapsEveryDomainEventMember()
        {
            // Arrange
            var foodId = Guid.NewGuid();
            var previous = new NutritionalInfo(100, 52, 0.3m, 14, 0.2m);
            var newInfo = new NutritionalInfo(100, 60, 0.5m, 16, 0.3m);
            var notification = new OutboxMessage<Catalog.Domain.Event.Food.FoodNutritionalInfoUpdated>(
                new Catalog.Domain.Event.Food.FoodNutritionalInfoUpdated(foodId, previous, newInfo));

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            var captured = Assert.Single(_published);
            Assert.Equal(foodId, captured.FoodId);
            Assert.Equal(52d, captured.PreviousCalories);
            Assert.Equal(0.3d, captured.PreviousProtein);
            Assert.Equal(0.2d, captured.PreviousFat);
            Assert.Equal(14d, captured.PreviousCarbs);
            Assert.Equal(60d, captured.NewCalories);
            Assert.Equal(0.5d, captured.NewProtein);
            Assert.Equal(0.3d, captured.NewFat);
            Assert.Equal(16d, captured.NewCarbs);
        }

        [Fact]
        public async Task Handle_FoodNutritionalInfoUpdatedContent_PublishesToFoodNutritionalInfoUpdatedDestination()
        {
            // Arrange
            var notification = new OutboxMessage<Catalog.Domain.Event.Food.FoodNutritionalInfoUpdated>(BuildFoodNutritionalInfoUpdated());

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _publisherMock.Verify(
                x => x.PublishAsync(
                    It.IsAny<IntegrationContract>(),
                    "food-nutritional-info-updated",
                    It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_FoodNutritionalInfoUpdatedContent_DeclaresTheDestinationExchange()
        {
            // Arrange
            var notification = new OutboxMessage<Catalog.Domain.Event.Food.FoodNutritionalInfoUpdated>(BuildFoodNutritionalInfoUpdated());

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _publisherMock.Verify(
                x => x.PublishAsync(
                    It.IsAny<IntegrationContract>(),
                    It.IsAny<string?>(),
                    true),
                Times.Once);
        }

        [Fact]
        public async Task Handle_FoodNutritionalInfoUpdatedContent_PropagatesTheOutboxCorrelationId()
        {
            // Arrange
            const string correlationId = "correlation-abc-123";
            var notification = new OutboxMessage<Catalog.Domain.Event.Food.FoodNutritionalInfoUpdated>(
                BuildFoodNutritionalInfoUpdated(),
                correlationId,
                "trace-1",
                "span-1");

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            var captured = Assert.Single(_published);
            Assert.Equal(correlationId, captured.CorrelationId);
        }

        [Fact]
        public async Task Handle_FoodNutritionalInfoUpdatedContent_StampsCatalogAsTheSource()
        {
            // Arrange
            var notification = new OutboxMessage<Catalog.Domain.Event.Food.FoodNutritionalInfoUpdated>(BuildFoodNutritionalInfoUpdated());

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            var captured = Assert.Single(_published);
            Assert.Equal("catalog", captured.Source);
        }

        [Fact]
        public void Handle_IsRegisteredOnlyForFoodNutritionalInfoUpdated_AndNotForOtherDomainEvents()
        {
            // The guarantee is structural: this handler is registered for
            // INotificationHandler<OutboxMessage<FoodNutritionalInfoUpdated>>, so a
            // OutboxMessage<DomainEvent> carrying a MealPlanCreated is never dispatched to it by
            // MediatR. There is nothing to invoke here — the mismatched call would not compile,
            // which is exactly the property this test records.
            var handlerContract = typeof(PublishIntegrationEventWhenFoodNutritionalInfoUpdated)
                .GetInterfaces()
                .Single();

            Assert.Equal(
                typeof(OutboxMessage<Catalog.Domain.Event.Food.FoodNutritionalInfoUpdated>),
                handlerContract.GetGenericArguments()[0]);

            Assert.DoesNotContain(
                typeof(PublishIntegrationEventWhenFoodNutritionalInfoUpdated).GetInterfaces(),
                i => i.GetGenericArguments()[0] == typeof(OutboxMessage<DomainEvent>));
        }

        [Fact]
        public async Task Handle_FoodNutritionalInfoUpdatedContent_LogsThePublishedContract()
        {
            // Arrange
            var foodId = Guid.NewGuid();
            var previous = new NutritionalInfo(100, 52, 0.3m, 14, 0.2m);
            var newInfo = new NutritionalInfo(100, 60, 0.5m, 16, 0.3m);
            var notification = new OutboxMessage<Catalog.Domain.Event.Food.FoodNutritionalInfoUpdated>(
                new Catalog.Domain.Event.Food.FoodNutritionalInfoUpdated(foodId, previous, newInfo));

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) =>
                        state.ToString()!.Contains("Food.NutritionalInfoUpdated") &&
                        state.ToString()!.Contains(foodId.ToString())),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private static Catalog.Domain.Event.Food.FoodNutritionalInfoUpdated BuildFoodNutritionalInfoUpdated()
        {
            return new Catalog.Domain.Event.Food.FoodNutritionalInfoUpdated(
                Guid.NewGuid(),
                new NutritionalInfo(100, 52, 0.3m, 14, 0.2m),
                new NutritionalInfo(100, 60, 0.5m, 16, 0.3m));
        }
    }
}
