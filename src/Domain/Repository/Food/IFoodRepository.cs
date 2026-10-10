using Shared.Core;

namespace Catalog.Domain.Repository.Food
{
    using Food = Catalog.Domain.Model.Foods.Food;

    public interface IFoodRepository : IRepository<Food, Guid>
    {
        Task UpdateAsync(Food food);
        Task RemoveAsync(Food food);
    }
}