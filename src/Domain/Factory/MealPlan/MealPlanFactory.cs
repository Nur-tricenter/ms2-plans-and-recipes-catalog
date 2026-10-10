using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Factory.MealPlan
{
    using MealPlan = Catalog.Domain.Model.MealPlans.MealPlan;

    public class MealPlanFactory : IMealPlanFactory
    {
        public MealPlan Create(string name, PlanDuration duration, int mealsPerDay)
        {
            return new MealPlan(name, duration, mealsPerDay);
        }
    }
}