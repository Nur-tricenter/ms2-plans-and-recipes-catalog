using Catalog.Domain.Event.MealPlan;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.MealPlan;
using Catalog.Infrastructure.Persistence.StoredModel.Projection;
using Joseco.Outbox.Contracts.Model;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Catalog.Tests.Infrastructure.Projection
{
    public class ProjectMealPlanModelWhenMealPlanCreatedTests
    {
        [Fact]
        public async Task Handle_WhenPlanDoesNotExist_InsertsMealPlanModel()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var _sut = new ProjectMealPlanModelWhenMealPlanCreated(dbContext);
            var planId = Guid.NewGuid();

            // Act
            await _sut.Handle(new OutboxMessage<MealPlanCreated>(
                new MealPlanCreated(planId, "Plan Saludable", "FIFTEEN_DAYS", 15, 5)));

            // Assert
            var stored = await dbContext.MealPlans.SingleAsync();
            Assert.Equal(planId, stored.Id);
            Assert.Equal("Plan Saludable", stored.Name);
            Assert.Equal("FIFTEEN_DAYS", stored.DurationType);
            Assert.Equal(5, stored.MealsPerDay);
        }

        [Fact]
        public async Task Handle_WhenPlanCreated_CreatesOnePlanDayPerTotalDays()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var _sut = new ProjectMealPlanModelWhenMealPlanCreated(dbContext);

            // Act
            await _sut.Handle(new OutboxMessage<MealPlanCreated>(
                new MealPlanCreated(Guid.NewGuid(), "Plan Saludable", "FIFTEEN_DAYS", 15, 5)));

            // Assert
            var stored = await dbContext.MealPlans.Include(x => x.PlanDays).SingleAsync();
            Assert.Equal(15, stored.PlanDays.Count);
            Assert.Equal(
                Enumerable.Range(1, 15),
                stored.PlanDays.Select(x => x.DayNumber).OrderBy(x => x));
        }

        [Fact]
        public async Task Handle_WhenSameMessageIsProjectedTwice_KeepsOneMealPlan()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var _sut = new ProjectMealPlanModelWhenMealPlanCreated(dbContext);
            var planCreated = new MealPlanCreated(Guid.NewGuid(), "Plan Saludable", "FIFTEEN_DAYS", 15, 5);

            // Act
            await _sut.Handle(new OutboxMessage<MealPlanCreated>(planCreated));
            await _sut.Handle(new OutboxMessage<MealPlanCreated>(planCreated));

            // Assert
            Assert.Equal(1, await dbContext.MealPlans.CountAsync());
        }

        [Fact]
        public async Task Handle_WhenSameMessageIsProjectedTwice_DoesNotDuplicatePlanDays()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var _sut = new ProjectMealPlanModelWhenMealPlanCreated(dbContext);
            var planCreated = new MealPlanCreated(Guid.NewGuid(), "Plan Saludable", "FIFTEEN_DAYS", 15, 5);
            await _sut.Handle(new OutboxMessage<MealPlanCreated>(planCreated));

            // Act
            await _sut.Handle(new OutboxMessage<MealPlanCreated>(planCreated));

            // Assert
            var stored = await dbContext.MealPlans.Include(x => x.PlanDays).SingleAsync();
            Assert.Equal(15, stored.PlanDays.Count);
        }

        [Fact]
        public async Task Handle_WhenPlanWasPreviouslyInserted_RefreshesTheExistingPlan()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var planId = Guid.NewGuid();
            var plan = new MealPlanModel
            {
                Id = planId,
                Name = "Old name",
                DurationType = "FIFTEEN_DAYS",
                MealsPerDay = 3,
                CreatedAt = DateTime.UtcNow
            };
            dbContext.MealPlans.Add(plan);
            await dbContext.SaveChangesAsync();
            var _sut = new ProjectMealPlanModelWhenMealPlanCreated(dbContext);

            // Act
            await _sut.Handle(new OutboxMessage<MealPlanCreated>(
                new MealPlanCreated(planId, "Plan Saludable", "THIRTY_DAYS", 30, 5)));

            // Assert
            var stored = await dbContext.MealPlans.Include(x => x.PlanDays).SingleAsync();
            Assert.Equal("Plan Saludable", stored.Name);
            Assert.Equal("THIRTY_DAYS", stored.DurationType);
            Assert.Equal(5, stored.MealsPerDay);
            Assert.Equal(30, stored.PlanDays.Count);
        }
    }
}
