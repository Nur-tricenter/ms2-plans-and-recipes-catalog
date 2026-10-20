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

        /// <remarks>
        /// This is the ONLY constructor on purpose. A public parameterless constructor would make
        /// the outbox's Newtonsoft deserializer build the event through it and then skip every
        /// get-only payload property, so the event would silently arrive empty. See
        /// <c>FoodCreated</c> and
        /// <c>Test/UnitTest/Domain/Events/DomainEventOutboxSerializationTests</c>.
        /// </remarks>
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
