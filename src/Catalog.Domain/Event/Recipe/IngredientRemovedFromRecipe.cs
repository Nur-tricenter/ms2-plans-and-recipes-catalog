using Joseco.DDD.Core.Abstractions;

namespace Catalog.Domain.Event.Recipe
{
    public record IngredientRemovedFromRecipe : DomainEvent
    {
        public Guid RecipeId { get; }
        public Guid FoodId { get; }
        public decimal PortionQuantity { get; }

        /// <remarks>
        /// This is the ONLY constructor on purpose. A public parameterless constructor would make
        /// the outbox's Newtonsoft deserializer build the event through it and then skip every
        /// get-only payload property, so the event would silently arrive empty. See
        /// <c>FoodCreated</c> and
        /// <c>Test/UnitTest/Domain/Events/DomainEventOutboxSerializationTests</c>.
        /// </remarks>
        public IngredientRemovedFromRecipe(Guid recipeId, Guid foodId, decimal portionQuantity)
        {
            OccuredOn = DateTime.UtcNow;
            RecipeId = recipeId;
            FoodId = foodId;
            PortionQuantity = portionQuantity;
        }
    }
}
