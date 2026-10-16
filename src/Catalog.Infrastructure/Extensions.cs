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
using Joseco.Outbox.EFCore.Persistence;
using Joseco.Outbox.EFCore.Procesor;
using Joseco.Outbox.Contracts.Service;
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
            services.AddScoped<IOutboxDatabase<DomainEvent>, UnitOfWork>();
            AddOutboxServices<DomainEvent>(services);
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Extensions).Assembly));

            services.AddScoped<IDbInitializer, DbInitializer>();

            return services;
        }

        /// <summary>
        /// Registers the transactional outbox services for <typeparamref name="TEvent"/>.
        /// This is the manual equivalent of <c>Joseco.Outbox.EFCore.DependencyInjection.AddOutbox&lt;TEvent&gt;()</c>,
        /// which cannot be called directly: that method is compiled against MediatR 12 and calls the
        /// removed <c>ServiceCollectionExtensions.AddMediatR</c>, so it throws a <c>TypeLoadException</c>
        /// on MediatR 14. Its MediatR registration only scans the Joseco assembly for tracing handlers,
        /// and none of them are used here because no <c>ITracingProvider</c> is registered.
        /// </summary>
        private static void AddOutboxServices<TEvent>(IServiceCollection services)
        {
            services.AddScoped<EfCoreOutboxService<TEvent>>();
            services.AddScoped<IOutboxService<TEvent>>(sp => sp.GetRequiredService<EfCoreOutboxService<TEvent>>());
            services.AddScoped<IOutboxRepository<TEvent>>(sp => sp.GetRequiredService<EfCoreOutboxService<TEvent>>());
            services.AddScoped<OutboxProcessor<TEvent>>();
        }
    }
}