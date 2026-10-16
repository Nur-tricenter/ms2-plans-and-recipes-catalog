using Shared.Core;

namespace Catalog.Domain.Repository.Recipe
{
    using Recipe = Catalog.Domain.Model.Recipes.Recipe;

    public interface IRecipeRepository : IRepository<Recipe, Guid>
    {
        Task UpdateAsync(Recipe recipe);
        Task RemoveAsync(Recipe recipe);
    }
}