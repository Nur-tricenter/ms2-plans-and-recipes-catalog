using Catalog.Domain.Event.MealPlan;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Persistence.StoredModel.Projection
{
    /// <summary>
    /// Applies <see cref="MealPlanUpdated"/> to the existing stored plan row, located by primary key,
    /// so a replay updates in place instead of inserting a second row.
    /// </summary>
    /// <remarks>
    /// Only the scalar plan fields are refreshed. The event's <see cref="MealPlanUpdated.TotalDays"/>
    /// is not used to add or drop plan day rows: <see cref="MealPlan.Update"/> on the aggregate does
    /// not change its days either, and dropping projected rows here would lose read data.
    /// </remarks>
    public class ProjectMealPlanModelWhenMealPlanUpdated : INotificationHandler<OutboxMessage<MealPlanUpdated>>
    {
        private readonly StoredDbContext _dbContext;
        private readonly ILogger<ProjectMealPlanModelWhenMealPlanUpdated> _logger;

        public ProjectMealPlanModelWhenMealPlanUpdated(
            StoredDbContext dbContext,
            ILogger<ProjectMealPlanModelWhenMealPlanUpdated> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Handle(OutboxMessage<MealPlanUpdated> notification, CancellationToken cancellationToken = default)
        {
            var planUpdated = notification.Content;

            var model = await _dbContext.MealPlans.FindAsync(new object?[] { planUpdated.PlanId }, cancellationToken);

            if (model is null)
            {
                _logger.LogWarning(
                    "Skipping meal plan projection: no stored plan found for {PlanId}",
                    planUpdated.PlanId);
                return;
            }

            model.Name = planUpdated.Name;
            model.DurationType = planUpdated.DurationType;
            model.MealsPerDay = planUpdated.MealsPerDay;
            model.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
