using Catalog.Domain.Event.Recipe;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.Recipe;
using Catalog.Infrastructure.Persistence.StoredModel.Projection;
using Joseco.Outbox.Contracts.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Catalog.Tests.Infrastructure.Projection
{
    public class ProjectRecipeModelWhenRecipeUpdatedTests
    {
        private readonly Mock<ILogger<ProjectRecipeModelWhenRecipeUpdated>> _loggerMock;

        public ProjectRecipeModelWhenRecipeUpdatedTests()
        {
            _loggerMock = new Mock<ILogger<ProjectRecipeModelWhenRecipeUpdated>>();
        }

        [Fact]
        public async Task Handle_WhenRecipeExists_UpdatesTheExistingModel()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var recipeId = Guid.NewGuid();
            dbContext.Recipes.Add(new RecipeModel
            {
                Id = recipeId,
                Name = "Ensalada de frutas",
                Instructions = "Picar y mezclar.",
                CreatedAt = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();
            var _sut = new ProjectRecipeModelWhenRecipeUpdated(dbContext, _loggerMock.Object);

            // Act
            await _sut.Handle(new OutboxMessage<RecipeUpdated>(
                new RecipeUpdated(recipeId, "Ensalada de verduras", "Lavar y aliñar.")));

            // Assert
            dbContext.ChangeTracker.Clear();
            var stored = await dbContext.Recipes.SingleAsync();
            Assert.Equal("Ensalada de verduras", stored.Name);
            Assert.Equal("Lavar y aliñar.", stored.Instructions);
        }

        [Fact]
        public async Task Handle_WhenSameMessageIsProjectedTwice_KeepsOneRecipeModel()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var recipeId = Guid.NewGuid();
            dbContext.Recipes.Add(new RecipeModel
            {
                Id = recipeId,
                Name = "Ensalada de frutas",
                Instructions = "Picar y mezclar.",
                CreatedAt = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();
            var _sut = new ProjectRecipeModelWhenRecipeUpdated(dbContext, _loggerMock.Object);
            var recipeUpdated = new RecipeUpdated(recipeId, "Ensalada de verduras", "Lavar y aliñar.");

            // Act
            await _sut.Handle(new OutboxMessage<RecipeUpdated>(recipeUpdated));
            await _sut.Handle(new OutboxMessage<RecipeUpdated>(recipeUpdated));

            // Assert
            Assert.Equal(1, await dbContext.Recipes.CountAsync());
        }

        [Fact]
        public async Task Handle_WhenRecipeDoesNotExist_DoesNotInsertARecipeModel()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var _sut = new ProjectRecipeModelWhenRecipeUpdated(dbContext, _loggerMock.Object);

            // Act
            await _sut.Handle(new OutboxMessage<RecipeUpdated>(
                new RecipeUpdated(Guid.NewGuid(), "Ensalada", "Picar.")));

            // Assert
            Assert.Equal(0, await dbContext.Recipes.CountAsync());
        }
    }
}
