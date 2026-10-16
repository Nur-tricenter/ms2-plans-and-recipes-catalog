using Joseco.Communication.External.Contracts.Message;

namespace Catalog.Application.IntegrationEvents
{
    /// <summary>
    /// External contract announcing that a meal plan was published in the catalog.
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
    public record MealPlanPublished : IntegrationMessage
    {
        /// <summary>
        /// Creates the contract with every member at a safe default. Required by the
        /// Newtonsoft deserializer used by the RabbitMQ library.
        /// </summary>
        public MealPlanPublished()
        {
            PlanId = Guid.Empty;
            Name = string.Empty;
            DurationType = string.Empty;
            TotalDays = 0;
            MealsPerDay = 0;
        }

        /// <summary>
        /// Creates the contract and forwards the trace metadata to the shared envelope.
        /// </summary>
        public MealPlanPublished(string? correlationId, string? source)
            : base(correlationId, source)
        {
            PlanId = Guid.Empty;
            Name = string.Empty;
            DurationType = string.Empty;
            TotalDays = 0;
            MealsPerDay = 0;
        }

        /// <summary>
        /// Stable discriminator carried on the wire. It already existed on the previous local
        /// <c>IntegrationEvent</c> shape; its value follows the English contract name, so any
        /// consumer must be updated in lockstep with this rename.
        /// </summary>
        public string EventType => "MealPlan.Published";

        public Guid PlanId { get; init; }
        public string Name { get; init; }
        public string DurationType { get; init; }
        public int TotalDays { get; init; }
        public int MealsPerDay { get; init; }
    }
}
