namespace Catalog.Infrastructure.Configuration
{
    /// <summary>
    /// Applies pending EF Core migrations to every database this service owns.
    /// </summary>
    /// <remarks>
    /// An explicit collaborator, not something a host does for itself. The entry points are the
    /// <c>--migrate</c> command in <c>Catalog.WebApi/Program.cs</c> (the deployment step) and
    /// <see cref="IDatabaseStartup"/> (the developer convenience), and both of them go through this
    /// one type, so "which migrations ran, in what order, against which database" has a single
    /// answer.
    /// </remarks>
    public interface IDatabaseMigrator
    {
        Task MigrateAsync(CancellationToken cancellationToken = default);
    }
}
