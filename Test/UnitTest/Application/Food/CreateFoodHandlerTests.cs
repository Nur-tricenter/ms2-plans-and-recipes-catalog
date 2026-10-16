using Catalog.Application.UseCase.Command.Food.CreateFood;
using Catalog.Domain.Factory.Food;
using Catalog.Domain.Model.Foods;
using Catalog.Domain.Repository.Food;
using Catalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Core;
using Xunit;

namespace Catalog.Tests.Application
{
    public class CreateFoodHandlerTests
    {
        private readonly Mock<IFoodRepository> _repoMock;
        private readonly Mock<IFoodFactory> _factoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<CreateFoodHandler>> _loggerMock;
        private readonly CreateFoodHandler _handler;

        public CreateFoodHandlerTests()
        {
            _repoMock = new Mock<IFoodRepository>();
            _factoryMock = new Mock<IFoodFactory>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<CreateFoodHandler>>();
            _handler = new CreateFoodHandler(_repoMock.Object, _factoryMock.Object, _unitOfWorkMock.Object, _loggerMock.Object);
        }

        private static CreateFoodCommand CreateValidCommand() =>
            new()
            {
                Name = "Manzana",
                Category = "Fruta",
                UnitOfMeasure = (int)UnitOfMeasure.Gram,
                Quantity = 100,
                Calories = 52,
                Protein = 0.3m,
                Carbs = 14,
                Fat = 0.2m
            };

        [Fact]
        public async Task Handle_ValidCommand_ReturnsSuccessAndVerifiesInteractions()
        {
            // Arrange
            var food = new Food(new FoodName("MANZANA"), new CategoryName("FRUTA"), UnitOfMeasure.Gram, new NutritionalInfo(100, 52, 0.3m, 14, 0.2m));
            _factoryMock.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UnitOfMeasure>(), It.IsAny<NutritionalInfo>()))
                .Returns(food);
            var command = CreateValidCommand();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(food.Id, result.Value);
            Assert.Equal("Food created successfully", result.Message);
            _factoryMock.Verify(f => f.Create("MANZANA", "FRUTA", UnitOfMeasure.Gram, It.Is<NutritionalInfo>(i => i.Calories == 52m)), Times.Once);
            _repoMock.Verify(r => r.CreateAsync(It.Is<Food>(a => a.Name.Name == "MANZANA")), Times.Once);
            _unitOfWorkMock.Verify(u => u.Commit(), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(99)]
        [InlineData(-1)]
        public async Task Handle_InvalidUnitOfMeasure_ReturnsFailureAndNoCommit(int unitOfMeasure)
        {
            // Arrange
            var command = CreateValidCommand();
            command.UnitOfMeasure = unitOfMeasure;

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Invalid unit of measure", result.Message);
            _factoryMock.Verify(f => f.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UnitOfMeasure>(), It.IsAny<NutritionalInfo>()), Times.Never);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Food>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.Commit(), Times.Never);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public async Task Handle_NonPositiveQuantity_ReturnsFailureAndNoCommit(decimal quantity)
        {
            // Arrange
            var command = CreateValidCommand();
            command.Quantity = quantity;

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("NutritionalInfo quantity must be greater than zero", result.Message);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Food>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.Commit(), Times.Never);
        }

        [Fact]
        public async Task Handle_FactoryThrows_ReturnsFailureAndNoCommit()
        {
            // Arrange
            _factoryMock.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UnitOfMeasure>(), It.IsAny<NutritionalInfo>()))
                .Throws(new BussinessRuleValidationException("Food name cannot be null"));
            var command = CreateValidCommand();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Food name cannot be null", result.Message);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Food>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.Commit(), Times.Never);
        }
    }
}