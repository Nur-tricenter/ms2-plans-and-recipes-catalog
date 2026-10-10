using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Factory.MealPlan
{
    using MealPlan = Catalog.Domain.Model.MealPlans.MealPlan;

    public interface IMealPlanFactory
    {
        MealPlan Create(string name, PlanDuration duration, int mealsPerDay);
    }
}