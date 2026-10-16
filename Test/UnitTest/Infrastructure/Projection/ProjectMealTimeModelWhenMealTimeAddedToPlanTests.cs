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
    public class ProjectMealTimeModelWhenMealTimeAddedToPlanTests
    {
        private readonly Mock<ILogger<ProjectMealTimeModelWhenMealTimeAddedToPlan>> _loggerMock;

        public ProjectMealTimeModelWhenMealTimeAddedToPlanTests()
        {
            _loggerMock = new Mock<ILogger<ProjectMealTimeModelWhenMealTimeAddedToPlan>>();
        }

        [Fact]
        public async Task Handle_WhenPlanDayExists_AddsTheMealTimeToIt()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var planId = Guid.NewGuid();
            await SeedPlanAsync(dbContext, planId, 1);
            var _sut = new ProjectMealTimeModelWhenMealTimeAddedToPlan(dbContext, _loggerMock.Object);

            // Act
            await _sut.Handle(new OutboxMessage<MealTimeAddedToPlan>(
                new MealTimeAddedToPlan(planId, 1, Guid.NewGuid(), "Breakfast", 1)));

            // Assert
            var mealTime = await SingleMealTimeAsync(dbContext, planId, 1);
            Assert.Equal("Breakfast", mealTime.Name);
            Assert.Equal(1, mealTime.Order);
        }

        [Fact]
        public async Task Handle_WhenPlanDayIsMissing_CreatesThePlanDay()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var planId = Guid.NewGuid();
            dbContext.MealPlans.Add(NewPlan(planId));
            await dbContext.SaveChangesAsync();
            var _sut = new ProjectMealTimeModelWhenMealTimeAddedToPlan(dbContext, _loggerMock.Object);

            // Act
            await _sut.Handle(new OutboxMessage<MealTimeAddedToPlan>(
                new MealTimeAddedToPlan(planId, 3, Guid.NewGuid(), "Dinner", 5)));

            // Assert
            var stored = await dbContext.MealPlans.Include(x => x.PlanDays).SingleAsync();
            Assert.Equal(3, Assert.Single(stored.PlanDays).DayNumber);
        }

        [Fact]
        public async Task Handle_WhenPlanDayIsMissing_AttachesTheMealTimeToTheCreatedPlanDay()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var planId = Guid.NewGuid();
            dbContext.MealPlans.Add(NewPlan(planId));
            await dbContext.SaveChangesAsync();
            var _sut = new ProjectMealTimeModelWhenMealTimeAddedToPlan(dbContext, _loggerMock.Object);

            // Act
            await _sut.Handle(new OutboxMessage<MealTimeAddedToPlan>(
                new MealTimeAddedToPlan(planId, 3, Guid.NewGuid(), "Dinner", 5)));

            // Assert
            var mealTime = await SingleMealTimeAsync(dbContext, planId, 3);
            Assert.NotEqual(Guid.Empty, mealTime.PlanDayId);
        }

        [Fact]
        public async Task Handle_WhenSameMessageIsProjectedTwice_KeepsOneMealTime()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var planId = Guid.NewGuid();
            await SeedPlanAsync(dbContext, planId, 1);
            var _sut = new ProjectMealTimeModelWhenMealTimeAddedToPlan(dbContext, _loggerMock.Object);
            var mealTimeAdded = new MealTimeAddedToPlan(planId, 1, Guid.NewGuid(), "Breakfast", 1);

            // Act
            await _sut.Handle(new OutboxMessage<MealTimeAddedToPlan>(mealTimeAdded));
            await _sut.Handle(new OutboxMessage<MealTimeAddedToPlan>(mealTimeAdded));

            // Assert
            var stored = await dbContext.MealPlans
                .Include(x => x.PlanDays)
                    .ThenInclude(x => x.MealTimes)
                .SingleAsync();
            Assert.Single(stored.PlanDays.SelectMany(x => x.MealTimes));
        }

        [Fact]
        public async Task Handle_WhenSameMessageIsProjectedTwice_DoesNotDuplicateThePlanDay()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var planId = Guid.NewGuid();
            dbContext.MealPlans.Add(NewPlan(planId));
            await dbContext.SaveChangesAsync();
            var _sut = new ProjectMealTimeModelWhenMealTimeAddedToPlan(dbContext, _loggerMock.Object);
            var mealTimeAdded = new MealTimeAddedToPlan(planId, 3, Guid.NewGuid(), "Dinner", 5);
            await _sut.Handle(new OutboxMessage<MealTimeAddedToPlan>(mealTimeAdded));

            // Act
            await _sut.Handle(new OutboxMessage<MealTimeAddedToPlan>(mealTimeAdded));

            // Assert
            var stored = await dbContext.MealPlans.Include(x => x.PlanDays).SingleAsync();
            Assert.Single(stored.PlanDays);
        }

        [Fact]
        public async Task Handle_WhenPlanIsNotProjected_DoesNotInsertAMealTime()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var _sut = new ProjectMealTimeModelWhenMealTimeAddedToPlan(dbContext, _loggerMock.Object);

            // Act
            await _sut.Handle(new OutboxMessage<MealTimeAddedToPlan>(
                new MealTimeAddedToPlan(Guid.NewGuid(), 1, Guid.NewGuid(), "Breakfast", 1)));

            // Assert
            Assert.Equal(0, await dbContext.MealPlans.CountAsync());
        }

        private static async Task<MealTimeModel> SingleMealTimeAsync(
            StoredDbContext dbContext, Guid planId, int dayNumber)
        {
            var stored = await dbContext.MealPlans
                .Include(x => x.PlanDays)
                    .ThenInclude(x => x.MealTimes)
                .SingleAsync();

            return Assert.Single(stored.PlanDays.Where(x => x.DayNumber == dayNumber).SelectMany(x => x.MealTimes));
        }

        private static async Task SeedPlanAsync(
            StoredDbContext dbContext, Guid planId, int dayNumber)
        {
            var plan = NewPlan(planId);
            plan.PlanDays.Add(new PlanDayModel
            {
                Id = Guid.NewGuid(),
                DayNumber = dayNumber,
                MealPlanId = planId,
                CreatedAt = DateTime.UtcNow
            });
            dbContext.MealPlans.Add(plan);
            await dbContext.SaveChangesAsync();
        }

        private static MealPlanModel NewPlan(Guid planId)
        {
            return new MealPlanModel
            {
                Id = planId,
                Name = "Plan Saludable",
                DurationType = "FIFTEEN_DAYS",
                MealsPerDay = 5,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
