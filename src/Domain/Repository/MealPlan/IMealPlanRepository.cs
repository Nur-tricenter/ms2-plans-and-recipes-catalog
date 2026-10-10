using Shared.Core;

namespace Catalog.Domain.Repository.MealPlan
{
    using MealPlan = Catalog.Domain.Model.MealPlans.MealPlan;

    public interface IMealPlanRepository : IRepository<MealPlan, Guid>
    {
        Task UpdateAsync(MealPlan mealPlan);
        Task RemoveAsync(MealPlan mealPlan);
    }
}