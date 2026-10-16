using Catalog.Domain.Repository.Food;
using Catalog.Domain.Repository.MealPlan;
using Catalog.Domain.Repository.Recipe;
using Catalog.Infrastructure.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Joseco.DDD.Core.Abstractions;

namespace Catalog.Tests.IntegrationTest.Setup
{
    public static class TestServiceExtensions
    {
        public static void ReplaceInfrastructureWithInMemory(this IServiceCollection services, string dbName)
        {
            services.ReplaceWithInMemoryFake<IFoodRepository, InMemoryFoodRepository>();
            services.ReplaceWithInMemoryFake<IRecipeRepository, InMemoryRecipeRepository>();
            services.ReplaceWithInMemoryFake<IMealPlanRepository, InMemoryMealPlanRepository>();
            services.ReplaceWithInMemoryFake<IUnitOfWork, InMemoryUnitOfWork>();

            services.RemoveAll<DbContextOptions<ReadDbContext>>();
            services.AddSingleton(new DbContextOptionsBuilder<ReadDbContext>().UseInMemoryDatabase(dbName).Options);

            services.RemoveAll<DbContextOptions<WriteDbContext>>();
            services.AddSingleton(new DbContextOptionsBuilder<WriteDbContext>().UseInMemoryDatabase(dbName).Options);
        }

        private static void ReplaceWithInMemoryFake<TService, TFake>(this IServiceCollection services)
            where TService : class
            where TFake : class, TService
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(TService));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<TService, TFake>();
        }
    }
}