using Joseco.DDD.Core.Abstractions;
using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Event.Food
{
    public record FoodNutritionalInfoUpdated : DomainEvent
    {
        public Guid FoodId { get; }
        public NutritionalInfo PreviousNutritionalInfo { get; }
        public NutritionalInfo NewNutritionalInfo { get; }

        public FoodNutritionalInfoUpdated(Guid foodId, NutritionalInfo previousNutritionalInfo, NutritionalInfo newNutritionalInfo)
        {
            OccuredOn = DateTime.UtcNow;
            FoodId = foodId;
            PreviousNutritionalInfo = previousNutritionalInfo;
            NewNutritionalInfo = newNutritionalInfo;
        }
    }
}
