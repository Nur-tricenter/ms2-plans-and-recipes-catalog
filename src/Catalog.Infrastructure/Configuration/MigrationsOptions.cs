namespace Catalog.Infrastructure.Configuration
{
    /// <summary>
    /// Controls the SCHEMA lifecycle of the two <see cref="Microsoft.EntityFrameworkCore.DbContext"/>s.
    /// </summary>
    /// <remarks>
    /// Lives in <c>Catalog.Infrastructure</c> rather than in <c>Catalog.WebApi</c> because the schema
    /// belongs to the data layer, not to the HTTP host: <c>Catalog.WorkerService</c> composes the same
    /// <c>AddInfrastructure</c> and could equally need to apply migrations as a deployment step.
    /// <para>
    /// The default is the SAFE one. <see cref="ApplyOnStartup"/> defaults to <c>false</c> because a
    /// web process is a bad place to run DDL: several replicas starting at once race each other,
    /// and a deployment that migrates implicitly migrates with whatever credentials the app happens
    /// to hold. Schema changes belong in an explicit step (<c>dotnet Catalog.WebApi.dll --migrate</c>);
    /// this flag exists only so a developer's <c>dotnet run</c> still works without ceremony.
    /// </para>
    /// </remarks>
    public class MigrationsOptions
    {
        public const string SectionName = "Migrations";

        public const bool ApplyOnStartupDefault = false;

        public const int DefaultTimeoutSeconds = 120;

        /// <summary>
        /// Whether the host applies pending migrations while starting. Off by default; see the type
        /// remarks.
        /// </summary>
        public bool ApplyOnStartup { get; set; } = ApplyOnStartupDefault;

        /// <summary>
        /// Budget for the whole migration run, covering both contexts. A migration step that hangs on
        /// an unreachable database must fail the deployment instead of blocking it forever, which is
        /// what <c>MigrateAsync(CancellationToken.None)</c> against a black-holed TCP connect does.
        /// </summary>
        public int TimeoutSeconds { get; set; } = DefaultTimeoutSeconds;
    }
}
