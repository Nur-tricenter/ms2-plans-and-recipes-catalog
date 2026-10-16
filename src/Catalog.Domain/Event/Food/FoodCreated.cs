using Joseco.DDD.Core.Abstractions;
using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Event.Food
{
    public record FoodCreated : DomainEvent
    {
        public Guid FoodId { get; }
        public string Name { get; }
        public string Category { get; }
        public NutritionalInfo NutritionalInfo { get; }
        public UnitOfMeasure UnitOfMeasure { get; }

        public FoodCreated()
            : this(Guid.Empty, string.Empty, string.Empty, default!, ValueObjects.UnitOfMeasure.Gram)
        {
        }

        public FoodCreated(Guid foodId, string name, string category, NutritionalInfo nutritionalInfo, UnitOfMeasure unitOfMeasure)
        {
            OccuredOn = DateTime.UtcNow;
            FoodId = foodId;
            Name = name;
            Category = category;
            NutritionalInfo = nutritionalInfo;
            UnitOfMeasure = unitOfMeasure;
        }
    }
}
