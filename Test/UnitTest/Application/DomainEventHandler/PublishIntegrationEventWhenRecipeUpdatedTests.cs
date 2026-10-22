using Catalog.Contracts.Integration;
using Catalog.Application.UseCase.DomainEventHandler;
using Catalog.Domain.Event.MealPlan;
using Joseco.Communication.External.Contracts.Services;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

// The domain event (Catalog.Domain.Event.Recipe.RecipeUpdated) and the integration contract
// (Catalog.Contracts.Integration.RecipeUpdated) deliberately share this name. The alias pins
// the wire type under test; the bare name means the domain event.
using IntegrationContract = Catalog.Contracts.Integration.RecipeUpdated;

namespace Catalog.Tests.Application
{
    public class PublishIntegrationEventWhenRecipeUpdatedTests
    {
        private readonly Mock<ILogger<PublishIntegrationEventWhenRecipeUpdated>> _loggerMock;
        private readonly Mock<IExternalPublisher> _publisherMock;
        private readonly PublishIntegrationEventWhenRecipeUpdated _handler;
        private readonly List<IntegrationContract> _published = new();

        public PublishIntegrationEventWhenRecipeUpdatedTests()
        {
            _loggerMock = new Mock<ILogger<PublishIntegrationEventWhenRecipeUpdated>>();
            _publisherMock = new Mock<IExternalPublisher>();
            _publisherMock
                .Setup(x => x.PublishAsync(It.IsAny<IntegrationContract>(), It.IsAny<string?>(), It.IsAny<bool>()))
                .Callback<IntegrationContract, string?, bool>((message, _, _) => _published.Add(message))
                .Returns(Task.CompletedTask);
            _handler = new PublishIntegrationEventWhenRecipeUpdated(
                _loggerMock.Object,
                _publisherMock.Object);
        }

        [Fact]
        public async Task Handle_RecipeUpdatedContent_PublishesTheRecipeUpdatedContract()
        {
            // Arrange
            var notification = new OutboxMessage<Catalog.Domain.Event.Recipe.RecipeUpdated>(BuildRecipeUpdated());

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _publisherMock.Verify(
                x => x.PublishAsync(It.IsAny<IntegrationContract>(), It.IsAny<string?>(), It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_RecipeUpdatedContent_MapsEveryDomainEventMember()
        {
            // Arrange
            var recipeId = Guid.NewGuid();
            var notification = new OutboxMessage<Catalog.Domain.Event.Recipe.RecipeUpdated>(
                new Catalog.Domain.Event.Recipe.RecipeUpdated(recipeId, "Ensalada Caesar", "Romana."));

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            var captured = Assert.Single(_published);
            Assert.Equal(recipeId, captured.RecipeId);
            Assert.Equal("Ensalada Caesar", captured.Name);
            Assert.Equal("Romana.", captured.Instructions);
        }

        [Fact]
        public async Task Handle_RecipeUpdatedContent_PublishesToRecipeUpdatedDestination()
        {
            // Arrange
            var notification = new OutboxMessage<Catalog.Domain.Event.Recipe.RecipeUpdated>(BuildRecipeUpdated());

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _publisherMock.Verify(
                x => x.PublishAsync(It.IsAny<IntegrationContract>(), "recipe-updated", It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_RecipeUpdatedContent_DeclaresTheDestinationExchange()
        {
            // Arrange
            var notification = new OutboxMessage<Catalog.Domain.Event.Recipe.RecipeUpdated>(BuildRecipeUpdated());

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _publisherMock.Verify(
                x => x.PublishAsync(It.IsAny<IntegrationContract>(), It.IsAny<string?>(), true),
                Times.Once);
        }

        [Fact]
        public async Task Handle_RecipeUpdatedContent_PropagatesTheOutboxCorrelationId()
        {
            // Arrange
            const string correlationId = "correlation-abc-123";
            var notification = new OutboxMessage<Catalog.Domain.Event.Recipe.RecipeUpdated>(
                BuildRecipeUpdated(),
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
        public async Task Handle_RecipeUpdatedContent_StampsCatalogAsTheSource()
        {
            // Arrange
            var notification = new OutboxMessage<Catalog.Domain.Event.Recipe.RecipeUpdated>(BuildRecipeUpdated());

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            var captured = Assert.Single(_published);
            Assert.Equal("catalog", captured.Source);
        }

        [Fact]
        public void Handle_IsRegisteredOnlyForRecipeUpdated_AndNotForOtherDomainEvents()
        {
            // RecipeCreated and RecipeUpdated are different facts on different exchanges: publishing
            // the creation message on the update exchange (or vice versa) would deliver a contract
            // whose discriminator does not match what the consumer subscribed to.
            //
            // The guarantee is structural. This handler is registered for
            // INotificationHandler<OutboxMessage<RecipeUpdated>>, so neither a RecipeCreated row nor
            // a MealPlanCreated row is ever dispatched to it. The mismatched calls would not
            // compile, which is exactly the property this test records.
            var handlerContract = typeof(PublishIntegrationEventWhenRecipeUpdated)
                .GetInterfaces()
                .Single();

            Assert.Equal(
                typeof(OutboxMessage<Catalog.Domain.Event.Recipe.RecipeUpdated>),
                handlerContract.GetGenericArguments()[0]);

            Assert.DoesNotContain(
                typeof(PublishIntegrationEventWhenRecipeUpdated).GetInterfaces(),
                i => i.GetGenericArguments()[0] == typeof(OutboxMessage<DomainEvent>));
        }

        [Fact]
        public async Task Handle_RecipeUpdatedContent_LogsThePublishedContract()
        {
            // Arrange
            var recipeId = Guid.NewGuid();
            var notification = new OutboxMessage<Catalog.Domain.Event.Recipe.RecipeUpdated>(
                new Catalog.Domain.Event.Recipe.RecipeUpdated(recipeId, "Ensalada Caesar", "Romana."));

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) =>
                        state.ToString()!.Contains("Recipe.Updated") &&
                        state.ToString()!.Contains(recipeId.ToString())),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private static Catalog.Domain.Event.Recipe.RecipeUpdated BuildRecipeUpdated()
        {
            return new Catalog.Domain.Event.Recipe.RecipeUpdated(
                Guid.NewGuid(),
                "Ensalada Caesar",
                "Romana.");
        }
    }
}
