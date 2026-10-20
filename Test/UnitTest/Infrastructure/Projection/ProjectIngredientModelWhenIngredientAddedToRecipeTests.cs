using Catalog.Domain.Event.Recipe;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.Recipe;
using Catalog.Infrastructure.Persistence.StoredModel.Projection;
using Joseco.Outbox.Contracts.Model;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Catalog.Tests.Infrastructure.Projection
{
    public class ProjectIngredientModelWhenIngredientAddedToRecipeTests
    {
        [Fact]
        public async Task Handle_WhenIngredientIsNotProjectedYet_AddsItToTheRecipe()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var recipeId = Guid.NewGuid();
            var foodId = Guid.NewGuid();
            dbContext.Recipes.Add(NewRecipe(recipeId));
            await dbContext.SaveChangesAsync();
            var _sut = new ProjectIngredientModelWhenIngredientAddedToRecipe(dbContext);

            // Act
            await _sut.Handle(new OutboxMessage<IngredientAddedToRecipe>(
                new IngredientAddedToRecipe(recipeId, foodId, 150m)));

            // Assert
            var stored = await dbContext.Recipes.Include(x => x.Ingredients).SingleAsync();
            var ingredient = Assert.Single(stored.Ingredients);
            Assert.Equal(foodId, ingredient.FoodId);
            Assert.Equal(150m, ingredient.PortionQuantity);
        }

        [Fact]
        public async Task Handle_WhenSameMessageIsProjectedTwice_KeepsOneIngredient()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var recipeId = Guid.NewGuid();
            var foodId = Guid.NewGuid();
            dbContext.Recipes.Add(NewRecipe(recipeId));
            await dbContext.SaveChangesAsync();
            var _sut = new ProjectIngredientModelWhenIngredientAddedToRecipe(dbContext);
            var ingredientAdded = new IngredientAddedToRecipe(recipeId, foodId, 150m);

            // Act
            await _sut.Handle(new OutboxMessage<IngredientAddedToRecipe>(ingredientAdded));
            await _sut.Handle(new OutboxMessage<IngredientAddedToRecipe>(ingredientAdded));

            // Assert
            var stored = await dbContext.Recipes.Include(x => x.Ingredients).SingleAsync();
            Assert.Single(stored.Ingredients);
        }

        [Fact]
        public async Task Handle_WhenSameMessageIsProjectedTwice_RefreshesThePortionQuantity()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var recipeId = Guid.NewGuid();
            var foodId = Guid.NewGuid();
            dbContext.Recipes.Add(NewRecipe(recipeId));
            await dbContext.SaveChangesAsync();
            var _sut = new ProjectIngredientModelWhenIngredientAddedToRecipe(dbContext);
            await _sut.Handle(new OutboxMessage<IngredientAddedToRecipe>(
                new IngredientAddedToRecipe(recipeId, foodId, 150m)));

            // Act
            await _sut.Handle(new OutboxMessage<IngredientAddedToRecipe>(
                new IngredientAddedToRecipe(recipeId, foodId, 200m)));

            // Assert
            var stored = await dbContext.Recipes.Include(x => x.Ingredients).SingleAsync();
            Assert.Equal(200m, Assert.Single(stored.Ingredients).PortionQuantity);
        }

        [Fact]
        public async Task Handle_WhenDifferentFoodIsAdded_KeepsBothIngredients()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var recipeId = Guid.NewGuid();
            var firstFoodId = Guid.NewGuid();
            var secondFoodId = Guid.NewGuid();
            dbContext.Recipes.Add(NewRecipe(recipeId));
            await dbContext.SaveChangesAsync();
            var _sut = new ProjectIngredientModelWhenIngredientAddedToRecipe(dbContext);
            await _sut.Handle(new OutboxMessage<IngredientAddedToRecipe>(
                new IngredientAddedToRecipe(recipeId, firstFoodId, 100m)));

            // Act
            await _sut.Handle(new OutboxMessage<IngredientAddedToRecipe>(
                new IngredientAddedToRecipe(recipeId, secondFoodId, 50m)));

            // Assert
            var stored = await dbContext.Recipes.Include(x => x.Ingredients).SingleAsync();
            Assert.Equal(2, stored.Ingredients.Count);
        }

        [Fact]
        public async Task Handle_WhenRecipeIsNotProjected_DoesNotInsertAnIngredient()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var _sut = new ProjectIngredientModelWhenIngredientAddedToRecipe(dbContext);

            // Act
            await _sut.Handle(new OutboxMessage<IngredientAddedToRecipe>(
                new IngredientAddedToRecipe(Guid.NewGuid(), Guid.NewGuid(), 100m)));

            // Assert
            Assert.Equal(0, await dbContext.Recipes.CountAsync());
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
