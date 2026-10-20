using Joseco.Communication.External.Contracts.Message;

namespace Catalog.Contracts.Integration
{
    /// <summary>
    /// External contract announcing that the nutritional information of a food changed.
    /// </summary>
    /// <remarks>
    /// This contract deliberately does NOT extend <c>Nur.Store2026.Integration</c> messages.
    /// That package describes the MS2026 Product/Inventory/Orders/Payments domain, not the
    /// Food/Recipe/MealPlan domain owned by this service, and a NuGet package we do not own
    /// cannot be extended anyway. What is aligned with MS2026 is the BASE SHAPE: every contract
    /// derives from <see cref="IntegrationMessage"/> and therefore carries the shared envelope
    /// (<c>Id</c>, <c>CreatedAt</c>, <c>CorrelationId</c>, <c>Source</c>) that
    /// <c>Joseco.Communication.External.RabbitMQ</c> publishes and consumes.
    /// </remarks>
    public record FoodNutritionalInfoUpdated : IntegrationMessage
    {
        /// <summary>
        /// Creates the contract with every member at a safe default. Required by the
        /// Newtonsoft deserializer used by the RabbitMQ library and by the
        /// <c>TypeNameHandling.Auto</c> materialization path.
        /// </summary>
        public FoodNutritionalInfoUpdated()
        {
            FoodId = Guid.Empty;
            PreviousCalories = 0;
            PreviousProtein = 0;
            PreviousFat = 0;
            PreviousCarbs = 0;
            NewCalories = 0;
            NewProtein = 0;
            NewFat = 0;
            NewCarbs = 0;
        }

        /// <summary>
        /// Creates the contract and forwards the trace metadata to the shared envelope.
        /// </summary>
        public FoodNutritionalInfoUpdated(string? correlationId, string? source)
            : base(correlationId, source)
        {
            FoodId = Guid.Empty;
            PreviousCalories = 0;
            PreviousProtein = 0;
            PreviousFat = 0;
            PreviousCarbs = 0;
            NewCalories = 0;
            NewProtein = 0;
            NewFat = 0;
            NewCarbs = 0;
        }

        /// <summary>
        /// Stable discriminator carried on the wire. It already existed on the previous local
        /// <c>IntegrationEvent</c> shape; its value follows the English contract name, so any
        /// consumer must be updated in lockstep with this rename.
        /// </summary>
        public string EventType => "Food.NutritionalInfoUpdated";

        public Guid FoodId { get; init; }
        public double PreviousCalories { get; init; }
        public double PreviousProtein { get; init; }
        public double PreviousFat { get; init; }
        public double PreviousCarbs { get; init; }
        public double NewCalories { get; init; }
        public double NewProtein { get; init; }
        public double NewFat { get; init; }
        public double NewCarbs { get; init; }
    }
}
