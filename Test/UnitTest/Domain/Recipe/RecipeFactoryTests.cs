using Catalog.Domain.Factory.Recipe;
using Catalog.Domain.Primitives;
using Catalog.Domain.Model.Recipes;
using Joseco.DDD.Core.Abstractions;
using Xunit;

namespace Catalog.Tests.Domain
{
    public class RecipeFactoryTests
    {
        [Fact]
        public void Create_ValidData_CreatesRecipeWithInitialState()
        {
            // Arrange
            var factory = new RecipeFactory();

            // Act
            var recipe = factory.Create("Ensalada", "Mezclar todos los ingredientes");

            // Assert
            Assert.IsType<Recipe>(recipe);
            Assert.NotEqual(Guid.Empty, recipe.Id);
            Assert.Equal("Ensalada", recipe.Name.Name);
            Assert.Equal("Mezclar todos los ingredientes", recipe.Instructions);
            Assert.Empty(recipe.Ingredients);
            Assert.Single(recipe.DomainEvents);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_EmptyOrWhitespaceInstructions_ThrowsException(string? instructions)
        {
            // Arrange
            var factory = new RecipeFactory();

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() => factory.Create("Ensalada", instructions!));
            Assert.Equal("Recipe instructions cannot be empty", ex.Details);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Create_NullOrEmptyName_ThrowsException(string? name)
        {
            // Arrange
            var factory = new RecipeFactory();

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() => factory.Create(name!, "Mezclar"));
            Assert.Equal("string cannot be null", ex.Details);
        }

        [Fact]
        public void Create_WhitespaceName_CreatesRecipe()
        {
            // Arrange
            var factory = new RecipeFactory();

            // Act
            var recipe = factory.Create("   ", "Mezclar");

            // Assert
            Assert.Equal("   ", recipe.Name.Name);
        }
    }
}
