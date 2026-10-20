using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure.Configuration
{
    /// <summary>
    /// Registration for the schema/data bootstrap. Called from <c>AddInfrastructure</c>, so every host
    /// that composes the data layer can run migrations as a deployment step — the WebApi's
    /// <c>--migrate</c> command and, if it ever needs one, the console worker.
    /// </summary>
    public static class DatabaseStartupExtensions
    {
        public static IServiceCollection AddDatabaseStartup(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ValidateOnStart on MigrationsOptions only: it is the one section with a value that can
            // be silently wrong (a zero or negative TimeoutSeconds would cancel the run immediately
            // and report "migrations finished"), and a deployment should learn about it when it boots
            // rather than when it is halfway through a schema change. SeedOptions is a bare bool, so
            // there is nothing to validate.
            services.AddOptions<MigrationsOptions>()
                .Bind(configuration.GetSection(MigrationsOptions.SectionName))
                .Validate(
                    options => options.TimeoutSeconds > 0,
                    $"{MigrationsOptions.SectionName}:{nameof(MigrationsOptions.TimeoutSeconds)} must be greater than zero.")
                .ValidateOnStart();

            services.AddOptions<SeedOptions>()
                .Bind(configuration.GetSection(SeedOptions.SectionName));

            // Scoped, because both contexts are scoped: a migrator holding a context past the scope it
            // was resolved in would keep a pooled connection checked out for the life of the process.
            services.AddScoped<IDatabaseMigrator, DatabaseMigrator>();
            services.AddScoped<IDatabaseStartup, DatabaseStartup>();

            return services;
        }
    }
}
