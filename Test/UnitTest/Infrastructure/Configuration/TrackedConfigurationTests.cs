using System.Text.Json;
using Catalog.Infrastructure.Configuration;
using Catalog.App.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Catalog.Tests.Infrastructure.Configuration
{
    /// <summary>
    /// The regression test for "no secrets in tracked files".
    /// </summary>
    /// <remarks>
    /// Every other thing this change did can be undone in a diff review. This one cannot: a
    /// connection string or a broker password put back into a tracked <c>appsettings.json</c> is
    /// committed, pushed and published before anyone notices, and the image that ships it carries it
    /// forever. A comment asking people not to do that is worth nothing next to a build failure.
    /// <para>
    /// The scope is the two TRACKED base configuration files (<c>git ls-files</c> confirms exactly
    /// those two exist). The development TEMPLATES are deliberately exempt in the other direction:
    /// they are tracked on purpose and they are supposed to carry the local credentials, otherwise a
    /// new contributor has nothing to copy and the local run is undocumented.
    /// </para>
    /// </remarks>
    public class TrackedConfigurationTests
    {
        [Theory]
        [InlineData("src", "Catalog.App", "appsettings.json")]
        [InlineData("src", "Catalog.WorkerService", "appsettings.json")]
        public void TrackedAppsettings_ContainsNoConnectionStringSection(string project, string folder, string fileName)
        {
            //Arrange
            var path = ResolveRepositoryFile(project, folder, fileName);

            //Act
            var document = JsonDocument.Parse(File.ReadAllText(path));

            //Assert
            Assert.False(
                document.RootElement.TryGetProperty("ConnectionStrings", out _),
                $"{fileName} must not declare a ConnectionStrings section: a connection string is a credential and this file is tracked by git. " +
                "Supply ConnectionStrings__CatalogDbConnectionString as an environment variable, or copy appsettings.Development.template.json to appsettings.Development.json.");

            document.Dispose();
        }

        [Theory]
        [InlineData("src", "Catalog.App", "appsettings.json")]
        [InlineData("src", "Catalog.WorkerService", "appsettings.json")]
        public void TrackedAppsettings_ContainsNoInlinePassword(string project, string folder, string fileName)
        {
            //Arrange
            var path = ResolveRepositoryFile(project, folder, fileName);

            //Act
            var content = File.ReadAllText(path);

            //Assert
            // Belt and braces over the section check above: a password smuggled in under any other
            // section, or a connection string pasted into one of the "//" comment keys, is still a
            // password in git.
            Assert.DoesNotContain("Password=", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"Password\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("pwd=", content, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("src", "Catalog.App", "appsettings.json")]
        [InlineData("src", "Catalog.WorkerService", "appsettings.json")]
        public void TrackedAppsettings_BindsToTheSafeDefaults(string project, string folder, string fileName)
        {
            //Arrange
            var path = ResolveRepositoryFile(project, folder, fileName);
            var configuration = new ConfigurationBuilder().AddJsonFile(path).Build();

            //Act
            var migrations = configuration.GetSection(MigrationsOptions.SectionName).Get<MigrationsOptions>() ?? new MigrationsOptions();
            var seed = configuration.GetSection(SeedOptions.SectionName).Get<SeedOptions>() ?? new SeedOptions();

            //Assert
            // Read back through the real binder, so the file that ships is the file the host binds.
            // A deployment that gets the tracked file and nothing else must not migrate, must not
            // seed and must not serve the OpenAPI document.
            Assert.False(migrations.ApplyOnStartup);
            Assert.True(migrations.TimeoutSeconds > 0);
            Assert.False(seed.Enabled);

            if (folder == "Catalog.App")
            {
                var swagger = configuration.GetSection(SwaggerOptions.SectionName).Get<SwaggerOptions>() ?? new SwaggerOptions();
                var httpsRedirection = configuration.GetSection(HttpsRedirectionOptions.SectionName).Get<HttpsRedirectionOptions>() ?? new HttpsRedirectionOptions();

                Assert.False(swagger.Enabled);
                Assert.False(httpsRedirection.Enabled);
            }
        }

        [Theory]
        [InlineData("src", "Catalog.App")]
        [InlineData("src", "Catalog.WorkerService")]
        public void DevelopmentTemplate_ExistsAndCarriesTheLocalCredentials(string project, string folder)
        {
            //Arrange
            var path = ResolveRepositoryFile(project, folder, "appsettings.Development.template.json");
            var configuration = new ConfigurationBuilder().AddJsonFile(path).Build();

            //Act
            var connectionString = configuration.GetConnectionString("CatalogDbConnectionString");

            //Assert
            // The template is the tracked artefact that replaces what was deleted from the base
            // file. Without it, "copy the template" is advice nobody can follow.
            Assert.False(string.IsNullOrWhiteSpace(connectionString));

            if (folder == "Catalog.WorkerService")
            {
                Assert.False(string.IsNullOrWhiteSpace(configuration["RabbitMqSettings:Host"]));
                Assert.False(string.IsNullOrWhiteSpace(configuration["RabbitMqSettings:UserName"]));
                Assert.False(string.IsNullOrWhiteSpace(configuration["RabbitMqSettings:Password"]));
            }
        }

        [Fact]
        public void GitIgnore_StillExcludesTheDevelopmentOverrideButNotTheTemplate()
        {
            //Arrange
            var gitIgnore = ResolveRepositoryFilePath(".gitignore");

            var patterns = File.ReadAllLines(gitIgnore)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0 && !line.StartsWith('#'))
                .ToArray();

            //Assert
            // The two halves of the contract, asserted against the file that implements it, without
            // changing that file. Half one: the developer's real override stays ignored, because it
            // holds real credentials. Half two: no wildcard pattern can swallow the template, or
            // "copy the template" would be advice a new contributor cannot follow and the tracked
            // artefact would silently not be tracked.
            Assert.Contains("appsettings.Development.json", patterns);

            Assert.DoesNotContain(patterns, pattern =>
                pattern.Contains('*') &&
                pattern.Contains("appsettings.Development", StringComparison.Ordinal));
        }

        /// <summary>
        /// Resolves a file inside the repository from the test output directory.
        /// </summary>
        /// <remarks>
        /// Walks up from <c>AppContext.BaseDirectory</c> until it finds the solution file instead of
        /// counting four <c>..</c> segments. The counting version
        /// (<c>Test/bin/Debug/net10.0</c> -> repository root) is what
        /// <c>Test/UnitTest/WorkerService/RabbitMqSettingsBindingTests</c> and
        /// <c>Test/ContractTest/Provider/CatalogApiProviderFixture</c> do, and it breaks the moment
        /// the test project gains a directory level, a RID sub-folder or a different output path — at
        /// which point a missing file reads as "the secret is not there" and the guard passes for the
        /// wrong reason. Anchoring on a file that must exist turns that into a loud failure.
        /// </remarks>
        private static string ResolveRepositoryFile(string project, string folder, string fileName) =>
            ResolveRepositoryFilePath(Path.Combine(project, folder, fileName));

        private static string ResolveRepositoryFilePath(string relativePath)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, relativePath);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new FileNotFoundException(
                $"Could not find '{relativePath}' walking up from '{AppContext.BaseDirectory}' to the repository root. " +
                "This test reads real files on disk, so it cannot pass by accident when they are missing.");
        }
    }
}
