using Catalog.Domain.Event.Recipe;
using Catalog.Domain.Primitives;
using Catalog.Domain.Model.Foods;
using Catalog.Domain.Model.Recipes;
using Catalog.Domain.ValueObjects;
using Joseco.DDD.Core.Abstractions;
using Xunit;

namespace Catalog.Tests.Domain
{
    public class RecipeTests
    {
        [Fact]
        public void Constructor_ValidData_CreatesRecipeWithInitialState()
        {
            // Arrange
            var nombre = new RecipeName("Ensalada");

            // Act
            var recipe = new Recipe(nombre, "Mezclar todos los ingredients");

            // Assert
            Assert.NotEqual(Guid.Empty, recipe.Id);
            Assert.Equal("Ensalada", recipe.Name.Name);
            Assert.Equal("Mezclar todos los ingredients", recipe.Instructions);
            Assert.Empty(recipe.Ingredients);
            var evt = Assert.IsType<RecipeCreated>(Assert.Single(recipe.DomainEvents));
            Assert.Equal(recipe.Id, evt.RecipeId);
        }

        [Fact]
        public void Constructor_ValidData_RaisesRecipeCreatedWithTheInstructions()
        {
            // Arrange
            var nombre = new RecipeName("Ensalada");

            // Act
            var recipe = new Recipe(nombre, "Mezclar todos los ingredientes");

            // Assert
            var evt = Assert.IsType<RecipeCreated>(Assert.Single(recipe.DomainEvents));
            Assert.Equal("Mezclar todos los ingredientes", evt.Instructions);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_EmptyOrWhitespaceInstructions_ThrowsException(string? instructions)
        {
            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() =>
                new Recipe(new RecipeName("Ensalada"), instructions!));
            Assert.Equal("Recipe instructions cannot be empty", ex.Details);
        }

        [Fact]
        public void AddIngredient_ValidIngredient_AddsAndQueuesEvent()
        {
            // Arrange
            var recipe = new Recipe(new RecipeName("Ensalada"), "Mezclar");
            var foodId = Guid.NewGuid();

            // Act
            recipe.AddIngredient(foodId, new Portion(2));

            // Assert
            var ingredient = Assert.Single(recipe.Ingredients);
            Assert.Equal(foodId, ingredient.FoodId);
            Assert.Equal(2m, ingredient.Portion.Quantity);
            var evt = Assert.IsType<IngredientAddedToRecipe>(recipe.DomainEvents.Last());
            Assert.Equal(foodId, evt.FoodId);
            Assert.Equal(2m, evt.PortionQuantity);
        }

        [Fact]
        public void AddIngredient_DuplicateIngredient_ThrowsException()
        {
            // Arrange
            var recipe = new Recipe(new RecipeName("Ensalada"), "Mezclar");
            var foodId = Guid.NewGuid();
            recipe.AddIngredient(foodId, new Portion(1));

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() =>
                recipe.AddIngredient(foodId, new Portion(2)));
            Assert.Contains("is already added", ex.Details);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void AddIngredient_NonPositiveQuantity_ThrowsException(decimal quantity)
        {
            // Arrange
            var recipe = new Recipe(new RecipeName("Ensalada"), "Mezclar");

            // Act + Assert
            Assert.Throws<BussinessRuleValidationException>(() =>
                recipe.AddIngredient(Guid.NewGuid(), new Portion(quantity)));
        }

        [Fact]
        public void RemoveIngredient_ExistingIngredient_RemovesAndQueuesEvent()
        {
            // Arrange
            var recipe = new Recipe(new RecipeName("Ensalada"), "Mezclar");
            var foodId = Guid.NewGuid();
            recipe.AddIngredient(foodId, new Portion(2));

            // Act
            recipe.RemoveIngredient(foodId);

            // Assert
            Assert.Empty(recipe.Ingredients);
            var evt = Assert.IsType<IngredientRemovedFromRecipe>(recipe.DomainEvents.Last());
            Assert.Equal(foodId, evt.FoodId);
            Assert.Equal(2m, evt.PortionQuantity);
        }

        [Fact]
        public void RemoveIngredient_NonExistingIngredient_ThrowsException()
        {
            // Arrange
            var recipe = new Recipe(new RecipeName("Ensalada"), "Mezclar");

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() =>
                recipe.RemoveIngredient(Guid.NewGuid()));
            Assert.Contains("does not exist in the recipe", ex.Details);
        }

        [Theory]
        [InlineData(2, 104)]
        [InlineData(1, 52)]
        [InlineData(0.5, 26)]
        public void CalculateNutritionalInfo_DifferentPortions_CalculatesCalories(decimal portion, decimal expected)
        {
            // Arrange
            var recipe = new Recipe(new RecipeName("Ensalada"), "Mezclar");
            var food = new Food(new FoodName("Manzana"), new CategoryName("Fruta"), UnitOfMeasure.Gram, new NutritionalInfo(1, 52, 0.3m, 14, 0.2m));
            recipe.AddIngredient(food.Id, new Portion(portion));

            // Act
            var info = recipe.CalculateTotalNutritionalInfo(_ => food);

            // Assert
            Assert.Equal(expected, info.Calories);
        }

        [Fact]
        public void CalculateNutritionalInfo_WithMultipleIngredients_CalculatesTotals()
        {
            // Arrange
            var recipe = new Recipe(new RecipeName("Ensalada"), "Mezclar");
            var apple = new Food(new FoodName("Manzana"), new CategoryName("Fruta"), UnitOfMeasure.Gram, new NutritionalInfo(100, 52, 0.3m, 14, 0.2m));
            var chicken = new Food(new FoodName("Pollo"), new CategoryName("Proteina"), UnitOfMeasure.Gram, new NutritionalInfo(100, 165, 31m, 0m, 3.6m));
            recipe.AddIngredient(apple.Id, new Portion(200));
            recipe.AddIngredient(chicken.Id, new Portion(100));

            // Act
            var info = recipe.CalculateTotalNutritionalInfo(id => id == apple.Id ? apple : chicken);

            // Assert
            Assert.Equal(269m, info.Calories);
            Assert.Equal(31.6m, info.Protein);
            Assert.Equal(28m, info.Carbs);
            Assert.Equal(4m, info.Fat);
        }

        [Fact]
        public void CalculateNutritionalInfo_WhenFoodIsNull_SkipsIngredient()
        {
            // Arrange
            var recipe = new Recipe(new RecipeName("Ensalada"), "Mezclar");
            recipe.AddIngredient(Guid.NewGuid(), new Portion(2));

            // Act
            var info = recipe.CalculateTotalNutritionalInfo(_ => null!);

            // Assert
            Assert.Equal(0m, info.Calories);
            Assert.Equal(0m, info.Protein);
            Assert.Equal(0m, info.Carbs);
            Assert.Equal(0m, info.Fat);
        }
    }
}
