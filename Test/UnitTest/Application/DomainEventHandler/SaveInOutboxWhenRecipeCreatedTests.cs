using Catalog.Application.UseCase.DomainEventHandler;
using Catalog.Domain.Event.Recipe;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.Contracts.Service;
using Moq;
using Xunit;

namespace Catalog.Tests.Application
{
    public class SaveInOutboxWhenRecipeCreatedTests
    {
        private readonly Mock<IOutboxService<DomainEvent>> _outboxServiceMock;
        private readonly SaveInOutboxWhenRecipeCreated _handler;

        public SaveInOutboxWhenRecipeCreatedTests()
        {
            _outboxServiceMock = new Mock<IOutboxService<DomainEvent>>();
            _handler = new SaveInOutboxWhenRecipeCreated(_outboxServiceMock.Object);
        }

        [Fact]
        public async Task Handle_RecipeCreated_StagesOutboxMessageWithTheEventAsContent()
        {
            // Arrange
            var recipeId = Guid.NewGuid();
            var notification = new RecipeCreated(recipeId, "Ensalada de frutas", "Picar la fruta y mezclar.");
            OutboxMessage<DomainEvent>? staged = null;

            _outboxServiceMock
                .Setup(x => x.AddAsync(It.IsAny<OutboxMessage<DomainEvent>>()))
                .Callback<OutboxMessage<DomainEvent>>(message => staged = message)
                .Returns(Task.CompletedTask);

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            Assert.NotNull(staged);
            Assert.False(staged!.Processed);
            Assert.Same(notification, staged.Content);
            Assert.IsType<RecipeCreated>(staged.Content);
            Assert.Equal(recipeId, Assert.IsType<RecipeCreated>(staged.Content).RecipeId);
        }

        [Fact]
        public async Task Handle_RecipeCreated_CallsOutboxServiceExactlyOnce()
        {
            // Arrange
            var notification = new RecipeCreated(Guid.NewGuid(), "Ensalada de frutas", "Picar la fruta y mezclar.");

            _outboxServiceMock
                .Setup(x => x.AddAsync(It.IsAny<OutboxMessage<DomainEvent>>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _outboxServiceMock.Verify(
                x => x.AddAsync(It.IsAny<OutboxMessage<DomainEvent>>()),
                Times.Once);
        }
    }
}