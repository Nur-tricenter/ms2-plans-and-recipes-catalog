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

        /// <remarks>
        /// This is the ONLY constructor on purpose. The outbox persists this event through a
        /// Newtonsoft value converter with <c>TypeNameHandling.All</c>, and Newtonsoft prefers a
        /// public parameterless constructor over a parameterized one when both exist. Because every
        /// payload property above is get-only, that constructor produced an event whose payload was
        /// never written — <c>NutritionalInfo</c> came back <c>null</c>, <c>FoodId</c> came back
        /// <c>Guid.Empty</c>, with no error raised. With a single fully parameterized constructor
        /// Newtonsoft matches the JSON members to the parameters by name and rebuilds the event
        /// correctly. Do NOT add a parameterless constructor back: it reintroduces the silent
        /// truncation. See <c>Test/UnitTest/Domain/Events/DomainEventOutboxSerializationTests</c>.
        /// </remarks>
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
