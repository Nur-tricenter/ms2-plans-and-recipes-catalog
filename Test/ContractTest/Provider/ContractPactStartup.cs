using Catalog.Tests.IntegrationTest.Setup;
using Catalog.App;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Tests.ContractTest.Provider
{
    /// <summary>
    /// Test host startup for Pact provider verification. It reuses the real <see cref="Startup"/>
    /// pipeline and then swaps the persistence layer for the in-memory fakes, so the provider
    /// runs exactly like production but without needing PostgreSQL.
    /// </summary>
    public class ContractPactStartup : Startup
    {
        public ContractPactStartup(IConfiguration configuration) : base(configuration)
        {
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.ReplaceInfrastructureWithInMemory($"ContractPact_{Guid.NewGuid():N}");

            // The host is built with Host.CreateDefaultBuilder, so the default application part
            // points at the test assembly. Register the real WebApi assembly so its controllers
            // are discovered and the pipeline exposes the endpoints under test.
            services.AddControllers()
                .AddApplicationPart(typeof(Startup).Assembly);
        }

        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Pact replays the consumer interactions against this host and, for the interactions
            // that declare .Given(...), first POSTs to /provider-states. The middleware runs
            // before the real pipeline and seeds the in-memory read model with the contract data.
            app.UseMiddleware<ProviderStateMiddleware>();

            base.Configure(app, env);
        }
    }
}