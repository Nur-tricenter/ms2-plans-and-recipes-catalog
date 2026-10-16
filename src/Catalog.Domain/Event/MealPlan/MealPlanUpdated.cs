using Joseco.DDD.Core.Abstractions;

namespace Catalog.Domain.Event.MealPlan
{
    public record MealPlanUpdated : DomainEvent
    {
        public Guid PlanId { get; }
        public string Name { get; }
        public string DurationType { get; }
        public int TotalDays { get; }
        public int MealsPerDay { get; }

        public MealPlanUpdated() : this(Guid.Empty, string.Empty, string.Empty, 0, 0)
        {
        }

        public MealPlanUpdated(Guid planId, string name, string durationType, int totalDays, int mealsPerDay)
        {
            OccuredOn = DateTime.UtcNow;
            PlanId = planId;
            Name = name;
            DurationType = durationType;
            TotalDays = totalDays;
            MealsPerDay = mealsPerDay;
        }
    }
}
