using Catalog.Domain.Model.Foods;
using Catalog.Domain.Model.MealPlans;
using Catalog.Domain.Model.Recipes;
using Catalog.Domain.Repository.Food;
using Catalog.Domain.Repository.MealPlan;
using Catalog.Domain.Repository.Recipe;
using Shared.Core;
using System.Collections.Concurrent;

namespace Catalog.Tests.IntegrationTest.Setup
{
    public class InMemoryUnitOfWork : IUnitOfWork
    {
        public Task Commit() => Task.CompletedTask;
    }

    public class InMemoryFoodRepository : IFoodRepository
    {
        private readonly ConcurrentDictionary<Guid, Food> _items = new();

        public Task<Food?> FindByIdAsync(Guid id) =>
            Task.FromResult<Food?>(_items.TryGetValue(id, out var item) ? item : null);

        public Task CreateAsync(Food obj)
        {
            _items[obj.Id] = obj;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Food food)
        {
            _items[food.Id] = food;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Food food)
        {
            _items.TryRemove(food.Id, out _);
            return Task.CompletedTask;
        }
    }

    public class InMemoryRecipeRepository : IRecipeRepository
    {
        private readonly ConcurrentDictionary<Guid, Recipe> _items = new();

        public Task<Recipe?> FindByIdAsync(Guid id) =>
            Task.FromResult<Recipe?>(_items.TryGetValue(id, out var item) ? item : null);

        public Task CreateAsync(Recipe obj)
        {
            _items[obj.Id] = obj;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Recipe recipe)
        {
            _items[recipe.Id] = recipe;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Recipe recipe)
        {
            _items.TryRemove(recipe.Id, out _);
            return Task.CompletedTask;
        }
    }

    public class InMemoryMealPlanRepository : IMealPlanRepository
    {
        private readonly ConcurrentDictionary<Guid, MealPlan> _items = new();

        public Task<MealPlan?> FindByIdAsync(Guid id) =>
            Task.FromResult<MealPlan?>(_items.TryGetValue(id, out var item) ? item : null);

        public Task CreateAsync(MealPlan obj)
        {
            _items[obj.Id] = obj;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(MealPlan plan)
        {
            _items[plan.Id] = plan;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(MealPlan plan)
        {
            _items.TryRemove(plan.Id, out _);
            return Task.CompletedTask;
        }
    }
}