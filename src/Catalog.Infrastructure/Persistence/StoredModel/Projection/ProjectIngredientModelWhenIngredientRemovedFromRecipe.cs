using Catalog.Domain.Event.Recipe;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Persistence.StoredModel.Projection
{
    /// <summary>
    /// Removes the stored ingredient row identified by the event's natural key (recipe, food).
    /// A replay finds nothing to remove and returns without failing the outbox batch.
    /// </summary>
    public class ProjectIngredientModelWhenIngredientRemovedFromRecipe
        : INotificationHandler<OutboxMessage<IngredientRemovedFromRecipe>>
    {
        private readonly StoredDbContext _dbContext;
        private readonly ILogger<ProjectIngredientModelWhenIngredientRemovedFromRecipe> _logger;

        public ProjectIngredientModelWhenIngredientRemovedFromRecipe(
            StoredDbContext dbContext,
            ILogger<ProjectIngredientModelWhenIngredientRemovedFromRecipe> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Handle(OutboxMessage<IngredientRemovedFromRecipe> notification, CancellationToken cancellationToken = default)
        {
            var ingredientRemoved = notification.Content;

            var recipe = await _dbContext.Recipes
                .Include(x => x.Ingredients)
                .FirstOrDefaultAsync(x => x.Id == ingredientRemoved.RecipeId, cancellationToken);

            if (recipe is null)
            {
                _logger.LogWarning(
                    "Skipping ingredient removal: no stored recipe found for {RecipeId}",
                    ingredientRemoved.RecipeId);
                return;
            }

            var ingredient = recipe.Ingredients.FirstOrDefault(x => x.FoodId == ingredientRemoved.FoodId);

            if (ingredient is null)
            {
                _logger.LogWarning(
                    "Skipping ingredient removal: food {FoodId} is not projected for recipe {RecipeId}",
                    ingredientRemoved.FoodId, ingredientRemoved.RecipeId);
                return;
            }

            recipe.Ingredients.Remove(ingredient);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
