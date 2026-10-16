using Joseco.Communication.External.Contracts.Message;

namespace Catalog.Application.IntegrationEvents
{
    /// <summary>
    /// External contract announcing that a recipe was created in the catalog.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LIMITATION — READ BEFORE CONSUMING.</b> This contract is fired by
    /// <c>Catalog.Domain.Event.Recipe.RecipeCreated</c>, which happens the moment the recipe row is
    /// created, BEFORE any ingredient exists: ingredients are attached later through
    /// <c>AddIngredient</c>. It therefore carries the recipe IDENTITY and INSTRUCTIONS ONLY. A
    /// consumer that needs the finished dish (its ingredients, portions or nutritional totals) will
    /// NOT find them here.
    /// </para>
    /// <para>
    /// The domain currently publishes no integration event for the ingredient-level facts
    /// (<c>IngredientAddedToRecipe</c> / <c>IngredientRemovedFromRecipe</c> exist as domain events
    /// only), so there is no "recipe is complete" moment to announce. Deciding when a recipe
    /// becomes publishable to catering/delivery — and which event announces it — is an open
    /// modelling question, not something this contract may paper over by inventing data.
    /// </para>
    /// <para>
    /// Delivered to catering and delivery (see <see cref="CatalogExchanges.RecipePublished"/>).
    /// </para>
    /// </remarks>
    public record RecipePublished : IntegrationMessage
    {
        /// <summary>
        /// Creates the contract with every member at a safe default. Required by the
        /// Newtonsoft deserializer used by the RabbitMQ library.
        /// </summary>
        public RecipePublished()
        {
            RecipeId = Guid.Empty;
            Name = string.Empty;
            Instructions = string.Empty;
        }

        /// <summary>
        /// Creates the contract and forwards the trace metadata to the shared envelope.
        /// </summary>
        public RecipePublished(string? correlationId, string? source)
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
        public string EventType => "Recipe.Published";

        public Guid RecipeId { get; init; }

        /// <summary>Recipe name, exactly as <c>RecipeCreated.Name</c> carried it.</summary>
        public string Name { get; init; }

        /// <summary>
        /// Preparation instructions. Present because <c>RecipeCreated</c> carries them; ingredients
        /// are NOT present because the domain event does not have them yet (see the remarks above).
        /// </summary>
        public string Instructions { get; init; }
    }
}