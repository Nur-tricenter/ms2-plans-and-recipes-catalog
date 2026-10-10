using Shared.Core;

namespace Catalog.Domain.Event.Recipe
{
    public record IngredientRemovedFromRecipe : DomainEvent
    {
        public Guid RecipeId { get; }
        public Guid FoodId { get; }
        public decimal PortionQuantity { get; }

        public IngredientRemovedFromRecipe(Guid recipeId, Guid foodId, decimal portionQuantity)
            : base(DateTime.UtcNow)
        {
            RecipeId = recipeId;
            FoodId = foodId;
            PortionQuantity = portionQuantity;
        }
    }
}