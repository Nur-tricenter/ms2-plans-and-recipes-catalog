namespace Catalog.Domain.Factory.Recipe
{
    using Recipe = Catalog.Domain.Model.Recipes.Recipe;

    public class RecipeFactory : IRecipeFactory
    {
        public Recipe Create(string name, string instructions)
        {
            return new Recipe(name, instructions);
        }
    }
}