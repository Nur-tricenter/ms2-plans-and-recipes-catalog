using Catalog.Domain.Event.Recipe;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.Recipe;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.StoredModel.Projection
{
    /// <summary>
    /// Projects <see cref="IngredientAddedToRecipe"/> into the recipe ingredient table.
    /// The read model has a generated primary key, so the natural key (recipe, food) is used to
    /// upsert: a replay updates the portion quantity instead of inserting a duplicate row.
    /// </summary>
    public class ProjectIngredientModelWhenIngredientAddedToRecipe
        : INotificationHandler<OutboxMessage<IngredientAddedToRecipe>>
    {
        private readonly StoredDbContext _dbContext;

        public ProjectIngredientModelWhenIngredientAddedToRecipe(StoredDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Handle(OutboxMessage<IngredientAddedToRecipe> notification, CancellationToken cancellationToken = default)
        {
            var ingredientAdded = notification.Content;

            var recipe = await _dbContext.Recipes
                .Include(x => x.Ingredients)
                .FirstOrDefaultAsync(x => x.Id == ingredientAdded.RecipeId, cancellationToken);

            if (recipe is null)
                return;

            var ingredient = recipe.Ingredients.FirstOrDefault(x => x.FoodId == ingredientAdded.FoodId);

            if (ingredient is null)
            {
                recipe.Ingredients.Add(new IngredientModel
                {
                    FoodId = ingredientAdded.FoodId,
                    PortionQuantity = ingredientAdded.PortionQuantity,
                    RecipeId = recipe.Id
                });
            }
            else
            {
                ingredient.PortionQuantity = ingredientAdded.PortionQuantity;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
