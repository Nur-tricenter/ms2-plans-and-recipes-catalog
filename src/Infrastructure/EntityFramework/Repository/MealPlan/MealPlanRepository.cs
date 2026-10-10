using DomainMealPlan = Catalog.Domain.Model.MealPlans.MealPlan;
using Catalog.Domain.Repository.MealPlan;
using Catalog.Infrastructure.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.EntityFramework.Repository.MealPlan
{
    internal class MealPlanRepository : IMealPlanRepository
    {
        private readonly WriteDbContext _context;

        public MealPlanRepository(WriteDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(DomainMealPlan mealPlan)
        {
            await _context.MealPlans.AddAsync(mealPlan);
        }

        public async Task<DomainMealPlan?> FindByIdAsync(Guid id)
        {
            return await _context.MealPlans
                .Include("_planDays._mealTimes")
                .SingleOrDefaultAsync(x => x.Id == id);
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