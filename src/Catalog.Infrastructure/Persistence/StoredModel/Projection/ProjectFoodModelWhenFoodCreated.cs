using Catalog.Domain.Event.Food;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.Food;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.StoredModel.Projection
{
    /// <summary>
    /// Projects <see cref="FoodCreated"/> into the stored/read side.
    /// Upsert by primary key: inserts the row the first time and refreshes the
    /// same row when the same outbox message is delivered again.
    /// </summary>
    public class ProjectFoodModelWhenFoodCreated : INotificationHandler<OutboxMessage<FoodCreated>>
    {
        private readonly StoredDbContext _dbContext;

        public ProjectFoodModelWhenFoodCreated(StoredDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Handle(OutboxMessage<FoodCreated> notification, CancellationToken cancellationToken = default)
        {
            var foodCreated = notification.Content;

            var model = await _dbContext.Foods.FindAsync(new object?[] { foodCreated.FoodId }, cancellationToken);

            if (model is null)
            {
                _dbContext.Foods.Add(new FoodModel
                {
                    Id = foodCreated.FoodId,
                    Name = foodCreated.Name,
                    Category = foodCreated.Category,
                    UnitOfMeasure = (int)foodCreated.UnitOfMeasure,
                    Quantity = foodCreated.NutritionalInfo.Quantity,
                    Calories = foodCreated.NutritionalInfo.Calories,
                    Protein = foodCreated.NutritionalInfo.Protein,
                    Carbs = foodCreated.NutritionalInfo.Carbs,
                    Fat = foodCreated.NutritionalInfo.Fat,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });
            }
            else
            {
                model.Name = foodCreated.Name;
                model.Category = foodCreated.Category;
                model.Quantity = foodCreated.NutritionalInfo.Quantity;
                model.Calories = foodCreated.NutritionalInfo.Calories;
                model.Protein = foodCreated.NutritionalInfo.Protein;
                model.Carbs = foodCreated.NutritionalInfo.Carbs;
                model.Fat = foodCreated.NutritionalInfo.Fat;
                model.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
