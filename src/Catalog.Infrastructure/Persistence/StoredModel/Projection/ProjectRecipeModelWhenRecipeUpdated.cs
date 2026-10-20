using Catalog.Domain.Event.Recipe;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Persistence.StoredModel.Projection
{
    /// <summary>
    /// Applies <see cref="RecipeUpdated"/> to the existing stored recipe row, located by primary key,
    /// so a replay updates in place instead of inserting a second row.
    /// </summary>
    public class ProjectRecipeModelWhenRecipeUpdated : INotificationHandler<OutboxMessage<RecipeUpdated>>
    {
        private readonly StoredDbContext _dbContext;
        private readonly ILogger<ProjectRecipeModelWhenRecipeUpdated> _logger;

        public ProjectRecipeModelWhenRecipeUpdated(
            StoredDbContext dbContext,
            ILogger<ProjectRecipeModelWhenRecipeUpdated> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Handle(OutboxMessage<RecipeUpdated> notification, CancellationToken cancellationToken = default)
        {
            var recipeUpdated = notification.Content;

            var model = await _dbContext.Recipes.FindAsync(new object?[] { recipeUpdated.RecipeId }, cancellationToken);

            if (model is null)
            {
                _logger.LogWarning(
                    "Skipping recipe projection: no stored recipe found for {RecipeId}",
                    recipeUpdated.RecipeId);
                return;
            }

            model.Name = recipeUpdated.Name;
            model.Instructions = recipeUpdated.Instructions;
            model.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
