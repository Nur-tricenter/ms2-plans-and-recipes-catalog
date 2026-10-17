using Catalog.Infrastructure.Seed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Catalog.Infrastructure.Configuration
{
    /// <summary>
    /// The host's database bootstrap: migrate, then (separately, optionally) seed.
    /// </summary>
    /// <remarks>
    /// Two independent gates, not one "initialize the database" switch. That split is the whole point:
    /// a staging environment wants the schema and not the sample catalogue, a developer's laptop
    /// wants both, and production wants the schema applied by a separate one-shot job that does not
    /// exist on the application pod at all.
    /// <para>
    /// It is not a hosted service. A hosted service would race the host for readiness — the service
    /// would start accepting requests while the seed was still writing — and it would have to rely
    /// on registration order to run after the migration. An explicit call, made from the composition
    /// root before the pipeline is served, makes both facts structural.
    /// </para>
    /// </remarks>
    public class DatabaseStartup : IDatabaseStartup
    {
        private readonly IDatabaseMigrator _migrator;
        private readonly IDbInitializer _initializer;
        private readonly IOptions<MigrationsOptions> _migrationsOptions;
        private readonly IOptions<SeedOptions> _seedOptions;
        private readonly ILogger<DatabaseStartup> _logger;

        public DatabaseStartup(
            IDatabaseMigrator migrator,
            IDbInitializer initializer,
            IOptions<MigrationsOptions> migrationsOptions,
            IOptions<SeedOptions> seedOptions,
            ILogger<DatabaseStartup> logger)
        {
            _migrator = migrator;
            _initializer = initializer;
            _migrationsOptions = migrationsOptions;
            _seedOptions = seedOptions;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            // ---- Concern 1 of 2: schema. Nothing below this block writes rows. ----
            if (_migrationsOptions.Value.ApplyOnStartup)
            {
                await _migrator.MigrateAsync(cancellationToken);
            }
            else
            {
                _logger.LogInformation(
                    "Startup migration skipped because {Section}:{Setting} is false. Apply schema changes with 'dotnet Catalog.WebApi.dll --migrate'.",
                    MigrationsOptions.SectionName,
                    nameof(MigrationsOptions.ApplyOnStartup));
            }

            // ---- Concern 2 of 2: sample data. Reached only if the migration above did not throw. ----
            if (_seedOptions.Value.Enabled)
            {
                await _initializer.Initialize();
            }
            else
            {
                _logger.LogInformation(
                    "Startup seeding skipped because {Section}:{Setting} is false.",
                    SeedOptions.SectionName,
                    nameof(SeedOptions.Enabled));
            }
        }
    }
}
