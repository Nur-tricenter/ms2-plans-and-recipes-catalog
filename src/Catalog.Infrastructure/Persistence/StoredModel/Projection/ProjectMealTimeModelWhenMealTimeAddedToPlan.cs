using Catalog.Domain.Event.MealPlan;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.MealPlan;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Persistence.StoredModel.Projection
{
    /// <summary>
    /// Projects <see cref="MealTimeAddedToPlan"/> into the stored/read side.
    /// The meal time itself is an upsert by primary key. Its parent plan day is not identified by id
    /// in the event, only by (plan, day number), so the day row is looked up by that natural key and
    /// created when absent. Re-running the message updates in place and reuses the existing day row.
    /// </summary>
    public class ProjectMealTimeModelWhenMealTimeAddedToPlan
        : INotificationHandler<OutboxMessage<MealTimeAddedToPlan>>
    {
        private readonly StoredDbContext _dbContext;
        private readonly ILogger<ProjectMealTimeModelWhenMealTimeAddedToPlan> _logger;

        public ProjectMealTimeModelWhenMealTimeAddedToPlan(
            StoredDbContext dbContext,
            ILogger<ProjectMealTimeModelWhenMealTimeAddedToPlan> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Handle(OutboxMessage<MealTimeAddedToPlan> notification, CancellationToken cancellationToken = default)
        {
            var mealTimeAdded = notification.Content;

            var plan = await _dbContext.MealPlans
                .Include(x => x.PlanDays)
                    .ThenInclude(x => x.MealTimes)
                .FirstOrDefaultAsync(x => x.Id == mealTimeAdded.PlanId, cancellationToken);

            if (plan is null)
            {
                _logger.LogWarning(
                    "Skipping meal time projection: no stored plan found for {PlanId}",
                    mealTimeAdded.PlanId);
                return;
            }

            var planDay = plan.PlanDays.FirstOrDefault(x => x.DayNumber == mealTimeAdded.DayNumber);

            if (planDay is null)
            {
                planDay = new PlanDayModel
                {
                    Id = Guid.NewGuid(),
                    DayNumber = mealTimeAdded.DayNumber,
                    MealPlanId = plan.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                plan.PlanDays.Add(planDay);
            }

            var mealTime = plan.PlanDays
                .SelectMany(x => x.MealTimes)
                .FirstOrDefault(x => x.Id == mealTimeAdded.MealTimeId);

            if (mealTime is null)
            {
                planDay.MealTimes.Add(new MealTimeModel
                {
                    Id = mealTimeAdded.MealTimeId,
                    Name = mealTimeAdded.MealTimeType,
                    Order = mealTimeAdded.Order,
                    PlanDayId = planDay.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });
            }
            else
            {
                mealTime.Name = mealTimeAdded.MealTimeType;
                mealTime.Order = mealTimeAdded.Order;
                mealTime.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
