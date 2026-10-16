namespace Catalog.Domain.Factory.Recipe
{
    using Recipe = Catalog.Domain.Model.Recipes.Recipe;

    public interface IRecipeFactory
    {
        Recipe Create(string name, string instructions);
    }
}