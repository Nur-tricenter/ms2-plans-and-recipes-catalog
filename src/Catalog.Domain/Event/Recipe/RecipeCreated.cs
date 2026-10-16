using Joseco.DDD.Core.Abstractions;

namespace Catalog.Domain.Event.Recipe
{
    public record RecipeCreated : DomainEvent
    {
        public Guid RecipeId { get; }
        public string Name { get; }
        public string Instructions { get; }

        public RecipeCreated() : this(Guid.Empty, string.Empty, string.Empty)
        {
        }

        public RecipeCreated(Guid recipeId, string name, string instructions)
        {
            OccuredOn = DateTime.UtcNow;
            RecipeId = recipeId;
            Name = name;
            Instructions = instructions;
        }
    }
}
