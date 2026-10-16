using Catalog.Domain.Event.Recipe;
using Catalog.Infrastructure.Persistence.StoredModel;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.Recipe;
using Catalog.Infrastructure.Persistence.StoredModel.Projection;
using Joseco.Outbox.Contracts.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Catalog.Tests.Infrastructure.Projection
{
    public class ProjectIngredientModelWhenIngredientRemovedFromRecipeTests
    {
        private readonly Mock<ILogger<ProjectIngredientModelWhenIngredientRemovedFromRecipe>> _loggerMock;

        public ProjectIngredientModelWhenIngredientRemovedFromRecipeTests()
        {
            _loggerMock = new Mock<ILogger<ProjectIngredientModelWhenIngredientRemovedFromRecipe>>();
        }

        [Fact]
        public async Task Handle_WhenIngredientIsProjected_RemovesIt()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var recipeId = Guid.NewGuid();
            var foodId = Guid.NewGuid();
            await SeedRecipeWithIngredientAsync(dbContext, recipeId, foodId);
            var _sut = new ProjectIngredientModelWhenIngredientRemovedFromRecipe(dbContext, _loggerMock.Object);

            // Act
            await _sut.Handle(new OutboxMessage<IngredientRemovedFromRecipe>(
                new IngredientRemovedFromRecipe(recipeId, foodId, 150m)));

            // Assert
            var stored = await dbContext.Recipes.Include(x => x.Ingredients).SingleAsync();
            Assert.Empty(stored.Ingredients);
        }

        [Fact]
        public async Task Handle_WhenIngredientIsRemoved_KeepsTheParentRecipe()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var recipeId = Guid.NewGuid();
            var foodId = Guid.NewGuid();
            await SeedRecipeWithIngredientAsync(dbContext, recipeId, foodId);
            var _sut = new ProjectIngredientModelWhenIngredientRemovedFromRecipe(dbContext, _loggerMock.Object);

            // Act
            await _sut.Handle(new OutboxMessage<IngredientRemovedFromRecipe>(
                new IngredientRemovedFromRecipe(recipeId, foodId, 150m)));

            // Assert
            Assert.Equal(1, await dbContext.Recipes.CountAsync());
        }

        [Fact]
        public async Task Handle_WhenIngredientIsRemoved_LeavesTheOtherIngredientsInPlace()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var recipeId = Guid.NewGuid();
            var removedFoodId = Guid.NewGuid();
            var keptFoodId = Guid.NewGuid();
            var recipe = NewRecipe(recipeId);
            recipe.Ingredients.Add(new IngredientModel
            {
                FoodId = removedFoodId,
                PortionQuantity = 100m,
                RecipeId = recipeId
            });
            recipe.Ingredients.Add(new IngredientModel
            {
                FoodId = keptFoodId,
                PortionQuantity = 50m,
                RecipeId = recipeId
            });
            dbContext.Recipes.Add(recipe);
            await dbContext.SaveChangesAsync();
            var _sut = new ProjectIngredientModelWhenIngredientRemovedFromRecipe(dbContext, _loggerMock.Object);

            // Act
            await _sut.Handle(new OutboxMessage<IngredientRemovedFromRecipe>(
                new IngredientRemovedFromRecipe(recipeId, removedFoodId, 100m)));

            // Assert
            var stored = await dbContext.Recipes.Include(x => x.Ingredients).SingleAsync();
            Assert.Equal(keptFoodId, Assert.Single(stored.Ingredients).FoodId);
        }

        [Fact]
        public async Task Handle_WhenSameMessageIsProjectedTwice_KeepsTheParentRecipe()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var recipeId = Guid.NewGuid();
            var foodId = Guid.NewGuid();
            await SeedRecipeWithIngredientAsync(dbContext, recipeId, foodId);
            var _sut = new ProjectIngredientModelWhenIngredientRemovedFromRecipe(dbContext, _loggerMock.Object);
            var ingredientRemoved = new IngredientRemovedFromRecipe(recipeId, foodId, 150m);

            // Act
            await _sut.Handle(new OutboxMessage<IngredientRemovedFromRecipe>(ingredientRemoved));
            await _sut.Handle(new OutboxMessage<IngredientRemovedFromRecipe>(ingredientRemoved));

            // Assert
            Assert.Equal(1, await dbContext.Recipes.CountAsync());
        }

        [Fact]
        public async Task Handle_WhenRecipeIsNotProjected_DoesNotThrow()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var _sut = new ProjectIngredientModelWhenIngredientRemovedFromRecipe(dbContext, _loggerMock.Object);

            // Act
            var act = () => _sut.Handle(new OutboxMessage<IngredientRemovedFromRecipe>(
                new IngredientRemovedFromRecipe(Guid.NewGuid(), Guid.NewGuid(), 100m)));

            // Assert
            await act();
        }

        private static async Task SeedRecipeWithIngredientAsync(
            StoredDbContext dbContext, Guid recipeId, Guid foodId)
        {
            var recipe = NewRecipe(recipeId);
            recipe.Ingredients.Add(new IngredientModel
            {
                FoodId = foodId,
                PortionQuantity = 150m,
                RecipeId = recipeId
            });
            dbContext.Recipes.Add(recipe);
            await dbContext.SaveChangesAsync();
        }

        private static RecipeModel NewRecipe(Guid recipeId)
        {
            return new RecipeModel
            {
                Id = recipeId,
                Name = "Ensalada de frutas",
                Instructions = "Picar y mezclar.",
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
