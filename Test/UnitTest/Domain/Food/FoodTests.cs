using Catalog.Domain.Event.Food;
using Catalog.Domain.Model.Foods;
using Catalog.Domain.ValueObjects;
using Xunit;

namespace Catalog.Tests.Domain
{
    public class FoodTests
    {
        [Fact]
        public void Constructor_ValidData_CreatesFoodWithInitialState()
        {
            // Arrange
            var name = new FoodName("Manzana");
            var category = new CategoryName("Fruta");
            var info = new NutritionalInfo(100, 52, 0.3m, 14, 0.2m);

            // Act
            var food = new Food(name, category, UnitOfMeasure.Gram, info);

            // Assert
            Assert.NotEqual(Guid.Empty, food.Id);
            Assert.Equal("Manzana", food.Name.Name);
            Assert.Equal("Fruta", food.Category.Name);
            Assert.Equal(UnitOfMeasure.Gram, food.UnitOfMeasure);
            Assert.Equal(52m, food.BaseNutritionalInfo.Calories);
            var evento = Assert.IsType<FoodCreated>(Assert.Single(food.DomainEvents));
            Assert.Equal(food.Id, evento.FoodId);
        }

        [Fact]
        public void UpdateNutritionalInfo_ValidInfo_UpdatesAndQueuesEvent()
        {
            // Arrange
            var food = new Food(new FoodName("Manzana"), new CategoryName("Fruta"), UnitOfMeasure.Gram, new NutritionalInfo(100, 52, 0.3m, 14, 0.2m));
            var newInfo = new NutritionalInfo(100, 60, 0.5m, 16, 0.3m);

            // Act
            food.UpdateNutritionalInfo(newInfo);

            // Assert
            Assert.Equal(60m, food.BaseNutritionalInfo.Calories);
            var evento = Assert.IsType<FoodNutritionalInfoUpdated>(food.DomainEvents.Last());
            Assert.Equal(52m, evento.PreviousNutritionalInfo.Calories);
            Assert.Equal(60m, evento.NewNutritionalInfo.Calories);
        }

        [Fact]
        public void Update_ValidData_UpdatesAllFieldsAndQueuesEvent()
        {
            // Arrange
            var food = new Food(new FoodName("Manzana"), new CategoryName("Fruta"), UnitOfMeasure.Gram, new NutritionalInfo(100, 52, 0.3m, 14, 0.2m));

            // Act
            food.Update(new FoodName("Manzana Roja"), new CategoryName("Fruta de temporada"), UnitOfMeasure.Kilogram, new NutritionalInfo(100, 60, 0.5m, 16, 0.3m));

            // Assert
            Assert.Equal("Manzana Roja", food.Name.Name);
            Assert.Equal("Fruta de temporada", food.Category.Name);
            Assert.Equal(UnitOfMeasure.Kilogram, food.UnitOfMeasure);
            Assert.Equal(60m, food.BaseNutritionalInfo.Calories);
            Assert.Equal(2, food.DomainEvents.Count);
            Assert.IsType<FoodNutritionalInfoUpdated>(food.DomainEvents.Last());
        }
    }
}