using Catalog.Domain.Event.Food;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Persistence.StoredModel.Projection
{
    /// <summary>
    /// Applies <see cref="FoodNutritionalInfoUpdated"/> to the existing stored food row.
    /// The event carries both the previous and the new nutritional info; only the new one is written,
    /// and the row is located by primary key so a replay updates in place instead of inserting.
    /// </summary>
    public class ProjectFoodModelWhenFoodNutritionalInfoUpdated
        : INotificationHandler<OutboxMessage<FoodNutritionalInfoUpdated>>
    {
        private readonly StoredDbContext _dbContext;
        private readonly ILogger<ProjectFoodModelWhenFoodNutritionalInfoUpdated> _logger;

        public ProjectFoodModelWhenFoodNutritionalInfoUpdated(
            StoredDbContext dbContext,
            ILogger<ProjectFoodModelWhenFoodNutritionalInfoUpdated> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Handle(OutboxMessage<FoodNutritionalInfoUpdated> notification, CancellationToken cancellationToken = default)
        {
            var foodUpdated = notification.Content;

            var model = await _dbContext.Foods.FindAsync(new object?[] { foodUpdated.FoodId }, cancellationToken);

            if (model is null)
            {
                _logger.LogWarning(
                    "Skipping nutritional info projection: no stored food found for {FoodId}",
                    foodUpdated.FoodId);
                return;
            }

            var nutritionalInfo = foodUpdated.NewNutritionalInfo;

            model.Quantity = nutritionalInfo.Quantity;
            model.Calories = nutritionalInfo.Calories;
            model.Protein = nutritionalInfo.Protein;
            model.Carbs = nutritionalInfo.Carbs;
            model.Fat = nutritionalInfo.Fat;
            model.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
