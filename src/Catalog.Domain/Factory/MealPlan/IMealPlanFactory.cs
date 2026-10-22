using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Factory.MealPlan
{
    using MealPlan = Catalog.Domain.Model.MealPlans.MealPlan;

    public interface IMealPlanFactory
    {
        /// <summary>Creates a meal plan owned by an account.</summary>
        /// <param name="authorId">
        /// The identity service's user id for the owner, taken from the caller's bearer token and
        /// never from the request body.
        /// </param>
        MealPlan Create(string name, PlanDuration duration, int mealsPerDay, Guid authorId);

        /// <summary>
        /// Creates a meal plan with no author.
        /// </summary>
        /// <remarks>
        /// For the seeded sample catalogue only. The command side never calls this: a plan a real
        /// user creates always has an author, so the null case is a system plan and saying so
        /// explicitly keeps the rule visible in the code.
        /// </remarks>
        MealPlan CreateWithoutAuthor(string name, PlanDuration duration, int mealsPerDay);
    }
}