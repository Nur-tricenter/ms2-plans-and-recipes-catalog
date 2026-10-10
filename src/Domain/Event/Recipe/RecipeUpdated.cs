using Shared.Core;

namespace Catalog.Domain.Event.Recipe
{
    public record RecipeUpdated : DomainEvent
    {
        public Guid RecipeId { get; }
        public string Name { get; }
        public string Instructions { get; }

        public RecipeUpdated(Guid recipeId, string name, string instructions)
            : base(DateTime.UtcNow)
        {
            RecipeId = recipeId;
            Name = name;
            Instructions = instructions;
        }
    }
}