using DomainFood = Catalog.Domain.Model.Foods.Food;
using Catalog.Domain.Repository.Food;
using Catalog.Infrastructure.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.EntityFramework.Repository.Food
{
    internal class FoodRepository : IFoodRepository
    {
        private readonly WriteDbContext _context;

        public FoodRepository(WriteDbContext context)
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
