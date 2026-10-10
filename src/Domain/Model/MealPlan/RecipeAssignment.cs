using Shared.Core;
using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Model.MealPlans
{
    public record RecipeAssignment : ValueObject
    {
        public Guid RecipeId { get; }
        public Ration Ration { get; }

        private RecipeAssignment() { Ration = default!; }

        public RecipeAssignment(Guid recipeId, Ration ration)
        {
            RecipeId = recipeId;
            Ration = ration;
        }
    }
}