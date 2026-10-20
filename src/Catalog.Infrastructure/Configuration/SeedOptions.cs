namespace Catalog.Infrastructure.Configuration
{
    /// <summary>
    /// Controls whether the sample catalogue data is written on startup.
    /// </summary>
    /// <remarks>
    /// Deliberately NOT part of <see cref="MigrationsOptions"/> and deliberately not a side effect of
    /// migrating. Schema and data are different concerns with different risk: a missing table breaks
    /// the service, while seeding 30 foods / 25 recipes / 2 plans on a production database overwrites
    /// nothing but does cost time on every boot and hides the fact that the database is never empty
    /// for a reason. Coupling them is what made this service seed itself in production, because
    /// <c>IsRelational()</c> is true under Npgsql just as it is under a developer's local Postgres.
    /// </remarks>
    public class SeedOptions
    {
        public const string SectionName = "Seed";

        public const bool EnabledDefault = false;

        /// <summary>
        /// Whether <c>IDbInitializer</c> runs on startup. Off by default: seeding is a development
        /// convenience and a deployment's explicit choice, never an accident of configuration.
        /// </summary>
        public bool Enabled { get; set; } = EnabledDefault;
    }
}
