using Catalog.Infrastructure.Persistence.DomainModel;
using Catalog.Infrastructure.Persistence.StoredModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Catalog.Infrastructure.Persistence.Design
{
    /// <summary>
    /// Design-time factory for <see cref="DomainDbContext"/> so the EF Core CLI
    /// (<c>dotnet ef migrations ...</c>) can create the context without booting the WebApi.
    /// The connection string is a placeholder and the PostgreSQL version is pinned, so no
    /// network probe is attempted at design time.
    /// </summary>
    public class DomainDbContextFactory : IDesignTimeDbContextFactory<DomainDbContext>
    {
        internal const string DesignTimeConnectionString =
            "Host=localhost;Port=5432;Database=catalog_design;Username=postgres;Password=postgres";

        internal const int DesignTimeMajorVersion = 17;
        internal const int DesignTimeMinorVersion = 0;

        public DomainDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DomainDbContext>();
            optionsBuilder.UseNpgsql(DesignTimeConnectionString,
                npgsqlOptions => npgsqlOptions.SetPostgresVersion(DesignTimeMajorVersion, DesignTimeMinorVersion));

            return new DomainDbContext(optionsBuilder.Options);
        }
    }

    /// <summary>
    /// Design-time factory for <see cref="StoredDbContext"/>. See
    /// <see cref="DomainDbContextFactory"/> for the rationale.
    /// </summary>
    public class StoredDbContextFactory : IDesignTimeDbContextFactory<StoredDbContext>
    {
        internal const string DesignTimeConnectionString =
            "Host=localhost;Port=5432;Database=catalog_design;Username=postgres;Password=postgres";

        internal const int DesignTimeMajorVersion = 17;
        internal const int DesignTimeMinorVersion = 0;

        public StoredDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<StoredDbContext>();
            optionsBuilder.UseNpgsql(DesignTimeConnectionString,
                npgsqlOptions => npgsqlOptions.SetPostgresVersion(DesignTimeMajorVersion, DesignTimeMinorVersion));

            return new StoredDbContext(optionsBuilder.Options);
        }
    }
}