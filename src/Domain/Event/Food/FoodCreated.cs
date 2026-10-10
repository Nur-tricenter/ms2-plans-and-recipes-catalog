using Shared.Core;
using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Event.Food
{
    public record FoodCreated : DomainEvent
    {
        public Guid FoodId { get; }
        public string Name { get; }
        public string Category { get; }
        public NutritionalInfo NutritionalInfo { get; }

        public FoodCreated(Guid foodId, string name, string category, NutritionalInfo nutritionalInfo)
            : base(DateTime.UtcNow)
        {
            FoodId = foodId;
            Name = name;
            Category = category;
            NutritionalInfo = nutritionalInfo;
        }
    }
}