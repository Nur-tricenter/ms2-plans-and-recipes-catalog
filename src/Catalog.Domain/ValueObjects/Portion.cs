using Joseco.DDD.Core.Abstractions;
using Catalog.Domain.Primitives;

namespace Catalog.Domain.ValueObjects
{
    public record Portion : ValueObject
    {
        public decimal Quantity { get; }

        private Portion() { }

        public Portion(decimal quantity)
        {
            if (quantity <= 0)
                throw new BussinessRuleValidationException("Portion quantity must be greater than zero");
            Quantity = quantity;
        }
    }
}