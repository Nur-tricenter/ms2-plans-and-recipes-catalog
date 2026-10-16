using Joseco.Communication.External.Contracts.Message;

namespace Catalog.Application.IntegrationEvents
{
    /// <summary>
    /// External contract announcing that a food (an ingredient) was registered in the catalog.
    /// </summary>
    /// <remarks>
    /// A "food" is an ingredient here: it carries its category, its per-100-unit nutritional facts
    /// and the unit of measure those facts are expressed in, which is what the production
    /// microservice needs to plan what to buy and in which unit.
    /// <para>
    /// Nutritional values travel as <see cref="double"/> for the same reason they do on
    /// <see cref="FoodNutritionalInfoUpdated"/>: the domain holds them as <c>decimal</c>, and a
    /// double is what the wire format round-trips consistently across consumers.
    /// </para>
    /// <para>
    /// Delivered to production (see <see cref="CatalogExchanges.FoodPublished"/>).
    /// </para>
    /// </remarks>
    public record FoodPublished : IntegrationMessage
    {
        /// <summary>
        /// Creates the contract with every member at a safe default. Required by the
        /// Newtonsoft deserializer used by the RabbitMQ library.
        /// </summary>
        public FoodPublished()
        {
            FoodId = Guid.Empty;
            Name = string.Empty;
            Category = string.Empty;
            Quantity = 0;
            Calories = 0;
            Protein = 0;
            Fat = 0;
            Carbs = 0;
            UnitOfMeasure = string.Empty;
        }

        /// <summary>
        /// Creates the contract and forwards the trace metadata to the shared envelope.
        /// </summary>
        public FoodPublished(string? correlationId, string? source)
            : base(correlationId, source)
        {
            FoodId = Guid.Empty;
            Name = string.Empty;
            Category = string.Empty;
            Quantity = 0;
            Calories = 0;
            Protein = 0;
            Fat = 0;
            Carbs = 0;
            UnitOfMeasure = string.Empty;
        }

        /// <summary>
        /// Stable discriminator carried on the wire, in the <c>&lt;Area&gt;.&lt;Fact&gt;</c> form
        /// every catalog contract uses. Consumers must switch on this value, not on the CLR type.
        /// </summary>
        public string EventType => "Food.Published";

        public Guid FoodId { get; init; }
        public string Name { get; init; }
        public string Category { get; init; }

        /// <summary>
        /// The reference amount the remaining numbers apply to (grams, milliliters, units...), as
        /// carried by <c>NutritionalInfo.Quantity</c>.
        /// </summary>
        public double Quantity { get; init; }

        public double Calories { get; init; }
        public double Protein { get; init; }
        public double Fat { get; init; }
        public double Carbs { get; init; }

        /// <summary>
        /// The unit <see cref="Quantity"/> and the nutritional facts are expressed in, as the enum
        /// member name (<c>"Gram"</c>, <c>"Milliliter"</c>, ...). A string rather than the numeric
        /// enum value so a consumer that renumbers the domain enum does not silently misread
        /// existing events.
        /// </summary>
        public string UnitOfMeasure { get; init; }
    }
}