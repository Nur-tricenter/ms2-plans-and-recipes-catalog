using Catalog.Domain.Event.MealPlan;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.MealPlan;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Persistence.StoredModel.Projection
{
    /// <summary>
    /// Projects <see cref="RecipeAssignedToMealTime"/> into the recipe assignment table.
    /// The read model has a generated primary key, so the natural key (meal time, recipe) is used to
    /// upsert: a replay updates the ration quantity instead of inserting a duplicate row.
    /// </summary>
    public class ProjectRecipeAssignmentModelWhenRecipeAssignedToMealTime
        : INotificationHandler<OutboxMessage<RecipeAssignedToMealTime>>
    {
        private readonly StoredDbContext _dbContext;
        private readonly ILogger<ProjectRecipeAssignmentModelWhenRecipeAssignedToMealTime> _logger;

        public ProjectRecipeAssignmentModelWhenRecipeAssignedToMealTime(
            StoredDbContext dbContext,
            ILogger<ProjectRecipeAssignmentModelWhenRecipeAssignedToMealTime> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Handle(OutboxMessage<RecipeAssignedToMealTime> notification, CancellationToken cancellationToken = default)
        {
            var recipeAssigned = notification.Content;

            var mealTime = await FindMealTimeAsync(recipeAssigned.MealTimeId, cancellationToken);

            if (mealTime is null)
            {
                _logger.LogWarning(
                    "Skipping recipe assignment: no stored meal time found for {MealTimeId}",
                    recipeAssigned.MealTimeId);
                return;
            }

            var assignment = mealTime.AssignedRecipes
                .FirstOrDefault(x => x.RecipeId == recipeAssigned.RecipeId);

            if (assignment is null)
            {
                mealTime.AssignedRecipes.Add(new RecipeAssignmentModel
                {
                    RecipeId = recipeAssigned.RecipeId,
                    RationQuantity = recipeAssigned.RationQuantity,
                    MealTimeId = mealTime.Id
                });
            }
            else
            {
                assignment.RationQuantity = recipeAssigned.RationQuantity;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private Task<MealTimeModel?> FindMealTimeAsync(Guid mealTimeId, CancellationToken cancellationToken)
        {
            return _dbContext.MealPlans
                .Include(x => x.PlanDays)
                    .ThenInclude(x => x.MealTimes)
                        .ThenInclude(x => x.AssignedRecipes)
                .SelectMany(x => x.PlanDays)
                .SelectMany(x => x.MealTimes)
                .FirstOrDefaultAsync(x => x.Id == mealTimeId, cancellationToken);
        }
    }
}
