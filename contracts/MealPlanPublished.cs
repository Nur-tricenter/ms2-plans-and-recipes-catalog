using Joseco.Communication.External.Contracts.Message;

namespace Catalog.Contracts.Integration
{
    /// <summary>
    /// External contract announcing that a meal plan was published in the catalog.
    /// </summary>
    /// <remarks>
    /// These contracts are owned by this service and live in <c>contracts/</c>, not in a shared
    /// kernel. A kernel that carried them would force the next service to fork it to publish its
    /// own messages, and the two copies would drift.
    ///
    /// What IS shared is the base shape: every contract derives from
    /// <see cref="IntegrationMessage"/>, so it carries the common envelope
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
