using Joseco.DDD.Core.Abstractions;

namespace Catalog.Domain.Repository.Food
{
    using Food = Catalog.Domain.Model.Foods.Food;

    public interface IFoodRepository : IRepository<Food>
    {
        Task UpdateAsync(Food food);
        Task RemoveAsync(Food food);
    }
}
