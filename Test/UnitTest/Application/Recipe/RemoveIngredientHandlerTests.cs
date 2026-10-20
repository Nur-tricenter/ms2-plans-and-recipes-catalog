using Catalog.Application.UseCase.Command.Recipe.RemoveIngredient;
using Catalog.Domain.Model.Recipes;
using Catalog.Domain.Repository.Recipe;
using Catalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Joseco.DDD.Core.Abstractions;
using Xunit;

namespace Catalog.Tests.Application
{
    public class RemoveIngredientHandlerTests
    {
        private readonly Mock<IRecipeRepository> _recipeRepoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<RemoveIngredientHandler>> _loggerMock;
        private readonly RemoveIngredientHandler _handler;

        public RemoveIngredientHandlerTests()
        {
            _recipeRepoMock = new Mock<IRecipeRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<RemoveIngredientHandler>>();
            _handler = new RemoveIngredientHandler(_recipeRepoMock.Object, _unitOfWorkMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsSuccessAndVerifiesInteractions()
        {
            // Arrange
            var recipe = new Recipe(new RecipeName("Ensalada"), "Mezclar");
            var foodId = Guid.NewGuid();
            recipe.AddIngredient(foodId, new Portion(2));
            _recipeRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync(recipe);
            var command = new RemoveIngredientCommand { RecipeId = recipe.Id, FoodId = foodId };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Ingredient removed successfully", result.Message);
            Assert.Empty(recipe.Ingredients);
            _recipeRepoMock.Verify(r => r.GetByIdAsync(It.Is<Guid>(id => id == command.RecipeId), It.IsAny<bool>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_RecipeNotFound_ReturnsFailureAndNoCommit()
        {
            // Arrange
            _recipeRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync((Recipe?)null);
            var command = new RemoveIngredientCommand { RecipeId = Guid.NewGuid(), FoodId = Guid.NewGuid() };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Recipe not found", result.Message);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_IngredientNotInRecipe_ReturnsFailureAndNoCommit()
        {
            // Arrange
            var recipe = new Recipe(new RecipeName("Ensalada"), "Mezclar");
            _recipeRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync(recipe);
            var command = new RemoveIngredientCommand { RecipeId = recipe.Id, FoodId = Guid.NewGuid() };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("does not exist in the recipe", result.Message);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
