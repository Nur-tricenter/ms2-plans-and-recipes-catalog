using Joseco.DDD.Core.Abstractions;
using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Event.Food
{
    public record FoodNutritionalInfoUpdated : DomainEvent
    {
        public Guid FoodId { get; }
        public NutritionalInfo PreviousNutritionalInfo { get; }
        public NutritionalInfo NewNutritionalInfo { get; }

        /// <remarks>
        /// This is the ONLY constructor on purpose. A public parameterless constructor would make
        /// the outbox's Newtonsoft deserializer build the event through it and then skip every
        /// get-only payload property, so both <see cref="NutritionalInfo"/> values would silently
        /// arrive as <c>null</c>. See <c>FoodCreated</c> and
        /// <c>Test/UnitTest/Domain/Events/DomainEventOutboxSerializationTests</c>.
        /// </remarks>
        public FoodNutritionalInfoUpdated(Guid foodId, NutritionalInfo previousNutritionalInfo, NutritionalInfo newNutritionalInfo)
        {
            OccuredOn = DateTime.UtcNow;
            FoodId = foodId;
            PreviousNutritionalInfo = previousNutritionalInfo;
            NewNutritionalInfo = newNutritionalInfo;
        }
    }
}
