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

namespace Catalog.Tests.Application
{
    public class PublishIntegrationEventWhenMealPlanCreatedTests
    {
        private readonly Mock<ILogger<PublishIntegrationEventWhenMealPlanCreated>> _loggerMock;
        private readonly Mock<IExternalPublisher> _publisherMock;
        private readonly PublishIntegrationEventWhenMealPlanCreated _handler;
        private readonly List<MealPlanPublished> _published = new();

        public PublishIntegrationEventWhenMealPlanCreatedTests()
        {
            _loggerMock = new Mock<ILogger<PublishIntegrationEventWhenMealPlanCreated>>();
            _publisherMock = new Mock<IExternalPublisher>();
            _publisherMock
                .Setup(x => x.PublishAsync(It.IsAny<MealPlanPublished>(), It.IsAny<string?>(), It.IsAny<bool>()))
                .Callback<MealPlanPublished, string?, bool>((message, _, _) => _published.Add(message))
                .Returns(Task.CompletedTask);
            _handler = new PublishIntegrationEventWhenMealPlanCreated(
                _loggerMock.Object,
                _publisherMock.Object);
        }

        [Fact]
        public async Task Handle_MealPlanCreatedContent_PublishesThePlanPublishedContract()
        {
            // Arrange
            var notification = new OutboxMessage<Catalog.Domain.Event.MealPlan.MealPlanCreated>(
                new MealPlanCreated(Guid.NewGuid(), "Plan Saludable", "FIFTEEN_DAYS", 15, 3, Guid.NewGuid()));

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _publisherMock.Verify(
                x => x.PublishAsync(It.IsAny<MealPlanPublished>(), It.IsAny<string?>(), It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_MealPlanCreatedContent_MapsEveryDomainEventMember()
        {
            // Arrange
            var planId = Guid.NewGuid();
            var mealPlanCreated = new MealPlanCreated(planId, "Plan Saludable", "FIFTEEN_DAYS", 15, 3, Guid.NewGuid());
            var notification = new OutboxMessage<Catalog.Domain.Event.MealPlan.MealPlanCreated>(mealPlanCreated);

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            var captured = Assert.Single(_published);
            Assert.Equal(planId, captured.PlanId);
            Assert.Equal("Plan Saludable", captured.Name);
            Assert.Equal("FIFTEEN_DAYS", captured.DurationType);
            Assert.Equal(15, captured.TotalDays);
            Assert.Equal(3, captured.MealsPerDay);
        }

        [Fact]
        public async Task Handle_MealPlanCreatedContent_PublishesToMealPlanPublishedDestination()
        {
            // Arrange
            var notification = new OutboxMessage<Catalog.Domain.Event.MealPlan.MealPlanCreated>(
                new MealPlanCreated(Guid.NewGuid(), "Plan Saludable", "FIFTEEN_DAYS", 15, 3, Guid.NewGuid()));

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _publisherMock.Verify(
                x => x.PublishAsync(It.IsAny<MealPlanPublished>(), "meal-plan-published", It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_MealPlanCreatedContent_DeclaresTheDestinationExchange()
        {
            // Arrange
            var notification = new OutboxMessage<Catalog.Domain.Event.MealPlan.MealPlanCreated>(
                new MealPlanCreated(Guid.NewGuid(), "Plan Saludable", "FIFTEEN_DAYS", 15, 3, Guid.NewGuid()));

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _publisherMock.Verify(
                x => x.PublishAsync(It.IsAny<MealPlanPublished>(), It.IsAny<string?>(), true),
                Times.Once);
        }

        [Fact]
        public async Task Handle_MealPlanCreatedContent_PropagatesTheOutboxCorrelationId()
        {
            // Arrange
            const string correlationId = "correlation-abc-123";
            var notification = new OutboxMessage<Catalog.Domain.Event.MealPlan.MealPlanCreated>(
                new MealPlanCreated(Guid.NewGuid(), "Plan Saludable", "FIFTEEN_DAYS", 15, 3, Guid.NewGuid()),
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
        public async Task Handle_MealPlanCreatedContent_StampsCatalogAsTheSource()
        {
            // Arrange
            var notification = new OutboxMessage<Catalog.Domain.Event.MealPlan.MealPlanCreated>(
                new MealPlanCreated(Guid.NewGuid(), "Plan Saludable", "FIFTEEN_DAYS", 15, 3, Guid.NewGuid()));

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            var captured = Assert.Single(_published);
            Assert.Equal("catalog", captured.Source);
        }

        [Fact]
        public void Handle_IsRegisteredOnlyForMealPlanCreated_AndNotForOtherDomainEvents()
        {
            // The guarantee is structural: this handler is registered for
            // INotificationHandler<OutboxMessage<MealPlanCreated>>, so a FoodCreated outbox row is
            // never dispatched to it by MediatR. The mismatched call would not compile, which is
            // exactly the property this test records.
            var handlerContract = typeof(PublishIntegrationEventWhenMealPlanCreated)
                .GetInterfaces()
                .Single();

            Assert.Equal(
                typeof(OutboxMessage<Catalog.Domain.Event.MealPlan.MealPlanCreated>),
                handlerContract.GetGenericArguments()[0]);

            Assert.DoesNotContain(
                typeof(PublishIntegrationEventWhenMealPlanCreated).GetInterfaces(),
                i => i.GetGenericArguments()[0] == typeof(OutboxMessage<DomainEvent>));
        }

        [Fact]
        public async Task Handle_MealPlanCreatedContent_LogsThePublishedContract()
        {
            // Arrange
            var planId = Guid.NewGuid();
            var notification = new OutboxMessage<Catalog.Domain.Event.MealPlan.MealPlanCreated>(
                new MealPlanCreated(planId, "Plan Saludable", "FIFTEEN_DAYS", 15, 3, Guid.NewGuid()));

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) =>
                        state.ToString()!.Contains("MealPlan.Published") &&
                        state.ToString()!.Contains(planId.ToString()) &&
                        state.ToString()!.Contains("Plan Saludable")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
