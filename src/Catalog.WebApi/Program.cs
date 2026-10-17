using Catalog.Infrastructure.Configuration;
using Catalog.Infrastructure.Observability;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Catalog.WebApi
{
    public class Program
    {
        /// <summary>
        /// Argument that turns this executable into a one-shot schema deployment step.
        /// </summary>
        /// <remarks>
        /// Matched case-insensitively but without any short alias on purpose: there is exactly one
        /// command, and a second spelling of it would be a second thing to document and test.
        /// </remarks>
        private const string MigrateSwitch = "--migrate";

        public static int Main(string[] args)
        {
            // The deployment contract: schema changes are a step a pipeline runs, not something the
            // application pod does to itself while it is coming up. Several replicas racing each
            // other through Migrate() on every rollout is how a service ends up wedged in a crash
            // loop on a lock, and it gives the application pod's runtime user DDL rights it should
            // never have had.
            if (HasMigrateSwitch(args))
            {
                return RunMigrations(args);
            }

            CreateHostBuilder(args).Build().Run();
            return 0;
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                // Serilog with the ambient span stamped onto every event, so a log line can be joined
                // to its trace without code of ours. Applied here rather than in Startup so it also
                // covers the host's own lifecycle logs.
                .UseCatalogLogging(ObservabilityOptions.ServiceName)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        private static bool HasMigrateSwitch(string[] args) =>
            args.Any(arg => string.Equals(arg, MigrateSwitch, StringComparison.OrdinalIgnoreCase));

        private static int RunMigrations(string[] args)
        {
            // The switch is stripped before the host is composed: CommandLineConfigurationProvider
            // would otherwise turn it into a configuration key, and the extra key would shadow the
            // real settings with an empty value. Everything else stays, so
            // `ConnectionStrings__CatalogDbConnectionString=... --migrate` works as expected.
            var hostArguments = args.Where(arg => !string.Equals(arg, MigrateSwitch, StringComparison.OrdinalIgnoreCase)).ToArray();

            // The host is BUILT but never started: Build() runs ConfigureServices, which is where the
            // DbContexts are registered, and that is all the migrator needs. Run() is never reached,
            // so no port is bound and no HTTP pipeline is served.
            using var host = CreateHostBuilder(hostArguments).Build();

            var logger = host.Services
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("Catalog.WebApi.Migrations");

            using var scope = host.Services.CreateScope();

            try
            {
                scope.ServiceProvider
                    .GetRequiredService<IDatabaseMigrator>()
                    .MigrateAsync()
                    .GetAwaiter()
                    .GetResult();

                logger.LogInformation(
                    "Database migration step completed successfully. Seed data is NOT touched by this command; run the API with Seed:Enabled=true for that.");
                return 0;
            }
            catch (Exception exception)
            {
                // A non-zero exit code is the entire point. The orchestrator (Kubernetes Job, compose
                // one-shot service, CI step) decides whether the application may start; swallowing
                // this and returning 0 would start it against a schema that does not exist.
                logger.LogError(exception, "Database migration step failed.");
                return 1;
            }
        }
    }
}
