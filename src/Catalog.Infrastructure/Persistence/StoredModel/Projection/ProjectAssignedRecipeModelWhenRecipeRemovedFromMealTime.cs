using Catalog.Domain.Event.MealPlan;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Persistence.StoredModel.Projection
{
    /// <summary>
    /// Removes the stored assigned recipe row identified by the event's natural key (meal time, recipe).
    /// A replay finds nothing to remove and returns without failing the outbox batch.
    /// </summary>
    public class ProjectAssignedRecipeModelWhenRecipeRemovedFromMealTime
        : INotificationHandler<OutboxMessage<RecipeRemovedFromMealTime>>
    {
        private readonly StoredDbContext _dbContext;
        private readonly ILogger<ProjectAssignedRecipeModelWhenRecipeRemovedFromMealTime> _logger;

        public ProjectAssignedRecipeModelWhenRecipeRemovedFromMealTime(
            StoredDbContext dbContext,
            ILogger<ProjectAssignedRecipeModelWhenRecipeRemovedFromMealTime> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Handle(OutboxMessage<RecipeRemovedFromMealTime> notification, CancellationToken cancellationToken = default)
        {
            var recipeRemoved = notification.Content;

            var mealTime = await _dbContext.MealPlans
                .Include(x => x.PlanDays)
                    .ThenInclude(x => x.MealTimes)
                        .ThenInclude(x => x.AssignedRecipes)
                .SelectMany(x => x.PlanDays)
                .SelectMany(x => x.MealTimes)
                .FirstOrDefaultAsync(x => x.Id == recipeRemoved.MealTimeId, cancellationToken);

            if (mealTime is null)
            {
                _logger.LogWarning(
                    "Skipping recipe removal: no stored meal time found for {MealTimeId}",
                    recipeRemoved.MealTimeId);
                return;
            }

            var assignment = mealTime.AssignedRecipes
                .FirstOrDefault(x => x.RecipeId == recipeRemoved.RecipeId);

            if (assignment is null)
            {
                _logger.LogWarning(
                    "Skipping recipe removal: recipe {RecipeId} is not projected for meal time {MealTimeId}",
                    recipeRemoved.RecipeId, recipeRemoved.MealTimeId);
                return;
            }

            mealTime.AssignedRecipes.Remove(assignment);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
