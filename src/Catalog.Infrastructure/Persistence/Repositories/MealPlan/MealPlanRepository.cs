using DomainMealPlan = Catalog.Domain.Model.MealPlans.MealPlan;
using Catalog.Domain.Repository.MealPlan;
using Catalog.Infrastructure.Persistence.DomainModel;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.Repositories.MealPlan
{
    internal class MealPlanRepository : IMealPlanRepository
    {
        private readonly DomainDbContext _context;

        public MealPlanRepository(DomainDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(DomainMealPlan mealPlan)
        {
            await _context.MealPlans.AddAsync(mealPlan);
        }

        public async Task<DomainMealPlan?> GetByIdAsync(Guid id, bool readOnly = false)
        {
            var query = readOnly
                ? _context.MealPlans.AsNoTracking().Include("_planDays._mealTimes")
                : _context.MealPlans.Include("_planDays._mealTimes");

            return await query.SingleOrDefaultAsync(x => x.Id == id);
        }

        public Task UpdateAsync(DomainMealPlan mealPlan)
        {
            _context.MealPlans.Update(mealPlan);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(DomainMealPlan mealPlan)
        {
            _context.MealPlans.Remove(mealPlan);
            return Task.CompletedTask;
        }
    }
}
