using Joseco.DDD.Core.Abstractions;

namespace Catalog.Domain.Event.MealPlan
{
    public record RecipeRemovedFromMealTime : DomainEvent
    {
        public Guid PlanId { get; }
        public int DayNumber { get; }
        public Guid MealTimeId { get; }
        public Guid RecipeId { get; }

        public RecipeRemovedFromMealTime() : this(Guid.Empty, 0, Guid.Empty, Guid.Empty)
        {
        }

        public RecipeRemovedFromMealTime(Guid planId, int dayNumber, Guid mealTimeId, Guid recipeId)
        {
            OccuredOn = DateTime.UtcNow;
            PlanId = planId;
            DayNumber = dayNumber;
            MealTimeId = mealTimeId;
            RecipeId = recipeId;
        }
    }
}
