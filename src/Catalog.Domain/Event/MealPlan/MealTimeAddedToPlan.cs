using Joseco.DDD.Core.Abstractions;

namespace Catalog.Domain.Event.MealPlan
{
    public record MealTimeAddedToPlan : DomainEvent
    {
        public Guid PlanId { get; }
        public int DayNumber { get; }
        public Guid MealTimeId { get; }
        public string MealTimeType { get; }
        public int Order { get; }

        public MealTimeAddedToPlan(Guid planId, int dayNumber, Guid mealTimeId, string mealTimeType, int order)
        {
            OccuredOn = DateTime.UtcNow;
            PlanId = planId;
            DayNumber = dayNumber;
            MealTimeId = mealTimeId;
            MealTimeType = mealTimeType;
            Order = order;
        }
    }
}
