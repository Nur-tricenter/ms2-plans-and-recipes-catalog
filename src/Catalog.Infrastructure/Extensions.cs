using Catalog.Application;
using Joseco.DDD.Core.Abstractions;
using Catalog.Domain.Repository.Food;
using Catalog.Domain.Repository.MealPlan;
using Catalog.Domain.Repository.Recipe;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Persistence.DomainModel;
using Catalog.Infrastructure.Persistence.StoredModel;
using Catalog.Infrastructure.Persistence.Repositories.Food;
using Catalog.Infrastructure.Persistence.Repositories.MealPlan;
using Catalog.Infrastructure.Persistence.Repositories.Recipe;
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

            services.AddDbContext<StoredDbContext>(context =>
                context.UseNpgsql(connectionString));
            services.AddDbContext<DomainDbContext>(context =>
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