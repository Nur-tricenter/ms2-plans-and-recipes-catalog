using Catalog.Domain.Event.MealPlan;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.MealPlan;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.StoredModel.Projection
{
    /// <summary>
    /// Projects <see cref="MealPlanCreated"/> into the stored/read side.
    /// Upsert by primary key for the plan itself, plus a reconcile of the plan day rows:
    /// the event carries <see cref="MealPlanCreated.TotalDays"/> but no plan day identifiers,
    /// so days 1..TotalDays are created only when they are missing. Re-running is a no-op.
    /// </summary>
    public class ProjectMealPlanModelWhenMealPlanCreated : INotificationHandler<OutboxMessage<MealPlanCreated>>
    {
        private readonly StoredDbContext _dbContext;

        public ProjectMealPlanModelWhenMealPlanCreated(StoredDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Handle(OutboxMessage<MealPlanCreated> notification, CancellationToken cancellationToken = default)
        {
            var planCreated = notification.Content;

            var model = await _dbContext.MealPlans
                .Include(x => x.PlanDays)
                .FirstOrDefaultAsync(x => x.Id == planCreated.PlanId, cancellationToken);

            if (model is null)
            {
                model = new MealPlanModel
                {
                    Id = planCreated.PlanId,
                    Name = planCreated.Name,
                    DurationType = planCreated.DurationType,
                    MealsPerDay = planCreated.MealsPerDay,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                _dbContext.MealPlans.Add(model);
            }
            else
            {
                model.Name = planCreated.Name;
                model.DurationType = planCreated.DurationType;
                model.MealsPerDay = planCreated.MealsPerDay;
                model.UpdatedAt = DateTime.UtcNow;
            }

            AddMissingPlanDays(model, planCreated.TotalDays);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private static void AddMissingPlanDays(MealPlanModel plan, int totalDays)
        {
            for (var dayNumber = 1; dayNumber <= totalDays; dayNumber++)
            {
                if (plan.PlanDays.Any(x => x.DayNumber == dayNumber))
                    continue;

                plan.PlanDays.Add(new PlanDayModel
                {
                    Id = Guid.NewGuid(),
                    DayNumber = dayNumber,
                    MealPlanId = plan.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });
            }
        }
    }
}
