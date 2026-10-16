using Joseco.Communication.External.Contracts.Message;

namespace Catalog.Application.IntegrationEvents
{
    /// <summary>
    /// External contract announcing that a recipe's descriptive data changed (name or instructions).
    /// </summary>
    /// <remarks>
    /// The name deliberately mirrors the domain event
    /// <see cref="Catalog.Domain.Event.Recipe.RecipeUpdated"/>; the two live in different
    /// namespaces and are always referenced through one of them qualified (or through the
    /// <c>IntegrationContract</c> alias), the same way
    /// <c>FoodNutritionalInfoUpdated</c> already does.
    /// <para>
    /// Like <see cref="RecipePublished"/>, this contract carries the recipe identity and
    /// instructions only — the domain event has no ingredient data to forward.
    /// </para>
    /// <para>
    /// Delivered to catering and delivery (see <see cref="CatalogExchanges.RecipeUpdated"/>).
    /// </para>
    /// </remarks>
    public record RecipeUpdated : IntegrationMessage
    {
        /// <summary>
        /// Creates the contract with every member at a safe default. Required by the
        /// Newtonsoft deserializer used by the RabbitMQ library.
        /// </summary>
        public RecipeUpdated()
        {
            RecipeId = Guid.Empty;
            Name = string.Empty;
            Instructions = string.Empty;
        }

        /// <summary>
        /// Creates the contract and forwards the trace metadata to the shared envelope.
        /// </summary>
        public RecipeUpdated(string? correlationId, string? source)
            : base(correlationId, source)
        {
            RecipeId = Guid.Empty;
            Name = string.Empty;
            Instructions = string.Empty;
        }

        /// <summary>
        /// Stable discriminator carried on the wire, in the <c>&lt;Area&gt;.&lt;Fact&gt;</c> form
        /// every catalog contract uses. Consumers must switch on this value, not on the CLR type.
        /// </summary>
        public string EventType => "Recipe.Updated";

        public Guid RecipeId { get; init; }

        /// <summary>Recipe name, exactly as the domain event carried it.</summary>
        public string Name { get; init; }

        /// <summary>
        /// Preparation instructions. Ingredients are NOT present because the domain event does not
        /// carry them (see the remarks above).
        /// </summary>
        public string Instructions { get; init; }
    }
}