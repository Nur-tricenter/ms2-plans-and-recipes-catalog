using Catalog.Infrastructure;
using Catalog.Infrastructure.Configuration;
using Catalog.Infrastructure.Observability;
using Catalog.Infrastructure.Consul;
using Catalog.WebApi.Configuration;
using Catalog.WebApi.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace Catalog.WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddInfrastructure(Configuration);

            // Traces, metrics and the meter collection list. Registered after AddInfrastructure so the
            // outbox decorator registered there resolves against the fully built provider.
            services.AddObservability(ObservabilityOptions.ServiceName, Configuration);

            services.AddControllers();

            // Bound here, read in Configure. SwaggerGen itself stays registered unconditionally:
            // it only builds the document generator, it publishes nothing. What publishes is
            // UseSwagger() below, and that is the part the flag controls. Registering it
            // conditionally instead would mean flipping the switch needs a different DI graph, not
            // just a different value.
            services.AddOptions<SwaggerOptions>()
                .Bind(Configuration.GetSection(SwaggerOptions.SectionName));

            services.AddOptions<HttpsRedirectionOptions>()
                .Bind(Configuration.GetSection(HttpsRedirectionOptions.SectionName));

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Catalog.WebApi", Version = "v1" });
            });

            // /health is always available and always mapped, because Consul probes it. It is wired
            // with ZERO checks by default: a host with no discovery configuration has nothing
            // external to report on, and "Healthy from having no checks" is the honest answer for
            // it. Registering checks unconditionally would instead make every WebApplicationFactory
            // test in Test/IntegrationTest open a TCP connection to a PostgreSQL that the InMemory
            // provider replaced.
            services.AddHealthChecks();

            // Configured through the DI options rather than passed to MapHealthChecks below, so
            // every consumer of HealthCheckOptions sees the same writer. Passing it only to
            // MapHealthChecks would leave anything resolving the options from DI reading a
            // different, default text response.
            services.Configure<HealthCheckOptions>(options => options.ResponseWriter = HealthReportWriter.WriteAsync);

            if (Configuration.IsServiceDiscoveryEnabled())
            {
                // The Postgres check and the Consul registration are gated by the SAME predicate.
                // Splitting them would let a host advertise itself to the registry with a health
                // check it never registered, or probe a database in a test that never reaches one.
                var connectionString = Configuration.GetConnectionString("CatalogDbConnectionString");
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    services.AddHealthChecks()
                        .AddNpgSql(connectionString, name: "postgres");
                }

                // Registered BEFORE AddConsulServiceRegistration so its IServerPortResolver wins:
                // the extension uses TryAdd, so whoever registers first provides the answer.
                services.AddSingleton<IServerPortResolver, ServerAddressesPortResolver>();
                services.AddConsulServiceRegistration(Configuration);
            }
        }

        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Every decision below is read from configuration, never from env.IsDevelopment().
            // The environment name describes how this process was launched; it does not describe
            // what the process should expose. What it did before was: `env.IsDevelopment()` was the
            // only switch for Swagger, which meant the published compose stack had to run
            // ASPNETCORE_ENVIRONMENT=Development just to get a docs page, and inherited the rest of
            // what "Development" implies. The name is still logged, because it is genuinely useful
            // when reading a log — it just no longer decides anything.
            app.ApplicationServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(Startup))
                .LogInformation(
                    "Catalog.WebApi starting in the {EnvironmentName} environment (content root {ContentRootPath}).",
                    env.EnvironmentName,
                    env.ContentRootPath);

            var swaggerOptions = app.ApplicationServices
                .GetRequiredService<IOptions<SwaggerOptions>>().Value;

            if (swaggerOptions.Enabled)
            {
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog.WebApi v1"));
            }

            var httpsRedirectionOptions = app.ApplicationServices
                .GetRequiredService<IOptions<HttpsRedirectionOptions>>().Value;

            if (httpsRedirectionOptions.Enabled)
            {
                // Only correct when THIS process is the TLS edge. Behind an ingress, an API gateway
                // or a mesh sidecar, TLS is already terminated upstream and the proxy re-originates
                // plain HTTP to this port; redirecting here answers every forwarded request with a
                // 307 to a port nothing is listening on.
                app.UseHttpsRedirection();
            }

            app.UseRouting();
            app.UseAuthorization();

            using (var scope = app.ApplicationServices.CreateScope())
            {
                // Migrate and seed are two independently configured concerns (Migrations:ApplyOnStartup
                // and Seed:Enabled), both safe defaults off. The previous code applied
                // StoredDbContext's migrations and then seeded unconditionally whenever the provider
                // was relational — which is true under Npgsql, so it seeded 30 foods / 25 recipes /
                // 2 plans on every production boot — and never migrated DomainDbContext at all.
                scope.ServiceProvider
                    .GetRequiredService<IDatabaseStartup>()
                    .RunAsync()
                    .GetAwaiter()
                    .GetResult();
            }

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                // This is the URL ServiceRegistration hands to Consul as the instance's check, so the
                // endpoint and the registration are the same contract by construction.
                // AllowAnonymous: a probe carries no credentials, and adding auth here would make
                // every instance look permanently unhealthy to the gateway.
                endpoints.MapHealthChecks("/health").AllowAnonymous();
            });
        }
    }
}
