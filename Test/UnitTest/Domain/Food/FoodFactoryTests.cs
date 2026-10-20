using Catalog.Domain.Factory.Food;
using Catalog.Domain.Primitives;
using Catalog.Domain.Model.Foods;
using Catalog.Domain.ValueObjects;
using Joseco.DDD.Core.Abstractions;
using Xunit;

namespace Catalog.Tests.Domain
{
    public class FoodFactoryTests
    {
        [Fact]
        public void Create_ValidData_CreatesFoodWithInitialState()
        {
            // Arrange
            var factory = new FoodFactory();
            var info = new NutritionalInfo(100, 52, 0.3m, 14, 0.2m);

            // Act
            var food = factory.Create("Manzana", "Fruta", UnitOfMeasure.Gram, info);

            // Assert
            Assert.IsType<Food>(food);
            Assert.NotEqual(Guid.Empty, food.Id);
            Assert.Equal("Manzana", food.Name.Name);
            Assert.Equal("Fruta", food.Category.Name);
            Assert.Equal(UnitOfMeasure.Gram, food.UnitOfMeasure);
            Assert.Equal(info, food.BaseNutritionalInfo);
            Assert.Single(food.DomainEvents);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Create_NullOrEmptyName_ThrowsException(string? name)
        {
            // Arrange
            var factory = new FoodFactory();

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() =>
                factory.Create(name!, "Fruta", UnitOfMeasure.Gram, new NutritionalInfo(100, 52, 0.3m, 14, 0.2m)));
            Assert.Equal("string cannot be null", ex.Details);
        }

        [Fact]
        public void Create_WhitespaceName_CreatesFood()
        {
            // Arrange
            var factory = new FoodFactory();

            // Act
            var food = factory.Create("   ", "Fruta", UnitOfMeasure.Gram, new NutritionalInfo(100, 52, 0.3m, 14, 0.2m));

            // Assert
            Assert.Equal("   ", food.Name.Name);
        }
    }
}
