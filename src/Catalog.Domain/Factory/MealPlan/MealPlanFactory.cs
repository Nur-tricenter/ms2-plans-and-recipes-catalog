using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Factory.MealPlan
{
    using MealPlan = Catalog.Domain.Model.MealPlans.MealPlan;

    public class MealPlanFactory : IMealPlanFactory
    {
        public MealPlan Create(string name, PlanDuration duration, int mealsPerDay, Guid authorId) =>
            new(name, duration, mealsPerDay, PlanAuthorId.Create(authorId));

        public MealPlan CreateWithoutAuthor(string name, PlanDuration duration, int mealsPerDay) =>
            new(name, duration, mealsPerDay, authorId: null);
    }
}