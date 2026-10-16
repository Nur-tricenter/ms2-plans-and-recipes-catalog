using Catalog.Infrastructure.Persistence.StoredModel;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Tests.Infrastructure.Projection
{
    /// <summary>
    /// Builds a fresh in-memory <see cref="StoredDbContext"/> per test so the projection handlers can
    /// be exercised without a database and without any state shared between tests.
    /// </summary>
    internal static class StoredDbContextFactory
    {
        public static StoredDbContext Create(string? databaseName = null)
        {
            var options = new DbContextOptionsBuilder<StoredDbContext>()
                .UseInMemoryDatabase(databaseName ?? $"ProjectionTests_{Guid.NewGuid()}")
                .Options;

            return new StoredDbContext(options);
        }

        public static string NewDatabaseName() => $"ProjectionTests_{Guid.NewGuid()}";
    }
}
