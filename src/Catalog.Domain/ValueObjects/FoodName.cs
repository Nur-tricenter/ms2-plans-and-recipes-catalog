using Shared.Core;
using Shared.Rules;

namespace Catalog.Domain.ValueObjects
{
    public record FoodName : ValueObject
    {
        public string Name { get; init; }

        public FoodName(string name)
        {
            CheckRule(new StringNotNullOrEmptyRule(name));
            if (name.Length > 100)
                throw new BussinessRuleValidationException("Food name cannot be more than 100 characters");
            Name = name;
        }

        public static implicit operator string(FoodName value) => value.Name;

        public static implicit operator FoodName(string name) => new(name);
    }
}