using Catalog.Infrastructure.Persistence.DomainModel;
using Catalog.Infrastructure.Persistence.StoredModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Catalog.Infrastructure.Configuration
{
    /// <summary>
    /// Applies pending migrations to the write side (<see cref="DomainDbContext"/>) and then to the
    /// read side (<see cref="StoredDbContext"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this type exists.</b> The service used to call <c>Migrate()</c> on
    /// <see cref="StoredDbContext"/> alone, inside <c>Startup.Configure</c>, and never touched
    /// <see cref="DomainDbContext"/> at all. The domain context owns the <c>foods</c>, <c>recipes</c>
    /// and <c>meal_plans</c> tables <em>and</em> the transactional outbox, so against a real
    /// PostgreSQL the first query failed with <c>42P01 relation "foods" does not exist</c> — the read
    /// model had been created while the model the service actually writes through had not.
    /// </para>
    /// <para>
    /// <b>Why write before read.</b> The read model is a projection of the write model: its rows are
    /// produced by handlers that read the write side. Migrating in the other order would leave a
    /// window in which a running deployment could take traffic against a read schema whose source
    /// tables are missing.
    /// </para>
    /// <para>
    /// <b>Why both contexts share one database is not a problem.</b> EF Core keys its
    /// <c>__EFMigrationsHistory</c> table by migration id, and the two contexts have disjoint ids, so
    /// each one applies only the migrations it does not see in the shared history.
    /// </para>
    /// <para>
    /// <b>Idempotent by construction.</b> EF Core filters by what the history table already records,
    /// so a second run applies nothing. The explicit pending check below makes that visible in the
    /// log instead of leaving it to be inferred from silence, and it also avoids opening a connection
    /// at all when there is nothing to do.
    /// </para>
    /// <para>
    /// <b>Non-relational providers are a no-op.</b> The integration suite swaps both contexts to EF
    /// Core InMemory, whose <c>Migrate()</c> throws by design. The migrator therefore checks
    /// <see cref="DatabaseFacade.IsRelational"/> per context, which is also why a host that was
    /// configured for PostgreSQL but is running under a test provider cannot take the schema down with
    /// it.
    /// </para>
    /// </remarks>
    public class DatabaseMigrator : IDatabaseMigrator
    {
        /// <summary>Human name for the write side, used in log messages and asserted on by tests.</summary>
        public const string WriteSide = "write (DomainDbContext)";

        /// <summary>Human name for the read side, used in log messages and asserted on by tests.</summary>
        public const string ReadSide = "read (StoredDbContext)";

        private readonly DomainDbContext _domainContext;
        private readonly StoredDbContext _storedContext;
        private readonly IOptions<MigrationsOptions> _options;
        private readonly ILogger<DatabaseMigrator> _logger;

        public DatabaseMigrator(
            DomainDbContext domainContext,
            StoredDbContext storedContext,
            IOptions<MigrationsOptions> options,
            ILogger<DatabaseMigrator> logger)
        {
            _domainContext = domainContext;
            _storedContext = storedContext;
            _options = options;
            _logger = logger;
        }

        public async Task MigrateAsync(CancellationToken cancellationToken = default)
        {
            var timeoutSeconds = _options.Value.TimeoutSeconds;
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            _logger.LogInformation(
                "Applying database migrations with a {TimeoutSeconds}s budget: {WriteSide} first, {ReadSide} second.",
                timeoutSeconds,
                WriteSide,
                ReadSide);

            try
            {
                await MigrateContextAsync(_domainContext, WriteSide, timeoutSource.Token);
                await MigrateContextAsync(_storedContext, ReadSide, timeoutSource.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Deliberately not swallowed into a log line and a success return code: a migration
                // step that timed out has NOT run, and a deployment that reported success there would
                // start a replica against a schema that does not exist.
                throw new TimeoutException(
                    $"Applying database migrations did not finish within {timeoutSeconds} seconds.");
            }

            _logger.LogInformation("Database migrations finished.");
        }

        private async Task MigrateContextAsync(DbContext context, string role, CancellationToken cancellationToken)
        {
            if (!context.Database.IsRelational())
            {
                _logger.LogInformation(
                    "Skipping the {Role} database: the {Provider} provider is not relational, so it has no schema to migrate.",
                    role,
                    context.Database.ProviderName);
                return;
            }

            var pending = context.Database.GetPendingMigrations().ToArray();

            if (pending.Length == 0)
            {
                _logger.LogInformation(
                    "The {Role} database is already up to date; no pending migration. Re-running is a no-op.",
                    role);
                return;
            }

            _logger.LogInformation(
                "Applying {PendingCount} pending migration(s) to the {Role} database: {PendingMigrations}.",
                pending.Length,
                role,
                string.Join(", ", pending));

            await context.Database.MigrateAsync(cancellationToken);

            _logger.LogInformation("The {Role} database is up to date.", role);
        }
    }
}
