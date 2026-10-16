using Joseco.DDD.Core.Abstractions;

namespace Catalog.Domain.Event.MealPlan
{
    public record RecipeAssignedToMealTime : DomainEvent
    {
        public Guid PlanId { get; }
        public int DayNumber { get; }
        public Guid MealTimeId { get; }
        public Guid RecipeId { get; }
        public int RationQuantity { get; }

        public RecipeAssignedToMealTime(Guid planId, int dayNumber, Guid mealTimeId, Guid recipeId, int rationQuantity)
        {
            OccuredOn = DateTime.UtcNow;
            PlanId = planId;
            DayNumber = dayNumber;
            MealTimeId = mealTimeId;
            RecipeId = recipeId;
            RationQuantity = rationQuantity;
        }
    }
}
