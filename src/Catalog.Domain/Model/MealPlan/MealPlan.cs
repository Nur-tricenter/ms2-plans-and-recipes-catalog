using Joseco.DDD.Core.Abstractions;
using Catalog.Domain.Primitives;
using Catalog.Domain.ValueObjects;
using Catalog.Domain.Event.MealPlan;

namespace Catalog.Domain.Model.MealPlans
{
    public class MealPlan : AuditableAggregateRoot
    {
        public MealPlanName Name { get; private set; }
        public PlanDuration Duration { get; private set; }
        public int MealsPerDay { get; private set; }
        private readonly ICollection<PlanDay> _planDays;

        public IEnumerable<PlanDay> PlanDays => _planDays;

        private MealPlan() { Name = null!; Duration = null!; _planDays = null!; }

        public MealPlan(MealPlanName name, PlanDuration duration, int mealsPerDay)
        {
            if (!duration.IsValid())
                throw new BussinessRuleValidationException("Invalid plan duration");

            Id = Guid.NewGuid();
            Name = name;
            Duration = duration;
            MealsPerDay = mealsPerDay;
            _planDays = new List<PlanDay>();

            for (int i = 1; i <= duration.Days(); i++)
            {
                _planDays.Add(new PlanDay(i));
            }

            AddDomainEvent(new MealPlanCreated(Id, Name, Duration.Type.ToString(), Duration.Days(), mealsPerDay));
        }

        public void Update(MealPlanName name, PlanDuration duration, int mealsPerDay)
        {
            if (!duration.IsValid())
                throw new BussinessRuleValidationException("Invalid plan duration");

            Name = name;
            Duration = duration;
            MealsPerDay = mealsPerDay;

            AddDomainEvent(new MealPlanUpdated(Id, Name, Duration.Type.ToString(), Duration.Days(), mealsPerDay));
        }

        public void AddMealTimeToDay(int dayNumber, MealTimeType type)
        {
            var day = _planDays.FirstOrDefault(d => d.DayNumber == dayNumber);
            if (day == null)
                throw new BussinessRuleValidationException($"Day {dayNumber} does not exist in the plan");

            day.AddMealTime(type);

            var addedMealTime = day.MealTimes.First(t => t.Order == (int)type);
            AddDomainEvent(new MealTimeAddedToPlan(Id, dayNumber, addedMealTime.Id, type.ToDisplayName(), addedMealTime.Order));
        }

        public void AssignRecipeToMealTime(int dayNumber, Guid mealTimeId, Guid recipeId, Ration ration)
        {
            var day = _planDays.FirstOrDefault(d => d.DayNumber == dayNumber);
            if (day == null)
                throw new BussinessRuleValidationException($"Day {dayNumber} does not exist in the plan");

            var mealTime = day.FindMealTime(mealTimeId);
            if (mealTime == null)
                throw new BussinessRuleValidationException($"Meal time {mealTimeId} does not exist on day {dayNumber}");

            mealTime.AssignRecipe(recipeId, ration);

            AddDomainEvent(new RecipeAssignedToMealTime(Id, dayNumber, mealTimeId, recipeId, ration.Quantity));
        }

        public void AssignRecipeToMealTime(Guid mealTimeId, Guid recipeId, Ration ration)
        {
            foreach (var day in _planDays)
            {
                var mealTime = day.FindMealTime(mealTimeId);
                if (mealTime != null)
                {
                    mealTime.AssignRecipe(recipeId, ration);
                    AddDomainEvent(new RecipeAssignedToMealTime(Id, day.DayNumber, mealTimeId, recipeId, ration.Quantity));
                    return;
                }
            }

            throw new BussinessRuleValidationException($"Meal time {mealTimeId} does not exist in the plan");
        }

        public void RemoveRecipeFromMealTime(Guid mealTimeId, Guid recipeId)
        {
            foreach (var day in _planDays)
            {
                var mealTime = day.FindMealTime(mealTimeId);
                if (mealTime != null)
                {
                    mealTime.RemoveRecipe(recipeId);
                    AddDomainEvent(new RecipeRemovedFromMealTime(Id, day.DayNumber, mealTimeId, recipeId));
                    return;
                }
            }

            throw new BussinessRuleValidationException($"Meal time {mealTimeId} does not exist in the plan");
        }
    }
}