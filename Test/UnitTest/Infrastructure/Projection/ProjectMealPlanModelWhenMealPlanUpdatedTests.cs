using Catalog.Domain.Event.MealPlan;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.MealPlan;
using Catalog.Infrastructure.Persistence.StoredModel.Projection;
using Joseco.Outbox.Contracts.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Catalog.Tests.Infrastructure.Projection
{
    public class ProjectMealPlanModelWhenMealPlanUpdatedTests
    {
        private readonly Mock<ILogger<ProjectMealPlanModelWhenMealPlanUpdated>> _loggerMock;

        public ProjectMealPlanModelWhenMealPlanUpdatedTests()
        {
            _loggerMock = new Mock<ILogger<ProjectMealPlanModelWhenMealPlanUpdated>>();
        }

        [Fact]
        public async Task Handle_WhenPlanExists_UpdatesTheExistingModel()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var planId = Guid.NewGuid();
            dbContext.MealPlans.Add(NewPlan(planId, "Plan Saludable", "FIFTEEN_DAYS", 3));
            await dbContext.SaveChangesAsync();
            var _sut = new ProjectMealPlanModelWhenMealPlanUpdated(dbContext, _loggerMock.Object);

            // Act
            await _sut.Handle(new OutboxMessage<MealPlanUpdated>(
                new MealPlanUpdated(planId, "Plan Deportivo", "THIRTY_DAYS", 30, 5)));

            // Assert
            dbContext.ChangeTracker.Clear();
            var stored = await dbContext.MealPlans.SingleAsync();
            Assert.Equal("Plan Deportivo", stored.Name);
            Assert.Equal("THIRTY_DAYS", stored.DurationType);
            Assert.Equal(5, stored.MealsPerDay);
        }

        [Fact]
        public async Task Handle_WhenSameMessageIsProjectedTwice_KeepsOneMealPlan()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var planId = Guid.NewGuid();
            dbContext.MealPlans.Add(NewPlan(planId, "Plan Saludable", "FIFTEEN_DAYS", 3));
            await dbContext.SaveChangesAsync();
            var _sut = new ProjectMealPlanModelWhenMealPlanUpdated(dbContext, _loggerMock.Object);
            var planUpdated = new MealPlanUpdated(planId, "Plan Deportivo", "THIRTY_DAYS", 30, 5);

            // Act
            await _sut.Handle(new OutboxMessage<MealPlanUpdated>(planUpdated));
            await _sut.Handle(new OutboxMessage<MealPlanUpdated>(planUpdated));

            // Assert
            Assert.Equal(1, await dbContext.MealPlans.CountAsync());
        }

        [Fact]
        public async Task Handle_WhenPlanDoesNotExist_DoesNotInsertAMealPlan()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var _sut = new ProjectMealPlanModelWhenMealPlanUpdated(dbContext, _loggerMock.Object);

            // Act
            await _sut.Handle(new OutboxMessage<MealPlanUpdated>(
                new MealPlanUpdated(Guid.NewGuid(), "Plan Deportivo", "THIRTY_DAYS", 30, 5)));

            // Assert
            Assert.Equal(0, await dbContext.MealPlans.CountAsync());
        }

        [Fact]
        public async Task Handle_WhenPlanDoesNotExist_LeavesTheProjectedPlanDaysAlone()
        {
            // Arrange
            await using var dbContext = StoredDbContextFactory.Create();
            var planId = Guid.NewGuid();
            dbContext.MealPlans.Add(NewPlan(planId, "Plan Saludable", "FIFTEEN_DAYS", 3));
            await dbContext.SaveChangesAsync();
            var _sut = new ProjectMealPlanModelWhenMealPlanUpdated(dbContext, _loggerMock.Object);

            // Act
            await _sut.Handle(new OutboxMessage<MealPlanUpdated>(
                new MealPlanUpdated(Guid.NewGuid(), "Plan Deportivo", "THIRTY_DAYS", 30, 5)));

            // Assert
            var stored = await dbContext.MealPlans.Include(x => x.PlanDays).SingleAsync();
            Assert.Equal("Plan Saludable", stored.Name);
            Assert.Empty(stored.PlanDays);
        }

        private static MealPlanModel NewPlan(Guid planId, string name, string durationType, int mealsPerDay)
        {
            return new MealPlanModel
            {
                Id = planId,
                Name = name,
                DurationType = durationType,
                MealsPerDay = mealsPerDay,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
