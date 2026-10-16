using Catalog.Domain.Event.Recipe;
using Catalog.Infrastructure.Persistence.StoredModel;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.Recipe;
using Catalog.Infrastructure.Persistence.StoredModel.Projection;
using Joseco.Outbox.Contracts.Model;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Catalog.Tests.Infrastructure.Projection
{
    public class ProjectRecipeModelWhenRecipeCreatedTests
    {
        [Fact]
        public async Task Handle_WhenRecipeDoesNotExist_InsertsRecipeModel()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var _sut = new ProjectRecipeModelWhenRecipeCreated(dbContext);
            var recipeCreated = new RecipeCreated(Guid.NewGuid(), "Ensalada de frutas", "Picar la fruta y mezclar.");

            // Act
            await _sut.Handle(new OutboxMessage<RecipeCreated>(recipeCreated));

            // Assert
            var stored = await dbContext.Recipes.SingleAsync();
            Assert.Equal(recipeCreated.RecipeId, stored.Id);
            Assert.Equal("Ensalada de frutas", stored.Name);
        }

        [Fact]
        public async Task Handle_WhenSameMessageIsProjectedTwice_KeepsOneRecipeModel()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var _sut = new ProjectRecipeModelWhenRecipeCreated(dbContext);
            var recipeCreated = new RecipeCreated(Guid.NewGuid(), "Ensalada de frutas", "Picar la fruta y mezclar.");

            // Act
            await _sut.Handle(new OutboxMessage<RecipeCreated>(recipeCreated));
            await _sut.Handle(new OutboxMessage<RecipeCreated>(recipeCreated));

            // Assert
            Assert.Equal(1, await dbContext.Recipes.CountAsync());
        }

        [Fact]
        public async Task Handle_WhenSameMessageIsProjectedTwice_RefreshesTheExistingModel()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var _sut = new ProjectRecipeModelWhenRecipeCreated(dbContext);
            var recipeCreated = new RecipeCreated(Guid.NewGuid(), "Ensalada de frutas", "Picar la fruta y mezclar.");
            await _sut.Handle(new OutboxMessage<RecipeCreated>(recipeCreated));

            // Act
            await _sut.Handle(new OutboxMessage<RecipeCreated>(recipeCreated));

            // Assert
            dbContext.ChangeTracker.Clear();
            var stored = await dbContext.Recipes.SingleAsync();
            Assert.Equal("Ensalada de frutas", stored.Name);
            Assert.NotNull(stored.UpdatedAt);
        }

        [Fact]
        public async Task Handle_WhenRecipeCreatedAgain_RenamesTheExistingModel()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var _sut = new ProjectRecipeModelWhenRecipeCreated(dbContext);
            var recipeId = Guid.NewGuid();
            await _sut.Handle(new OutboxMessage<RecipeCreated>(new RecipeCreated(recipeId, "Ensalada de frutas", "Picar la fruta y mezclar.")));

            // Act
            await _sut.Handle(new OutboxMessage<RecipeCreated>(new RecipeCreated(recipeId, "Ensalada de verduras", "Cortar y aliñar las verduras.")));

            // Assert
            Assert.Equal("Ensalada de verduras", (await dbContext.Recipes.SingleAsync()).Name);
        }

        [Fact]
        public async Task Handle_WhenRecipeCreated_StoresTheInstructionsFromTheEvent()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var _sut = new ProjectRecipeModelWhenRecipeCreated(dbContext);

            // Act
            await _sut.Handle(new OutboxMessage<RecipeCreated>(
                new RecipeCreated(Guid.NewGuid(), "Arroz con pollo", "Cocinar el arroz y agregar el pollo.")));

            // Assert
            Assert.Equal("Cocinar el arroz y agregar el pollo.", (await dbContext.Recipes.SingleAsync()).Instructions);
        }

        [Fact]
        public async Task Handle_WhenRecipeWasPreviouslyInserted_LeavesItsIngredientsUntouched()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var recipeId = Guid.NewGuid();
            var recipe = new RecipeModel
            {
                Id = recipeId,
                Name = "Old name",
                Instructions = "Old instructions",
                CreatedAt = DateTime.UtcNow
            };
            recipe.Ingredients.Add(new IngredientModel
            {
                FoodId = Guid.NewGuid(),
                PortionQuantity = 100m,
                RecipeId = recipeId
            });
            dbContext.Recipes.Add(recipe);
            await dbContext.SaveChangesAsync();

            // Act
            await new ProjectRecipeModelWhenRecipeCreated(dbContext)
                .Handle(new OutboxMessage<RecipeCreated>(new RecipeCreated(recipeId, "Ensalada de frutas", "Picar la fruta y mezclar.")));

            // Assert
            var stored = await dbContext.Recipes.Include(x => x.Ingredients).SingleAsync();
            Assert.Equal("Ensalada de frutas", stored.Name);
            Assert.Equal("Old instructions", stored.Instructions);
            Assert.Single(stored.Ingredients);
        }
    }
}
