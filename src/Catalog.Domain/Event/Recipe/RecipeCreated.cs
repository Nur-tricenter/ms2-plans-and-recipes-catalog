using Shared.Core;

namespace Catalog.Domain.Event.Recipe
{
    public record RecipeCreated : DomainEvent
    {
        public Guid RecipeId { get; }
        public string Name { get; }

        public RecipeCreated(Guid recipeId, string name)
            : base(DateTime.UtcNow)
        {
            RecipeId = recipeId;
            Name = name;
        }
    }
}