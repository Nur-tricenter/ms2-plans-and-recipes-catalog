using DomainFood = Catalog.Domain.Model.Foods.Food;
using Catalog.Domain.Repository.Food;
using Catalog.Infrastructure.Persistence.DomainModel;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.Repositories.Food
{
    internal class FoodRepository : IFoodRepository
    {
        private readonly DomainDbContext _context;

        public FoodRepository(DomainDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(DomainFood food)
        {
            await _context.Foods.AddAsync(food);
        }

        public async Task<DomainFood?> GetByIdAsync(Guid id, bool readOnly = false)
        {
            var query = readOnly
                ? _context.Foods.AsNoTracking()
                : _context.Foods.AsQueryable();

            return await query.SingleOrDefaultAsync(x => x.Id == id);
        }

        public Task UpdateAsync(DomainFood food)
        {
            _context.Foods.Update(food);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(DomainFood food)
        {
            _context.Foods.Remove(food);
            return Task.CompletedTask;
        }
    }
}
