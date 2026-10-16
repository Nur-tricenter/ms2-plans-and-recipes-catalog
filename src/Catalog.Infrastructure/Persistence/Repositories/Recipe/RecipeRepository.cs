using DomainRecipe = Catalog.Domain.Model.Recipes.Recipe;
using Catalog.Domain.Repository.Recipe;
using Catalog.Infrastructure.Persistence.DomainModel;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.Repositories.Recipe
{
    internal class RecipeRepository : IRecipeRepository
    {
        private readonly DomainDbContext _context;

        public RecipeRepository(DomainDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(DomainRecipe recipe)
        {
            await _context.Recipes.AddAsync(recipe);
        }

        public async Task<DomainRecipe?> GetByIdAsync(Guid id, bool readOnly = false)
        {
            var query = readOnly
                ? _context.Recipes.AsNoTracking().Include("_ingredients")
                : _context.Recipes.Include("_ingredients");

            return await query.SingleOrDefaultAsync(x => x.Id == id);
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
