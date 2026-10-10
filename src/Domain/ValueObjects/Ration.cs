using Shared.Core;

namespace Catalog.Domain.ValueObjects
{
    public record Ration : ValueObject
    {
        public int Quantity { get; }

        private Ration() { }

        public Ration(int quantity)
        {
            if (quantity <= 0)
                throw new BussinessRuleValidationException("Ration must be greater than zero");
            Quantity = quantity;
        }
    }
}