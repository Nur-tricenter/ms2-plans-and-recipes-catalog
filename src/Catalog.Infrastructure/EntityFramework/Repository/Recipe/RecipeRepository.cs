using DomainRecipe = Catalog.Domain.Model.Recipes.Recipe;
using Catalog.Domain.Repository.Recipe;
using Catalog.Infrastructure.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.EntityFramework.Repository.Recipe
{
    internal class RecipeRepository : IRecipeRepository
    {
        private readonly WriteDbContext _context;

        public RecipeRepository(WriteDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(DomainRecipe recipe)
        {
            await _context.Recipes.AddAsync(recipe);
        }

        public async Task<DomainRecipe?> FindByIdAsync(Guid id)
        {
            return await _context.Recipes
                .Include("_ingredients")
                .SingleOrDefaultAsync(x => x.Id == id);
        }

        public Task UpdateAsync(DomainRecipe recipe)
        {
            _context.Recipes.Update(recipe);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(DomainRecipe recipe)
        {
            _context.Recipes.Remove(recipe);
            return Task.CompletedTask;
        }
    }
}