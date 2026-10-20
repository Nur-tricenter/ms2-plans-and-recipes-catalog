using Catalog.Application;
using Joseco.DDD.Core.Abstractions;
using Catalog.Domain.Repository.Food;
using Catalog.Domain.Repository.MealPlan;
using Catalog.Domain.Repository.Recipe;
using Catalog.Infrastructure.Configuration;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Persistence.DomainModel;
using Catalog.Infrastructure.Persistence.StoredModel;
using Catalog.Infrastructure.Persistence.Repositories.Food;
using Catalog.Infrastructure.Persistence.Repositories.MealPlan;
using Catalog.Infrastructure.Persistence.Repositories.Recipe;
using Catalog.Infrastructure.Seed;
using Joseco.Outbox.EFCore.Persistence;
using Scrutor;
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

            // Resolved LAZILY, inside the AddDbContext callbacks below, on purpose. Reading it here
            // would make a missing connection string a failure of composition time, which the
            // integration suite hits on every single test: it boots the real Startup with the real
            // src/Catalog.App/appsettings.json and then replaces DbContextOptions<T> with the
            // InMemory ones, so the callbacks never run. Reading it inside the callbacks means the
            // suite never needs a PostgreSQL credential, while a real deployment that forgot to
            // supply one still fails immediately — with a message naming the setting and the fix —
            // the first time a context is resolved, instead of Npgsql's null-argument exception.
            services.AddDbContext<StoredDbContext>(context =>
                context.UseNpgsql(RequireConnectionString(configuration)));
            services.AddDbContext<DomainDbContext>(context =>
                context.UseNpgsql(RequireConnectionString(configuration)));

            services.AddScoped<IMealPlanRepository, MealPlanRepository>();
            services.AddScoped<IRecipeRepository, RecipeRepository>();
            services.AddScoped<IFoodRepository, FoodRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IOutboxDatabase<DomainEvent>, UnitOfWork>();
            AddOutboxServices<DomainEvent>(services);
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Extensions).Assembly));

            services.AddScoped<IDbInitializer, DbInitializer>();

            services.AddDatabaseStartup(configuration);

            return services;
        }

        /// <summary>
        /// Name of the single connection string both contexts share.
        /// </summary>
        /// <remarks>
        /// The write side (<c>DomainDbContext</c>) and the read side (<c>StoredDbContext</c>) point at
        /// the SAME database, separated by schema: <c>write</c> for the aggregates, <c>read</c> for
        /// the projections, <c>outbox</c> for the transactional outbox. One database, one
        /// credential, one deployment step — matching the reference architecture, which gives each
        /// microservice exactly one database. The CQRS split is physical at the schema level and
        /// logical at the code level: every query is served from <c>StoredDbContext</c>, never from
        /// the write model.
        /// </remarks>
        public const string ConnectionStringName = "CatalogDbConnectionString";

        /// <summary>
        /// Reads <see cref="ConnectionStringName"/> or fails with a message that says where it comes
        /// from.
        /// </summary>
        /// <remarks>
        /// The value is no longer in the tracked <c>appsettings.json</c>, by design: a connection
        /// string is a credential and a credential does not belong in version control. It arrives
        /// from a local, git-ignored <c>appsettings.Development.json</c> (copy
        /// <c>appsettings.Development.template.json</c>) or from the
        /// <c>ConnectionStrings__CatalogDbConnectionString</c> environment variable, which is what a
        /// deployment uses.
        /// </remarks>
        private static string RequireConnectionString(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"No value was found for configuration 'ConnectionStrings:{ConnectionStringName}'. " +
                    "Supply it through the ConnectionStrings__" + ConnectionStringName + " environment variable, " +
                    "or copy src/Catalog.App/appsettings.Development.template.json to " +
                    "src/Catalog.App/appsettings.Development.json and fill it in for local runs.");
            }

            return connectionString;
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

            // Wraps the registration above so every staged outbox row inherits the ambient trace.
            // Must run AFTER the AddScoped calls, or Scrutor decorates nothing.
            services.Decorate<IOutboxService<TEvent>, Observability.OutboxTracingService<TEvent>>();
        }
    }
}