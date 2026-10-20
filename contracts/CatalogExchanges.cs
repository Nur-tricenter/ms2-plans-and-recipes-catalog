namespace Catalog.Contracts.Integration
{
    /// <summary>
    /// Single source of truth for the RabbitMQ topology the catalog owns: one fanout exchange per
    /// published event, plus the single queue that accumulates the complete record of everything
    /// published.
    /// </summary>
    /// <remarks>
    /// Every publisher takes its <c>Destination</c> from a constant declared HERE, and the
    /// <c>DefaultQueueRegistrar</c> in <c>Catalog.WorkerService</c> binds
    /// <see cref="DefaultQueue"/> to <see cref="All"/>. That way the registrar cannot bind a queue
    /// to an exchange nobody publishes to, and a publisher cannot publish to an exchange nothing is
    /// bound to (which on a fanout exchange means the message is silently dropped).
    /// <para>
    /// <b>Why one exchange per event.</b> Each downstream microservice has a different interest:
    /// catering cares about recipes, production cares about the ingredients (foods), and delivery
    /// only needs the recipe basics to confirm it is delivering what was ordered. Separate
    /// exchanges keep those concerns independent, so adding a consumer to one stream never forces
    /// a consumer of another to also receive (and deserialize) messages it does not understand.
    /// </para>
    /// <para>
    /// Adding an event means adding a constant here, a contract, a publisher, and a line in
    /// <see cref="All"/>. The registry test in <c>Test/UnitTest/Application/IntegrationEvents</c>
    /// fails if that last step is forgotten.
    /// </para>
    /// </remarks>
    public static class CatalogExchanges
    {
        /// <summary>Exchange for <c>MealPlanPublished</c>.</summary>
        public const string MealPlanPublished = "meal-plan-published";

        /// <summary>Exchange for <c>FoodNutritionalInfoUpdated</c>.</summary>
        public const string FoodNutritionalInfoUpdated = "food-nutritional-info-updated";

        /// <summary>Exchange for <c>RecipePublished</c>. Bound by catering and delivery.</summary>
        public const string RecipePublished = "recipe-published";

        /// <summary>Exchange for <c>RecipeUpdated</c>. Bound by catering and delivery.</summary>
        public const string RecipeUpdated = "recipe-updated";

        /// <summary>Exchange for <c>FoodPublished</c>. Bound by production.</summary>
        public const string FoodPublished = "food-published";

        /// <summary>
        /// The one queue that exists today. Its purpose is to be the COMPLETE RECORD of everything
        /// the catalog publishes: it is bound to every exchange in <see cref="All"/> and never
        /// consumed, so the whole event history accumulates in it.
        /// </summary>
        /// <remarks>
        /// Per-consumer queues (catering, production, delivery) are intentionally NOT declared here.
        /// They are created by each consuming microservice against these exchanges.
        /// </remarks>
        public const string DefaultQueue = "catalog.default";

        private static readonly IReadOnlyList<string> _all =
        [
            MealPlanPublished,
            FoodNutritionalInfoUpdated,
            RecipePublished,
            RecipeUpdated,
            FoodPublished
        ];

        /// <summary>
        /// Every exchange the catalog publishes to, in registry order. The registrar binds
        /// <see cref="DefaultQueue"/> to exactly these, so this list is the authoritative topology.
        /// </summary>
        public static IReadOnlyList<string> All => _all;

        /// <summary>
        /// <c>true</c> when <paramref name="exchange"/> is one of the catalog exchanges in
        /// <see cref="All"/>. Used to keep publisher destinations and registrar bindings in step.
        /// </summary>
        public static bool Contains(string? exchange) =>
            exchange is not null && _all.Contains(exchange, StringComparer.Ordinal);
    }
}