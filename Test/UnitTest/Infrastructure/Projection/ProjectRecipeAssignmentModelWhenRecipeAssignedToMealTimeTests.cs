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
    public class ProjectRecipeAssignmentModelWhenRecipeAssignedToMealTimeTests
    {
        private readonly Mock<ILogger<ProjectRecipeAssignmentModelWhenRecipeAssignedToMealTime>> _loggerMock;

        public ProjectRecipeAssignmentModelWhenRecipeAssignedToMealTimeTests()
        {
            _loggerMock = new Mock<ILogger<ProjectRecipeAssignmentModelWhenRecipeAssignedToMealTime>>();
        }

        [Fact]
        public async Task Handle_WhenMealTimeIsProjected_AddsTheAssignment()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var mealTimeId = Guid.NewGuid();
            var recipeId = Guid.NewGuid();
            await SeedMealTimeAsync(dbContext, mealTimeId);
            var _sut = new ProjectRecipeAssignmentModelWhenRecipeAssignedToMealTime(dbContext, _loggerMock.Object);

            // Act
            await _sut.Handle(new OutboxMessage<RecipeAssignedToMealTime>(
                new RecipeAssignedToMealTime(Guid.NewGuid(), 1, mealTimeId, recipeId, 2)));

            // Assert
            var assignment = Assert.Single(await AssignmentsAsync(dbContext, mealTimeId));
            Assert.Equal(recipeId, assignment.RecipeId);
            Assert.Equal(2, assignment.RationQuantity);
        }

        [Fact]
        public async Task Handle_WhenSameMessageIsProjectedTwice_KeepsOneAssignment()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var mealTimeId = Guid.NewGuid();
            await SeedMealTimeAsync(dbContext, mealTimeId);
            var _sut = new ProjectRecipeAssignmentModelWhenRecipeAssignedToMealTime(dbContext, _loggerMock.Object);
            var recipeAssigned = new RecipeAssignedToMealTime(Guid.NewGuid(), 1, mealTimeId, Guid.NewGuid(), 2);

            // Act
            await _sut.Handle(new OutboxMessage<RecipeAssignedToMealTime>(recipeAssigned));
            await _sut.Handle(new OutboxMessage<RecipeAssignedToMealTime>(recipeAssigned));

            // Assert
            Assert.Single(await AssignmentsAsync(dbContext, mealTimeId));
        }

        [Fact]
        public async Task Handle_WhenSameMessageIsProjectedTwice_RefreshesTheRationQuantity()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var mealTimeId = Guid.NewGuid();
            var recipeId = Guid.NewGuid();
            await SeedMealTimeAsync(dbContext, mealTimeId);
            var _sut = new ProjectRecipeAssignmentModelWhenRecipeAssignedToMealTime(dbContext, _loggerMock.Object);
            await _sut.Handle(new OutboxMessage<RecipeAssignedToMealTime>(
                new RecipeAssignedToMealTime(Guid.NewGuid(), 1, mealTimeId, recipeId, 2)));

            // Act
            await _sut.Handle(new OutboxMessage<RecipeAssignedToMealTime>(
                new RecipeAssignedToMealTime(Guid.NewGuid(), 1, mealTimeId, recipeId, 4)));

            // Assert
            Assert.Equal(4, Assert.Single(await AssignmentsAsync(dbContext, mealTimeId)).RationQuantity);
        }

        [Fact]
        public async Task Handle_WhenDifferentRecipeIsAssigned_KeepsBothAssignments()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var mealTimeId = Guid.NewGuid();
            var firstRecipeId = Guid.NewGuid();
            var secondRecipeId = Guid.NewGuid();
            await SeedMealTimeAsync(dbContext, mealTimeId);
            var _sut = new ProjectRecipeAssignmentModelWhenRecipeAssignedToMealTime(dbContext, _loggerMock.Object);
            await _sut.Handle(new OutboxMessage<RecipeAssignedToMealTime>(
                new RecipeAssignedToMealTime(Guid.NewGuid(), 1, mealTimeId, firstRecipeId, 1)));

            // Act
            await _sut.Handle(new OutboxMessage<RecipeAssignedToMealTime>(
                new RecipeAssignedToMealTime(Guid.NewGuid(), 1, mealTimeId, secondRecipeId, 1)));

            // Assert
            Assert.Equal(2, (await AssignmentsAsync(dbContext, mealTimeId)).Count);
        }

        [Fact]
        public async Task Handle_WhenMealTimeIsNotProjected_DoesNotThrow()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var _sut = new ProjectRecipeAssignmentModelWhenRecipeAssignedToMealTime(dbContext, _loggerMock.Object);

            // Act
            var act = () => _sut.Handle(new OutboxMessage<RecipeAssignedToMealTime>(
                new RecipeAssignedToMealTime(Guid.NewGuid(), 1, Guid.NewGuid(), Guid.NewGuid(), 1)));

            // Assert
            await act();
        }

        private static Task<List<RecipeAssignmentModel>> AssignmentsAsync(StoredDbContext dbContext, Guid mealTimeId)
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

        private static async Task SeedMealTimeAsync(StoredDbContext dbContext, Guid mealTimeId)
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
            planDay.MealTimes.Add(new MealTimeModel
            {
                Id = mealTimeId,
                Name = "Breakfast",
                Order = 1,
                PlanDayId = planDay.Id,
                CreatedAt = DateTime.UtcNow
            });
            plan.PlanDays.Add(planDay);
            dbContext.MealPlans.Add(plan);
            await dbContext.SaveChangesAsync();
        }
    }
}
