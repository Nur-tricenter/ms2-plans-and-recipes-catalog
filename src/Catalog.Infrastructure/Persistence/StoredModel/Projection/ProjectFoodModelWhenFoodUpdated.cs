using Catalog.Domain.Event.Food;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Persistence.StoredModel.Projection
{
    /// <summary>
    /// Applies <see cref="FoodUpdated"/> to the existing stored food row.
    /// The event carries all updated fields; every field is written, and the row is located by
    /// primary key so a replay updates in place instead of inserting.
    /// </summary>
    public class ProjectFoodModelWhenFoodUpdated
        : INotificationHandler<OutboxMessage<FoodUpdated>>
    {
        private readonly StoredDbContext _dbContext;
        private readonly ILogger<ProjectFoodModelWhenFoodUpdated> _logger;

        public ProjectFoodModelWhenFoodUpdated(
            StoredDbContext dbContext,
            ILogger<ProjectFoodModelWhenFoodUpdated> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Handle(OutboxMessage<FoodUpdated> notification, CancellationToken cancellationToken = default)
        {
            var foodUpdated = notification.Content;

            var model = await _dbContext.Foods.FindAsync(new object?[] { foodUpdated.FoodId }, cancellationToken);

            if (model is null)
            {
                _logger.LogWarning(
                    "Skipping food update projection: no stored food found for {FoodId}",
                    foodUpdated.FoodId);
                return;
            }

            model.Name = foodUpdated.Name;
            model.Category = foodUpdated.Category;
            model.UnitOfMeasure = (int)foodUpdated.UnitOfMeasure;
            model.Quantity = foodUpdated.NewNutritionalInfo.Quantity;
            model.Calories = foodUpdated.NewNutritionalInfo.Calories;
            model.Protein = foodUpdated.NewNutritionalInfo.Protein;
            model.Carbs = foodUpdated.NewNutritionalInfo.Carbs;
            model.Fat = foodUpdated.NewNutritionalInfo.Fat;
            model.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
