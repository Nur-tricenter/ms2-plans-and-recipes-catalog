using Catalog.Application.UseCase.Command.Recipe.AddIngredient;
using Catalog.Domain.Model.Foods;
using Catalog.Domain.Model.Recipes;
using Catalog.Domain.Repository.Food;
using Catalog.Domain.Repository.Recipe;
using Catalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Joseco.DDD.Core.Abstractions;
using Xunit;

namespace Catalog.Tests.Application
{
    public class AddIngredientHandlerTests
    {
        private readonly Mock<IRecipeRepository> _recipeRepoMock;
        private readonly Mock<IFoodRepository> _foodRepoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<AddIngredientHandler>> _loggerMock;
        private readonly AddIngredientHandler _handler;

        public AddIngredientHandlerTests()
        {
            _recipeRepoMock = new Mock<IRecipeRepository>();
            _foodRepoMock = new Mock<IFoodRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<AddIngredientHandler>>();
            _handler = new AddIngredientHandler(_recipeRepoMock.Object, _foodRepoMock.Object, _unitOfWorkMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsSuccessAndVerifiesInteractions()
        {
            // Arrange
            var recipe = new Recipe(new RecipeName("Ensalada"), "Mezclar");
            var apple = new Food(new FoodName("Manzana"), new CategoryName("Fruta"), UnitOfMeasure.Gram, new NutritionalInfo(100, 52, 0.3m, 14, 0.2m));
            var chicken = new Food(new FoodName("Pollo"), new CategoryName("Proteina"), UnitOfMeasure.Gram, new NutritionalInfo(100, 165, 31m, 0m, 3.6m));
            _recipeRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync(recipe);
            _foodRepoMock.Setup(a => a.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync(apple);
            _foodRepoMock.Setup(a => a.GetByIdAsync(It.Is<Guid>(id => id == chicken.Id), It.IsAny<bool>())).ReturnsAsync(chicken);

            var command = new AddIngredientCommand
            {
                RecipeId = recipe.Id,
                Ingredients = new List<IngredientItem>
                {
                    new IngredientItem { FoodId = apple.Id, Quantity = 200 },
                    new IngredientItem { FoodId = chicken.Id, Quantity = 100 }
                }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("2 ingredient(s) added successfully", result.Message);
            Assert.Equal(2, recipe.Ingredients.Count());
            _recipeRepoMock.Verify(r => r.GetByIdAsync(It.Is<Guid>(id => id == command.RecipeId), It.IsAny<bool>()), Times.Once);
            _foodRepoMock.Verify(a => a.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Exactly(2));
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_RecipeNotFound_ReturnsFailureAndNoCommit()
        {
            // Arrange
            _recipeRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync((Recipe?)null);
            var command = new AddIngredientCommand
            {
                RecipeId = Guid.NewGuid(),
                Ingredients = new List<IngredientItem> { new IngredientItem { FoodId = Guid.NewGuid(), Quantity = 1 } }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Recipe not found", result.Message);
            _foodRepoMock.Verify(a => a.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_FoodNotFound_ReturnsFailureAndNoCommit()
        {
            // Arrange
            var recipe = new Recipe(new RecipeName("Ensalada"), "Mezclar");
            _recipeRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync(recipe);
            _foodRepoMock.Setup(a => a.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync((Food?)null);
            var foodId = Guid.NewGuid();
            var command = new AddIngredientCommand
            {
                RecipeId = recipe.Id,
                Ingredients = new List<IngredientItem> { new IngredientItem { FoodId = foodId, Quantity = 1 } }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains($"Food with Id {foodId} does not exist", result.Message);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Handle_NonPositivePortion_ReturnsFailureAndNoCommit(decimal quantity)
        {
            // Arrange
            var recipe = new Recipe(new RecipeName("Ensalada"), "Mezclar");
            var food = new Food(new FoodName("Manzana"), new CategoryName("Fruta"), UnitOfMeasure.Gram, new NutritionalInfo(100, 52, 0.3m, 14, 0.2m));
            _recipeRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync(recipe);
            _foodRepoMock.Setup(a => a.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync(food);
            var command = new AddIngredientCommand
            {
                RecipeId = recipe.Id,
                Ingredients = new List<IngredientItem> { new IngredientItem { FoodId = food.Id, Quantity = quantity } }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Portion quantity must be greater than zero", result.Message);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_DuplicateIngredient_ReturnsFailureAndNoCommit()
        {
            // Arrange
            var recipe = new Recipe(new RecipeName("Ensalada"), "Mezclar");
            var food = new Food(new FoodName("Manzana"), new CategoryName("Fruta"), UnitOfMeasure.Gram, new NutritionalInfo(100, 52, 0.3m, 14, 0.2m));
            recipe.AddIngredient(food.Id, new Portion(100));
            _recipeRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync(recipe);
            _foodRepoMock.Setup(a => a.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync(food);
            var command = new AddIngredientCommand
            {
                RecipeId = recipe.Id,
                Ingredients = new List<IngredientItem> { new IngredientItem { FoodId = food.Id, Quantity = 200 } }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("is already added", result.Message);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
