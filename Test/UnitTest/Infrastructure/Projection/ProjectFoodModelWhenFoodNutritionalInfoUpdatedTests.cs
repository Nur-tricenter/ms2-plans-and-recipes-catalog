using Catalog.Domain.Event.Food;
using Catalog.Domain.ValueObjects;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.Food;
using Catalog.Infrastructure.Persistence.StoredModel.Projection;
using Joseco.Outbox.Contracts.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Catalog.Tests.Infrastructure.Projection
{
    public class ProjectFoodModelWhenFoodNutritionalInfoUpdatedTests
    {
        private readonly Mock<ILogger<ProjectFoodModelWhenFoodNutritionalInfoUpdated>> _loggerMock;

        public ProjectFoodModelWhenFoodNutritionalInfoUpdatedTests()
        {
            _loggerMock = new Mock<ILogger<ProjectFoodModelWhenFoodNutritionalInfoUpdated>>();
        }

        [Fact]
        public async Task Handle_WhenFoodExists_UpdatesTheNutritionalFields()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var foodId = Guid.NewGuid();
            dbContext.Foods.Add(NewFood(foodId, 100, 52, 0.3m, 14, 0.2m));
            await dbContext.SaveChangesAsync();
            var _sut = new ProjectFoodModelWhenFoodNutritionalInfoUpdated(dbContext, _loggerMock.Object);

            // Act
            await _sut.Handle(new OutboxMessage<FoodNutritionalInfoUpdated>(new FoodNutritionalInfoUpdated(
                foodId,
                new NutritionalInfo(100, 52, 0.3m, 14, 0.2m),
                new NutritionalInfo(100, 60, 0.5m, 15, 0.4m))));

            // Assert
            dbContext.ChangeTracker.Clear();
            var stored = await dbContext.Foods.SingleAsync();
            Assert.Equal(60m, stored.Calories);
            Assert.Equal(0.5m, stored.Protein);
            Assert.Equal(15m, stored.Carbs);
            Assert.Equal(0.4m, stored.Fat);
            Assert.Equal(100m, stored.Quantity);
        }

        [Fact]
        public async Task Handle_WhenSameMessageIsProjectedTwice_KeepsOneFoodModel()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var foodId = Guid.NewGuid();
            dbContext.Foods.Add(NewFood(foodId, 100, 52, 0.3m, 14, 0.2m));
            await dbContext.SaveChangesAsync();
            var _sut = new ProjectFoodModelWhenFoodNutritionalInfoUpdated(dbContext, _loggerMock.Object);
            var updated = new FoodNutritionalInfoUpdated(
                foodId,
                new NutritionalInfo(100, 52, 0.3m, 14, 0.2m),
                new NutritionalInfo(100, 60, 0.5m, 15, 0.4m));

            // Act
            await _sut.Handle(new OutboxMessage<FoodNutritionalInfoUpdated>(updated));
            await _sut.Handle(new OutboxMessage<FoodNutritionalInfoUpdated>(updated));

            // Assert
            Assert.Equal(1, await dbContext.Foods.CountAsync());
        }

        [Fact]
        public async Task Handle_WhenSameMessageIsProjectedTwice_KeepsTheNewNutritionalValues()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var foodId = Guid.NewGuid();
            dbContext.Foods.Add(NewFood(foodId, 100, 52, 0.3m, 14, 0.2m));
            await dbContext.SaveChangesAsync();
            var _sut = new ProjectFoodModelWhenFoodNutritionalInfoUpdated(dbContext, _loggerMock.Object);
            var updated = new FoodNutritionalInfoUpdated(
                foodId,
                new NutritionalInfo(100, 52, 0.3m, 14, 0.2m),
                new NutritionalInfo(100, 60, 0.5m, 15, 0.4m));
            await _sut.Handle(new OutboxMessage<FoodNutritionalInfoUpdated>(updated));

            // Act
            await _sut.Handle(new OutboxMessage<FoodNutritionalInfoUpdated>(updated));

            // Assert
            Assert.Equal(60m, (await dbContext.Foods.SingleAsync()).Calories);
        }

        [Fact]
        public async Task Handle_WhenFoodDoesNotExist_DoesNotInsertAFoodModel()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var _sut = new ProjectFoodModelWhenFoodNutritionalInfoUpdated(dbContext, _loggerMock.Object);

            // Act
            await _sut.Handle(new OutboxMessage<FoodNutritionalInfoUpdated>(new FoodNutritionalInfoUpdated(
                Guid.NewGuid(),
                new NutritionalInfo(100, 52, 0.3m, 14, 0.2m),
                new NutritionalInfo(100, 60, 0.5m, 15, 0.4m))));

            // Assert
            Assert.Equal(0, await dbContext.Foods.CountAsync());
        }

        [Fact]
        public async Task Handle_WhenFoodDoesNotExist_LeavesTheOtherFoodsUntouched()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var otherFoodId = Guid.NewGuid();
            dbContext.Foods.Add(NewFood(otherFoodId, 100, 52, 0.3m, 14, 0.2m));
            await dbContext.SaveChangesAsync();
            var _sut = new ProjectFoodModelWhenFoodNutritionalInfoUpdated(dbContext, _loggerMock.Object);

            // Act
            await _sut.Handle(new OutboxMessage<FoodNutritionalInfoUpdated>(new FoodNutritionalInfoUpdated(
                Guid.NewGuid(),
                new NutritionalInfo(100, 52, 0.3m, 14, 0.2m),
                new NutritionalInfo(100, 60, 0.5m, 15, 0.4m))));

            // Assert
            Assert.Equal(52m, (await dbContext.Foods.SingleAsync()).Calories);
        }

        private static FoodModel NewFood(
            Guid foodId, decimal quantity, decimal calories, decimal protein, decimal carbs, decimal fat)
        {
            return new FoodModel
            {
                Id = foodId,
                Name = "MANZANA",
                Category = "FRUTAS",
                Quantity = quantity,
                Calories = calories,
                Protein = protein,
                Carbs = carbs,
                Fat = fat,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
