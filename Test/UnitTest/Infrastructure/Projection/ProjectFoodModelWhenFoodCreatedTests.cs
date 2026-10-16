using Catalog.Domain.Event.Food;
using Catalog.Domain.ValueObjects;
using Catalog.Infrastructure.Persistence.StoredModel.Projection;
using Joseco.Outbox.Contracts.Model;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Catalog.Tests.Infrastructure.Projection
{
    public class ProjectFoodModelWhenFoodCreatedTests
    {
        [Fact]
        public async Task Handle_WhenFoodDoesNotExist_InsertsFoodModel()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var _sut = new ProjectFoodModelWhenFoodCreated(dbContext);
            var foodCreated = NewFoodCreated("MANZANA", "FRUTAS", 100, 52, 0.3m, 14, 0.2m);

            // Act
            await _sut.Handle(new OutboxMessage<FoodCreated>(foodCreated));

            // Assert
            var stored = await dbContext.Foods.SingleAsync();
            Assert.Equal(foodCreated.FoodId, stored.Id);
            Assert.Equal("MANZANA", stored.Name);
            Assert.Equal("FRUTAS", stored.Category);
        }

        [Fact]
        public async Task Handle_WhenFoodCreated_StoresTheNutritionalInfo()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var _sut = new ProjectFoodModelWhenFoodCreated(dbContext);

            // Act
            await _sut.Handle(new OutboxMessage<FoodCreated>(
                NewFoodCreated("MANZANA", "FRUTAS", 100, 52, 0.3m, 14, 0.2m)));

            // Assert
            var stored = await dbContext.Foods.SingleAsync();
            Assert.Equal(100m, stored.Quantity);
            Assert.Equal(52m, stored.Calories);
            Assert.Equal(0.3m, stored.Protein);
            Assert.Equal(14m, stored.Carbs);
            Assert.Equal(0.2m, stored.Fat);
        }

        [Fact]
        public async Task Handle_WhenSameMessageIsProjectedTwice_KeepsOneFoodModel()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var _sut = new ProjectFoodModelWhenFoodCreated(dbContext);
            var foodCreated = NewFoodCreated("MANZANA", "FRUTAS", 100, 52, 0.3m, 14, 0.2m);

            // Act
            await _sut.Handle(new OutboxMessage<FoodCreated>(foodCreated));
            await _sut.Handle(new OutboxMessage<FoodCreated>(foodCreated));

            // Assert
            Assert.Equal(1, await dbContext.Foods.CountAsync());
        }

        [Fact]
        public async Task Handle_WhenSameMessageIsProjectedTwice_RefreshesTheExistingModel()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var _sut = new ProjectFoodModelWhenFoodCreated(dbContext);
            var foodCreated = NewFoodCreated("MANZANA", "FRUTAS", 100, 52, 0.3m, 14, 0.2m);
            await _sut.Handle(new OutboxMessage<FoodCreated>(foodCreated));

            // Act
            await _sut.Handle(new OutboxMessage<FoodCreated>(foodCreated));

            // Assert
            dbContext.ChangeTracker.Clear();
            var stored = await dbContext.Foods.SingleAsync();
            Assert.Equal(52m, stored.Calories);
            Assert.NotNull(stored.UpdatedAt);
        }

        [Fact]
        public async Task Handle_WhenFoodCreated_StoresTheUnitOfMeasureFromTheEvent()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var _sut = new ProjectFoodModelWhenFoodCreated(dbContext);

            // Act
            await _sut.Handle(new OutboxMessage<FoodCreated>(
                NewFoodCreated("MANZANA", "FRUTAS", 100, 52, 0.3m, 14, 0.2m, UnitOfMeasure.Kilogram)));

            // Assert
            var stored = await dbContext.Foods.SingleAsync();
            Assert.Equal((int)UnitOfMeasure.Kilogram, stored.UnitOfMeasure);
        }

        private static FoodCreated NewFoodCreated(
            string name, string category, decimal quantity, decimal calories, decimal protein, decimal carbs, decimal fat,
            UnitOfMeasure unitOfMeasure = UnitOfMeasure.Gram)
        {
            return new FoodCreated(
                Guid.NewGuid(),
                name,
                category,
                new NutritionalInfo(quantity, calories, protein, carbs, fat),
                unitOfMeasure);
        }
    }
}
