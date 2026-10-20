using Catalog.Domain.Repository.Food;
using Catalog.Domain.Repository.MealPlan;
using Catalog.Domain.Repository.Recipe;
using Catalog.Infrastructure.Consul;
using Catalog.Infrastructure.Persistence.DomainModel;
using Catalog.Infrastructure.Persistence.StoredModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Joseco.DDD.Core.Abstractions;

namespace Catalog.Tests.IntegrationTest.Setup
{
    public static class TestServiceExtensions
    {
        private const string ServiceDiscoveryAddressKey = "ServiceDiscoveryAddress";

        /// <summary>
        /// Environment name for the test host. <c>WebApplicationFactory</c> forces
        /// <c>Development</c>, which is what makes it load
        /// <c>src/Catalog.App/appsettings.Development.json</c> — the file that points at a real
        /// Consul on http://localhost:8500. A test host is not a developer workstation, so it runs
        /// under a name of its own and the developer profile never applies to it.
        /// </summary>
        private const string TestingEnvironment = "Testing";

        /// <summary>
        /// Blanks <c>ServiceRegistration:ServiceDiscoveryAddress</c> for the test host.
        /// </summary>
        /// <remarks>
        /// This is not optional politeness, it is what keeps the suite hermetic.
        /// <c>WebApplicationFactory</c> runs the real host in the <c>Development</c> environment with
        /// the content root set to <c>src/Catalog.App</c>, so
        /// <c>src/Catalog.App/appsettings.Development.json</c> — which points at the real Consul
        /// on http://localhost:8500 — is loaded by every integration test. Without this override each
        /// test class would register a phantom <c>catalog-service</c> instance in the developer's
        /// live registry, advertise the Npgsql health check against a database the InMemory provider
        /// replaced, and spend up to 1.5s in registration retries whenever Consul is down.
        /// <para>
        /// The override is appended as an in-memory configuration source rather than issued through
        /// <c>IWebHostBuilder.UseSetting</c>. <c>UseSetting</c> was tried first and does NOT win here:
        /// its values are merged into the host's configuration, which the app configuration reads with
        /// LOWER precedence than the appsettings files, so <c>appsettings.Development.json</c> kept
        /// supplying <c>http://localhost:8500</c> and the suite still tried to register itself in the
        /// live registry. A provider appended last through <c>ConfigureAppConfiguration</c> does win.
        /// </para>
        /// <para>
        /// One blank value closes every door at once, because no <c>IConsulClient</c>, no registration
        /// hosted service and no Npgsql health check exist unless that single predicate is true.
        /// </para>
        /// </remarks>
        public static void DisableServiceDiscovery(this IWebHostBuilder builder)
        {
            builder.UseEnvironment(TestingEnvironment);

            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                var key = $"{ServiceRegistrationOptions.SectionName}:{nameof(ServiceDiscoveryAddressKey)}";
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [key] = string.Empty
                });
            });
        }

        public static void ReplaceInfrastructureWithInMemory(this IServiceCollection services, string dbName)
        {
            services.ReplaceWithInMemoryFake<IFoodRepository, InMemoryFoodRepository>();
            services.ReplaceWithInMemoryFake<IRecipeRepository, InMemoryRecipeRepository>();
            services.ReplaceWithInMemoryFake<IMealPlanRepository, InMemoryMealPlanRepository>();
            services.ReplaceWithInMemoryFake<IUnitOfWork, InMemoryUnitOfWork>();

            services.RemoveAll<DbContextOptions<StoredDbContext>>();
            services.AddSingleton(new DbContextOptionsBuilder<StoredDbContext>().UseInMemoryDatabase(dbName).Options);

            services.RemoveAll<DbContextOptions<DomainDbContext>>();
            services.AddSingleton(new DbContextOptionsBuilder<DomainDbContext>().UseInMemoryDatabase(dbName).Options);
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