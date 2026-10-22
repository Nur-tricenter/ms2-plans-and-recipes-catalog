using Catalog.Contracts;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Configuration;
using Catalog.Infrastructure.Observability;
using Catalog.Infrastructure.Consul;
using Catalog.App.Configuration;
using Catalog.App.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Nur.Tricenter.Observability.Tracing;
using Nur.Tricenter.Security;
using Nur.Tricenter.Access.Catalog;

namespace Catalog.App
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

            // -------------------------------------------------------------------------
            // Authentication.
            //
            // The catalog used to have none: the token from the gateway arrived, nothing
            // validated it, `User` was an anonymous principal and no claim was readable. That was
            // invisible while the service only published and projected, and became visible the
            // moment a controller needed to know WHO was calling Ã¢â‚¬â€ a meal plan's author is an
            // authorization fact read from the `sub` claim.
            //
            // AddSecurity is the shared kernel's: it registers JWT bearer validation plus one
            // authorization policy per permission, so a controller asks for
            // [Authorize(Policy = CatalogPermission.MealPlanEdit)] instead of hand-rolling a scheme.
            //
            // The Jwt section MUST be identical to the identity service's Ã¢â‚¬â€ same signing key, same
            // issuer, same audience. They are one credential: identity signs it, catalog validates
            // it. A mismatch does not fail as a configuration error, it fails as a 401 on every
            // authenticated request, which reads like a gateway routing problem.
            // -------------------------------------------------------------------------
            services.AddSecurity(
                Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions(),
                CatalogPermission.PermissionsList);

            // The kernel's policies read FindAll("scope") by that LITERAL name. MapInboundClaims
            // defaults to true, which rewrites JWT claim names to WS-Federation URIs on the way in,
            // so a token minted by identity would arrive renamed and every policy would deny it Ã¢â‚¬â€
            // producing a valid token that authorizes nothing.
            services.PostConfigure<JwtBearerOptions>(options => options.MapInboundClaims = false);

            services.AddAuthorization();

            // Traces, metrics and the meter collection list, wired by the shared kernel. Registered
            // after AddInfrastructure so the outbox decorator registered there resolves against the
            // fully built provider. shouldIncludeHttpInstrumentation is left at its default (true):
            // this host serves every HTTP request in, and the ASP.NET Core instrumentation is what
            // creates the inbound Activity the outbox decorator later reads.
            services.AddObservability(ObservabilityOptions.ServiceName);

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
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Catalog.App", Version = "v1" });
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
            // when reading a log Ã¢â‚¬â€ it just no longer decides anything.
            app.ApplicationServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(Startup))
                .LogInformation(
                    "Catalog.App starting in the {EnvironmentName} environment (content root {ContentRootPath}).",
                    env.EnvironmentName,
                    env.ContentRootPath);

            var swaggerOptions = app.ApplicationServices
                .GetRequiredService<IOptions<SwaggerOptions>>().Value;

            if (swaggerOptions.Enabled)
            {
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog.App v1"));
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

            // Authentication BEFORE Authorization, always. UseAuthorization reads the principal
            // that UseAuthentication produced; with the order reversed it sees an anonymous user,
            // every policy denies, and the host answers 401 on endpoints that are correctly
            // configured Ã¢â‚¬â€ which reads as a bad permission rather than a missing middleware.
            app.UseAuthentication();
            app.UseAuthorization();

            using (var scope = app.ApplicationServices.CreateScope())
            {
                // Migrate and seed are two independently configured concerns (Migrations:ApplyOnStartup
                // and Seed:Enabled), both safe defaults off. The previous code applied
                // StoredDbContext's migrations and then seeded unconditionally whenever the provider
                // was relational Ã¢â‚¬â€ which is true under Npgsql, so it seeded 30 foods / 25 recipes /
                // 2 plans on every production boot Ã¢â‚¬â€ and never migrated DomainDbContext at all.
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
