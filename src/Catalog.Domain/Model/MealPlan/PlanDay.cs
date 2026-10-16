using Catalog.Domain.ValueObjects;
using Joseco.DDD.Core.Abstractions;
using Catalog.Domain.Primitives;

namespace Catalog.Domain.Model.MealPlans
{
    public class PlanDay : AuditableEntity
    {
        public int DayNumber { get; private set; }
        private readonly ICollection<MealTime> _mealTimes = new List<MealTime>();

        public IEnumerable<MealTime> MealTimes => _mealTimes;

        internal PlanDay(int dayNumber)
        {
            Id = Guid.NewGuid();
            DayNumber = dayNumber;
        }

        internal void AddMealTime(MealTimeType type)
        {
            var order = (int)type;
            if (_mealTimes.Any(t => t.Order == order))
                throw new BussinessRuleValidationException($"Meal time with order {order} already exists on day {DayNumber}");

            var mealTime = new MealTime(type.ToDisplayName(), order);
            _mealTimes.Add(mealTime);
        }

        internal MealTime? FindMealTime(Guid mealTimeId)
        {
            return _mealTimes.FirstOrDefault(t => t.Id == mealTimeId);
        }
    }
}