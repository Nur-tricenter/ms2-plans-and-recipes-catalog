using Joseco.DDD.Core.Abstractions;

namespace Catalog.Domain.Repository.MealPlan
{
    using MealPlan = Catalog.Domain.Model.MealPlans.MealPlan;

    public interface IMealPlanRepository : IRepository<MealPlan>
    {
        Task UpdateAsync(MealPlan mealPlan);
        Task RemoveAsync(MealPlan mealPlan);
    }
}
