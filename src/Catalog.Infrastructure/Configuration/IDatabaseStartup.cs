namespace Catalog.Infrastructure.Configuration
{
    /// <summary>
    /// Runs the two database startup concerns — schema then sample data — each behind its own
    /// independently configured switch.
    /// </summary>
    /// <remarks>
    /// This is the single place where <see cref="IDatabaseMigrator"/> and <c>IDbInitializer</c> meet,
    /// which is exactly why they are two collaborators and not one method. Either can be off while the
    /// other is on, neither knows about the other, and <see cref="IDbInitializer"/> can never run as a
    /// side effect of a migration: if <see cref="IDatabaseMigrator.MigrateAsync"/> throws, this method
    /// never reaches the seeding branch.
    /// </remarks>
    public interface IDatabaseStartup
    {
        Task RunAsync(CancellationToken cancellationToken = default);
    }
}
