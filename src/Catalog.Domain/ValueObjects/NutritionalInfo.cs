using Joseco.DDD.Core.Abstractions;
using Catalog.Domain.Primitives;

namespace Catalog.Domain.ValueObjects
{
    public record NutritionalInfo : ValueObject
    {
        public decimal Quantity { get; }
        public decimal Calories { get; }
        public decimal Protein { get; }
        public decimal Carbs { get; }
        public decimal Fat { get; }

        public NutritionalInfo(decimal quantity, decimal calories, decimal protein, decimal carbs, decimal fat)
        {
            if (quantity <= 0)
                throw new BussinessRuleValidationException("NutritionalInfo quantity must be greater than zero");
            Quantity = quantity;
            Calories = calories;
            Protein = protein;
            Carbs = carbs;
            Fat = fat;
        }
    }
}