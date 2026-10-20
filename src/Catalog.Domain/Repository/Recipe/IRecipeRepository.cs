using Joseco.DDD.Core.Abstractions;

namespace Catalog.Domain.Repository.Recipe
{
    using Recipe = Catalog.Domain.Model.Recipes.Recipe;

    public interface IRecipeRepository : IRepository<Recipe>
    {
        Task UpdateAsync(Recipe recipe);
        Task RemoveAsync(Recipe recipe);
    }
}
