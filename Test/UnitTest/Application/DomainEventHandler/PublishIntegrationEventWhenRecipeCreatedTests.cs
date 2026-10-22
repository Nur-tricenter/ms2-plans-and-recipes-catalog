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
    public class PublishIntegrationEventWhenRecipeCreatedTests
    {
        private readonly Mock<ILogger<PublishIntegrationEventWhenRecipeCreated>> _loggerMock;
        private readonly Mock<IExternalPublisher> _publisherMock;
        private readonly PublishIntegrationEventWhenRecipeCreated _handler;
        private readonly List<RecipePublished> _published = new();

        public PublishIntegrationEventWhenRecipeCreatedTests()
        {
            _loggerMock = new Mock<ILogger<PublishIntegrationEventWhenRecipeCreated>>();
            _publisherMock = new Mock<IExternalPublisher>();
            _publisherMock
                .Setup(x => x.PublishAsync(It.IsAny<RecipePublished>(), It.IsAny<string?>(), It.IsAny<bool>()))
                .Callback<RecipePublished, string?, bool>((message, _, _) => _published.Add(message))
                .Returns(Task.CompletedTask);
            _handler = new PublishIntegrationEventWhenRecipeCreated(
                _loggerMock.Object,
                _publisherMock.Object);
        }

        [Fact]
        public async Task Handle_RecipeCreatedContent_PublishesTheRecipePublishedContract()
        {
            // Arrange
            var notification = new OutboxMessage<Catalog.Domain.Event.Recipe.RecipeCreated>(
                new Catalog.Domain.Event.Recipe.RecipeCreated(Guid.NewGuid(), "Ensalada", "Picar y mezclar."));

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _publisherMock.Verify(
                x => x.PublishAsync(It.IsAny<RecipePublished>(), It.IsAny<string?>(), It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_RecipeCreatedContent_MapsEveryDomainEventMember()
        {
            // Arrange
            var recipeId = Guid.NewGuid();
            var notification = new OutboxMessage<Catalog.Domain.Event.Recipe.RecipeCreated>(
                new Catalog.Domain.Event.Recipe.RecipeCreated(recipeId, "Ensalada", "Picar y mezclar."));

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            var captured = Assert.Single(_published);
            Assert.Equal(recipeId, captured.RecipeId);
            Assert.Equal("Ensalada", captured.Name);
            Assert.Equal("Picar y mezclar.", captured.Instructions);
        }

        [Fact]
        public async Task Handle_RecipeCreatedContent_PublishesToRecipePublishedDestination()
        {
            // Arrange
            var notification = new OutboxMessage<Catalog.Domain.Event.Recipe.RecipeCreated>(
                new Catalog.Domain.Event.Recipe.RecipeCreated(Guid.NewGuid(), "Ensalada", "Picar y mezclar."));

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _publisherMock.Verify(
                x => x.PublishAsync(It.IsAny<RecipePublished>(), "recipe-published", It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_RecipeCreatedContent_DeclaresTheDestinationExchange()
        {
            // Arrange
            var notification = new OutboxMessage<Catalog.Domain.Event.Recipe.RecipeCreated>(
                new Catalog.Domain.Event.Recipe.RecipeCreated(Guid.NewGuid(), "Ensalada", "Picar y mezclar."));

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _publisherMock.Verify(
                x => x.PublishAsync(It.IsAny<RecipePublished>(), It.IsAny<string?>(), true),
                Times.Once);
        }

        [Fact]
        public async Task Handle_RecipeCreatedContent_PropagatesTheOutboxCorrelationId()
        {
            // Arrange
            const string correlationId = "correlation-abc-123";
            var notification = new OutboxMessage<Catalog.Domain.Event.Recipe.RecipeCreated>(
                new Catalog.Domain.Event.Recipe.RecipeCreated(Guid.NewGuid(), "Ensalada", "Picar y mezclar."),
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
        public async Task Handle_RecipeCreatedContent_StampsCatalogAsTheSource()
        {
            // Arrange
            var notification = new OutboxMessage<Catalog.Domain.Event.Recipe.RecipeCreated>(
                new Catalog.Domain.Event.Recipe.RecipeCreated(Guid.NewGuid(), "Ensalada", "Picar y mezclar."));

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            var captured = Assert.Single(_published);
            Assert.Equal("catalog", captured.Source);
        }

        [Fact]
        public void Handle_IsRegisteredOnlyForRecipeCreated_AndNotForOtherDomainEvents()
        {
            // The guarantee is structural: this handler is registered for
            // INotificationHandler<OutboxMessage<RecipeCreated>>, so a MealPlanCreated outbox row is
            // never dispatched to it by MediatR. The mismatched call would not compile, which is
            // exactly the property this test records.
            var handlerContract = typeof(PublishIntegrationEventWhenRecipeCreated)
                .GetInterfaces()
                .Single();

            Assert.Equal(
                typeof(OutboxMessage<Catalog.Domain.Event.Recipe.RecipeCreated>),
                handlerContract.GetGenericArguments()[0]);

            Assert.DoesNotContain(
                typeof(PublishIntegrationEventWhenRecipeCreated).GetInterfaces(),
                i => i.GetGenericArguments()[0] == typeof(OutboxMessage<DomainEvent>));
        }

        [Fact]
        public async Task Handle_RecipeCreatedContent_LogsThePublishedContract()
        {
            // Arrange
            var recipeId = Guid.NewGuid();
            var notification = new OutboxMessage<Catalog.Domain.Event.Recipe.RecipeCreated>(
                new Catalog.Domain.Event.Recipe.RecipeCreated(recipeId, "Ensalada", "Picar y mezclar."));

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) =>
                        state.ToString()!.Contains("Recipe.Published") &&
                        state.ToString()!.Contains(recipeId.ToString()) &&
                        state.ToString()!.Contains("Ensalada")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_RecipeCreatedContent_CarriesNoIngredientData()
        {
            // The documented limitation, made executable: the domain event has no ingredients at
            // creation time, so neither does the contract.
            // Arrange
            var notification = new OutboxMessage<Catalog.Domain.Event.Recipe.RecipeCreated>(
                new Catalog.Domain.Event.Recipe.RecipeCreated(Guid.NewGuid(), "Ensalada", "Picar y mezclar."));

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            var captured = Assert.Single(_published);
            Assert.DoesNotContain(
                captured.GetType().GetProperties(),
                property => property.Name.Contains("Ingredient", StringComparison.OrdinalIgnoreCase));
        }
    }
}
