using Catalog.Application.UseCase.Command.Food.UpdateFood;
using Catalog.Domain.Model.Foods;
using Catalog.Domain.Repository.Food;
using Catalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Joseco.DDD.Core.Abstractions;
using Xunit;

namespace Catalog.Tests.Application
{
    public class UpdateFoodHandlerTests
    {
        private readonly Mock<IFoodRepository> _repoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<UpdateFoodHandler>> _loggerMock;
        private readonly UpdateFoodHandler _handler;

        public UpdateFoodHandlerTests()
        {
            _repoMock = new Mock<IFoodRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<UpdateFoodHandler>>();
            _handler = new UpdateFoodHandler(_repoMock.Object, _unitOfWorkMock.Object, _loggerMock.Object);
        }

        private static UpdateFoodCommand CreateValidCommand(Guid foodId) =>
            new()
            {
                FoodId = foodId,
                Name = "Manzana Roja",
                Category = "Fruta",
                UnitOfMeasure = (int)UnitOfMeasure.Kilogram,
                Quantity = 100,
                Calories = 60,
                Protein = 0.5m,
                Carbs = 16,
                Fat = 0.3m
            };

        [Fact]
        public async Task Handle_ValidCommand_ReturnsSuccessAndVerifiesInteractions()
        {
            // Arrange
            var food = new Food(new FoodName("MANZANA"), new CategoryName("FRUTA"), UnitOfMeasure.Gram, new NutritionalInfo(100, 52, 0.3m, 14, 0.2m));
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(food);
            var command = CreateValidCommand(food.Id);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Food updated successfully", result.Message);
            Assert.Equal("MANZANA ROJA", food.Name.Name);
            Assert.Equal(UnitOfMeasure.Kilogram, food.UnitOfMeasure);
            Assert.Equal(60m, food.BaseNutritionalInfo.Calories);
            _repoMock.Verify(r => r.GetByIdAsync(It.Is<Guid>(id => id == food.Id)), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_FoodNotFound_ReturnsFailureAndNoCommit()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Food?)null);
            var command = CreateValidCommand(Guid.NewGuid());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Food not found", result.Message);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(99)]
        public async Task Handle_InvalidUnitOfMeasure_ReturnsFailureAndNoCommit(int unitOfMeasure)
        {
            // Arrange
            var food = new Food(new FoodName("MANZANA"), new CategoryName("FRUTA"), UnitOfMeasure.Gram, new NutritionalInfo(100, 52, 0.3m, 14, 0.2m));
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(food);
            var command = CreateValidCommand(food.Id);
            command.UnitOfMeasure = unitOfMeasure;

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Invalid unit of measure", result.Message);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task Handle_NonPositiveQuantity_ReturnsFailureAndNoCommit(decimal quantity)
        {
            // Arrange
            var food = new Food(new FoodName("MANZANA"), new CategoryName("FRUTA"), UnitOfMeasure.Gram, new NutritionalInfo(100, 52, 0.3m, 14, 0.2m));
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(food);
            var command = CreateValidCommand(food.Id);
            command.Quantity = quantity;

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("NutritionalInfo quantity must be greater than zero", result.Message);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}