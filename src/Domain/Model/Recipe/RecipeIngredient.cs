using Shared.Core;
using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Model.Recipes
{
    public record RecipeIngredient : ValueObject
    {
        public Guid FoodId { get; }
        public Portion Portion { get; }

        private RecipeIngredient() { Portion = default!; }

        public RecipeIngredient(Guid foodId, Portion portion)
        {
            FoodId = foodId;
            Portion = portion;
        }
    }
}