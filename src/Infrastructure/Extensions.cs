using Catalog.Application;
using Shared.Core;
using Catalog.Domain.Repository.Food;
using Catalog.Domain.Repository.MealPlan;
using Catalog.Domain.Repository.Recipe;
using Catalog.Infrastructure.EntityFramework;
using Catalog.Infrastructure.EntityFramework.Context;
using Catalog.Infrastructure.EntityFramework.Repository.Food;
using Catalog.Infrastructure.EntityFramework.Repository.MealPlan;
using Catalog.Infrastructure.EntityFramework.Repository.Recipe;
using Catalog.Infrastructure.Seed;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure
{
    public static class Extensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddApplication();

            var connectionString = configuration.GetConnectionString("CatalogDbConnectionString");

            services.AddDbContext<ReadDbContext>(context =>
                context.UseNpgsql(connectionString));
            services.AddDbContext<WriteDbContext>(context =>
                context.UseNpgsql(connectionString));

            services.AddScoped<IMealPlanRepository, MealPlanRepository>();
            services.AddScoped<IRecipeRepository, RecipeRepository>();
            services.AddScoped<IFoodRepository, FoodRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Extensions).Assembly));

            services.AddScoped<IDbInitializer, DbInitializer>();

            return services;
        }
    }
}