using Catalog.Domain.Event.Recipe;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.Recipe;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.StoredModel.Projection
{
    /// <summary>
    /// Projects <see cref="RecipeCreated"/> into the stored/read side.
    /// Upsert by primary key: inserts the row the first time and refreshes the
    /// same row when the same outbox message is delivered again.
    /// </summary>
    /// <remarks>
    /// The projected row also carries the instructions carried by <see cref="RecipeCreated"/>, so a
    /// created recipe is readable from the read side before any <see cref="RecipeUpdated"/> arrives.
    /// </remarks>
    public class ProjectRecipeModelWhenRecipeCreated : INotificationHandler<OutboxMessage<RecipeCreated>>
    {
        private readonly StoredDbContext _dbContext;

        public ProjectRecipeModelWhenRecipeCreated(StoredDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Handle(OutboxMessage<RecipeCreated> notification, CancellationToken cancellationToken = default)
        {
            var recipeCreated = notification.Content;

            var model = await _dbContext.Recipes.FindAsync(new object?[] { recipeCreated.RecipeId }, cancellationToken);

            if (model is null)
            {
                _dbContext.Recipes.Add(new RecipeModel
                {
                    Id = recipeCreated.RecipeId,
                    Name = recipeCreated.Name,
                    Instructions = recipeCreated.Instructions,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });
            }
            else
            {
                model.Name = recipeCreated.Name;
                model.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
