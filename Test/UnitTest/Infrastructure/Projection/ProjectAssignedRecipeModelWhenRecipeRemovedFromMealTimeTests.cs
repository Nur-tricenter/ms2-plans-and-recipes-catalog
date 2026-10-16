using Catalog.Domain.Event.MealPlan;
using Catalog.Infrastructure.Persistence.StoredModel;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.MealPlan;
using Catalog.Infrastructure.Persistence.StoredModel.Projection;
using Joseco.Outbox.Contracts.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Catalog.Tests.Infrastructure.Projection
{
    public class ProjectAssignedRecipeModelWhenRecipeRemovedFromMealTimeTests
    {
        private readonly Mock<ILogger<ProjectAssignedRecipeModelWhenRecipeRemovedFromMealTime>> _loggerMock;

        public ProjectAssignedRecipeModelWhenRecipeRemovedFromMealTimeTests()
        {
            _loggerMock = new Mock<ILogger<ProjectAssignedRecipeModelWhenRecipeRemovedFromMealTime>>();
        }

        [Fact]
        public async Task Handle_WhenAssignmentIsProjected_RemovesIt()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var mealTimeId = Guid.NewGuid();
            var recipeId = Guid.NewGuid();
            await SeedMealTimeAsync(dbContext, mealTimeId, recipeId);
            var _sut = new ProjectAssignedRecipeModelWhenRecipeRemovedFromMealTime(dbContext, _loggerMock.Object);

            // Act
            await _sut.Handle(new OutboxMessage<RecipeRemovedFromMealTime>(
                new RecipeRemovedFromMealTime(Guid.NewGuid(), 1, mealTimeId, recipeId)));

            // Assert
            Assert.Empty(await AssignmentsAsync(dbContext, mealTimeId));
        }

        [Fact]
        public async Task Handle_WhenAssignmentIsRemoved_KeepsTheParentMealTime()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var mealTimeId = Guid.NewGuid();
            await SeedMealTimeAsync(dbContext, mealTimeId, Guid.NewGuid());
            var _sut = new ProjectAssignedRecipeModelWhenRecipeRemovedFromMealTime(dbContext, _loggerMock.Object);

            // Act
            await _sut.Handle(new OutboxMessage<RecipeRemovedFromMealTime>(
                new RecipeRemovedFromMealTime(Guid.NewGuid(), 1, mealTimeId, Guid.NewGuid())));

            // Assert
            Assert.Equal(1, await MealTimesAsync(dbContext, mealTimeId));
        }

        [Fact]
        public async Task Handle_WhenAssignmentIsRemoved_LeavesTheOtherAssignmentsInPlace()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var mealTimeId = Guid.NewGuid();
            var removedRecipeId = Guid.NewGuid();
            var keptRecipeId = Guid.NewGuid();
            await SeedMealTimeAsync(dbContext, mealTimeId, removedRecipeId, keptRecipeId);
            var _sut = new ProjectAssignedRecipeModelWhenRecipeRemovedFromMealTime(dbContext, _loggerMock.Object);

            // Act
            await _sut.Handle(new OutboxMessage<RecipeRemovedFromMealTime>(
                new RecipeRemovedFromMealTime(Guid.NewGuid(), 1, mealTimeId, removedRecipeId)));

            // Assert
            Assert.Equal(keptRecipeId, Assert.Single(await AssignmentsAsync(dbContext, mealTimeId)).RecipeId);
        }

        [Fact]
        public async Task Handle_WhenSameMessageIsProjectedTwice_KeepsTheParentMealTime()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var mealTimeId = Guid.NewGuid();
            var recipeId = Guid.NewGuid();
            await SeedMealTimeAsync(dbContext, mealTimeId, recipeId);
            var _sut = new ProjectAssignedRecipeModelWhenRecipeRemovedFromMealTime(dbContext, _loggerMock.Object);
            var recipeRemoved = new RecipeRemovedFromMealTime(Guid.NewGuid(), 1, mealTimeId, recipeId);

            // Act
            await _sut.Handle(new OutboxMessage<RecipeRemovedFromMealTime>(recipeRemoved));
            await _sut.Handle(new OutboxMessage<RecipeRemovedFromMealTime>(recipeRemoved));

            // Assert
            Assert.Equal(1, await MealTimesAsync(dbContext, mealTimeId));
        }

        [Fact]
        public async Task Handle_WhenMealTimeIsNotProjected_DoesNotThrow()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var _sut = new ProjectAssignedRecipeModelWhenRecipeRemovedFromMealTime(dbContext, _loggerMock.Object);

            // Act
            var act = () => _sut.Handle(new OutboxMessage<RecipeRemovedFromMealTime>(
                new RecipeRemovedFromMealTime(Guid.NewGuid(), 1, Guid.NewGuid(), Guid.NewGuid())));

            // Assert
            await act();
        }

        private static Task<List<AssignedRecipeModel>> AssignmentsAsync(StoredDbContext dbContext, Guid mealTimeId)
        {
            return dbContext.MealPlans
                .Include(x => x.PlanDays)
                    .ThenInclude(x => x.MealTimes)
                        .ThenInclude(x => x.AssignedRecipes)
                .SelectMany(x => x.PlanDays)
                .SelectMany(x => x.MealTimes)
                .Where(x => x.Id == mealTimeId)
                .SelectMany(x => x.AssignedRecipes)
                .ToListAsync();
        }

        private static Task<int> MealTimesAsync(StoredDbContext dbContext, Guid mealTimeId)
        {
            return dbContext.MealPlans
                .Include(x => x.PlanDays)
                    .ThenInclude(x => x.MealTimes)
                .SelectMany(x => x.PlanDays)
                .SelectMany(x => x.MealTimes)
                .CountAsync(x => x.Id == mealTimeId);
        }

        private static async Task SeedMealTimeAsync(StoredDbContext dbContext, Guid mealTimeId, params Guid[] recipeIds)
        {
            var plan = new MealPlanModel
            {
                Id = Guid.NewGuid(),
                Name = "Plan Saludable",
                DurationType = "FIFTEEN_DAYS",
                MealsPerDay = 5,
                CreatedAt = DateTime.UtcNow
            };
            var planDay = new PlanDayModel
            {
                Id = Guid.NewGuid(),
                DayNumber = 1,
                MealPlanId = plan.Id,
                CreatedAt = DateTime.UtcNow
            };
            var mealTime = new MealTimeModel
            {
                Id = mealTimeId,
                Name = "Breakfast",
                Order = 1,
                PlanDayId = planDay.Id,
                CreatedAt = DateTime.UtcNow
            };
            foreach (var recipeId in recipeIds)
            {
                mealTime.AssignedRecipes.Add(new AssignedRecipeModel
                {
                    RecipeId = recipeId,
                    RationQuantity = 1,
                    MealTimeId = mealTimeId
                });
            }
            planDay.MealTimes.Add(mealTime);
            plan.PlanDays.Add(planDay);
            dbContext.MealPlans.Add(plan);
            await dbContext.SaveChangesAsync();
        }
    }
}
