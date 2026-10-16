using Joseco.DDD.Core.Abstractions;

namespace Catalog.Domain.Event.Recipe
{
    public record RecipeCreated : DomainEvent
    {
        public Guid RecipeId { get; }
        public string Name { get; }

        public RecipeCreated(Guid recipeId, string name)
        {
            OccuredOn = DateTime.UtcNow;
            RecipeId = recipeId;
            Name = name;
        }
    }
}
