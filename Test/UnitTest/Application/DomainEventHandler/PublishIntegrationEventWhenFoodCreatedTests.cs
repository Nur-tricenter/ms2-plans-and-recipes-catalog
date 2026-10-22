using Nur.Tricenter.Integration.Messages.Catalog;
using Catalog.Application.UseCase.DomainEventHandler;
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
    public class PublishIntegrationEventWhenFoodCreatedTests
    {
        private readonly Mock<ILogger<PublishIntegrationEventWhenFoodCreated>> _loggerMock;
        private readonly Mock<IExternalPublisher> _publisherMock;
        private readonly PublishIntegrationEventWhenFoodCreated _handler;
        private readonly List<FoodPublished> _published = new();

        public PublishIntegrationEventWhenFoodCreatedTests()
        {
            _loggerMock = new Mock<ILogger<PublishIntegrationEventWhenFoodCreated>>();
            _publisherMock = new Mock<IExternalPublisher>();
            _publisherMock
                .Setup(x => x.PublishAsync(It.IsAny<FoodPublished>(), It.IsAny<string?>(), It.IsAny<bool>()))
                .Callback<FoodPublished, string?, bool>((message, _, _) => _published.Add(message))
                .Returns(Task.CompletedTask);
            _handler = new PublishIntegrationEventWhenFoodCreated(
                _loggerMock.Object,
                _publisherMock.Object);
        }

        [Fact]
        public async Task Handle_FoodCreatedContent_PublishesTheFoodPublishedContract()
        {
            // Arrange
            var notification = new OutboxMessage<Catalog.Domain.Event.Food.FoodCreated>(BuildFoodCreated());

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _publisherMock.Verify(
                x => x.PublishAsync(It.IsAny<FoodPublished>(), It.IsAny<string?>(), It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_FoodCreatedContent_MapsEveryDomainEventMember()
        {
            // Arrange
            var foodId = Guid.NewGuid();
            var notification = new OutboxMessage<Catalog.Domain.Event.Food.FoodCreated>(
                new Catalog.Domain.Event.Food.FoodCreated(
                    foodId,
                    "MANZANA",
                    "FRUTAS",
                    new NutritionalInfo(100, 52, 0.3m, 14, 0.2m),
                    UnitOfMeasure.Gram));

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            var captured = Assert.Single(_published);
            Assert.Equal(foodId, captured.FoodId);
            Assert.Equal("MANZANA", captured.Name);
            Assert.Equal("FRUTAS", captured.Category);
            Assert.Equal(100d, captured.Quantity);
            Assert.Equal(52d, captured.Calories);
            Assert.Equal(0.3d, captured.Protein);
            Assert.Equal(0.2d, captured.Fat);
            Assert.Equal(14d, captured.Carbs);
        }

        [Theory]
        [InlineData(UnitOfMeasure.Gram, "Gram")]
        [InlineData(UnitOfMeasure.Kilogram, "Kilogram")]
        [InlineData(UnitOfMeasure.Milliliter, "Milliliter")]
        [InlineData(UnitOfMeasure.Liter, "Liter")]
        [InlineData(UnitOfMeasure.Unit, "Unit")]
        [InlineData(UnitOfMeasure.Cup, "Cup")]
        [InlineData(UnitOfMeasure.Tablespoon, "Tablespoon")]
        [InlineData(UnitOfMeasure.Teaspoon, "Teaspoon")]
        public async Task Handle_FoodCreatedContent_PublishesTheUnitOfMeasureByName(
            UnitOfMeasure unitOfMeasure,
            string expected)
        {
            // The enum NAME travels, not the numeric value: renumbering the domain enum must not
            // silently change the meaning of already-published events.
            // Arrange
            var notification = new OutboxMessage<Catalog.Domain.Event.Food.FoodCreated>(
                new Catalog.Domain.Event.Food.FoodCreated(
                    Guid.NewGuid(),
                    "MANZANA",
                    "FRUTAS",
                    new NutritionalInfo(100, 52, 0.3m, 14, 0.2m),
                    unitOfMeasure));

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            var captured = Assert.Single(_published);
            Assert.Equal(expected, captured.UnitOfMeasure);
        }

        [Fact]
        public async Task Handle_FoodCreatedContent_PublishesToFoodPublishedDestination()
        {
            // Arrange
            var notification = new OutboxMessage<Catalog.Domain.Event.Food.FoodCreated>(BuildFoodCreated());

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _publisherMock.Verify(
                x => x.PublishAsync(It.IsAny<FoodPublished>(), "food-published", It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_FoodCreatedContent_DeclaresTheDestinationExchange()
        {
            // Arrange
            var notification = new OutboxMessage<Catalog.Domain.Event.Food.FoodCreated>(BuildFoodCreated());

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _publisherMock.Verify(
                x => x.PublishAsync(It.IsAny<FoodPublished>(), It.IsAny<string?>(), true),
                Times.Once);
        }

        [Fact]
        public async Task Handle_FoodCreatedContent_PropagatesTheOutboxCorrelationId()
        {
            // Arrange
            const string correlationId = "correlation-abc-123";
            var notification = new OutboxMessage<Catalog.Domain.Event.Food.FoodCreated>(
                BuildFoodCreated(),
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
        public async Task Handle_FoodCreatedContent_StampsCatalogAsTheSource()
        {
            // Arrange
            var notification = new OutboxMessage<Catalog.Domain.Event.Food.FoodCreated>(BuildFoodCreated());

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            var captured = Assert.Single(_published);
            Assert.Equal("catalog", captured.Source);
        }

        [Fact]
        public void Handle_IsRegisteredOnlyForFoodCreated_AndNotForOtherDomainEvents()
        {
            // A MediatR notification handler is selected by the notification type, so the guarantee
            // that a FoodCreated outbox row never reaches this handler is structural rather than a
            // runtime check: `INotificationHandler<OutboxMessage<FoodCreated>>` cannot be
            // satisfied by an `OutboxMessage<MealPlanCreated>`.
            //
            // This asserts that structural fact. Constructing the mismatched message and passing it
            // to Handle would not compile, which is the point.
            var handlerContract = typeof(PublishIntegrationEventWhenFoodCreated)
                .GetInterfaces()
                .Single();

            Assert.Equal(
                typeof(OutboxMessage<Catalog.Domain.Event.Food.FoodCreated>),
                handlerContract.GetGenericArguments()[0]);

            Assert.DoesNotContain(
                typeof(PublishIntegrationEventWhenFoodCreated).GetInterfaces(),
                i => i.GetGenericArguments()[0] == typeof(OutboxMessage<DomainEvent>));
        }

        [Fact]
        public async Task Handle_FoodCreatedContent_LogsThePublishedContract()
        {
            // Arrange
            var foodId = Guid.NewGuid();
            var notification = new OutboxMessage<Catalog.Domain.Event.Food.FoodCreated>(
                new Catalog.Domain.Event.Food.FoodCreated(
                    foodId,
                    "MANZANA",
                    "FRUTAS",
                    new NutritionalInfo(100, 52, 0.3m, 14, 0.2m),
                    UnitOfMeasure.Gram));

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) =>
                        state.ToString()!.Contains("Food.Published") &&
                        state.ToString()!.Contains(foodId.ToString()) &&
                        state.ToString()!.Contains("MANZANA")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private static Catalog.Domain.Event.Food.FoodCreated BuildFoodCreated()
        {
            return new Catalog.Domain.Event.Food.FoodCreated(
                Guid.NewGuid(),
                "MANZANA",
                "FRUTAS",
                new NutritionalInfo(100, 52, 0.3m, 14, 0.2m),
                UnitOfMeasure.Gram);
        }
    }
}
