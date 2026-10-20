using Joseco.DDD.Core.Abstractions;
using Catalog.Domain.Primitives;

namespace Catalog.Domain.ValueObjects
{
    public record MealPlanName : ValueObject
    {
        public string Name { get; init; }

        public MealPlanName(string name)
        {
            CheckRule(new StringNotNullOrEmptyRule(name));
            if (name.Length > 200)
                throw new BussinessRuleValidationException("Meal plan name cannot be more than 200 characters");
            Name = name;
        }

        public static implicit operator string(MealPlanName value) => value.Name;

        public static implicit operator MealPlanName(string name) => new(name);
    }
}