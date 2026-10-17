using Catalog.WorkerService;
using Joseco.Communication.External.RabbitMQ.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Catalog.Tests.WorkerService
{
    public class RabbitMqSettingsBindingTests
    {
        [Fact]
        public void GetSection_BoundFromAnInMemoryConfiguration_BindsEveryMember()
        {
            // Arrange
            var configuration = BuildInMemoryConfiguration();

            // Act
            var settings = configuration.GetSection("RabbitMqSettings").Get<RabbitMqSettings>();

            // Assert
            Assert.NotNull(settings);
            Assert.Equal("localhost", settings.Host);
            Assert.Equal("catalogUser", settings.UserName);
            Assert.Equal("catalogUserPassword", settings.Password);
            Assert.Equal("/", settings.VirtualHost);
        }

        [Fact]
        public void GetSection_BoundFromAnInMemoryConfiguration_KeepsTheExplicitRetryPolicy()
        {
            // Arrange
            var configuration = BuildInMemoryConfiguration();

            // Act
            var settings = configuration.GetSection("RabbitMqSettings").Get<RabbitMqSettings>();

            // Assert
            Assert.NotNull(settings);
            Assert.Equal(3, settings.MaxRetryAttempts);
            Assert.Equal(500, settings.RetryBaseDelayMilliseconds);
        }

        [Fact]
        public void GetSection_BoundFromTheShippedWorkerServiceTemplate_BindsEveryMember()
        {
            // Arrange
            var configuration = BuildShippedAppsettingsConfiguration();

            // Act
            var settings = configuration.GetSection("RabbitMqSettings").Get<RabbitMqSettings>();

            // Assert
            Assert.NotNull(settings);
            Assert.Equal("localhost", settings.Host);
            Assert.Equal("catalogUser", settings.UserName);
            Assert.Equal("catalogUserPassword", settings.Password);
            Assert.Equal("/", settings.VirtualHost);
            Assert.Equal(3, settings.MaxRetryAttempts);
            Assert.Equal(500, settings.RetryBaseDelayMilliseconds);
        }

        private static IConfiguration BuildInMemoryConfiguration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RabbitMqSettings:Host"] = "localhost",
                    ["RabbitMqSettings:UserName"] = "catalogUser",
                    ["RabbitMqSettings:Password"] = "catalogUserPassword",
                    ["RabbitMqSettings:VirtualHost"] = "/",
                    ["RabbitMqSettings:MaxRetryAttempts"] = "3",
                    ["RabbitMqSettings:RetryBaseDelayMilliseconds"] = "500"
                })
                .Build();
        }

        private static IConfiguration BuildShippedAppsettingsConfiguration()
        {
            // Test/bin/<config>/<tfm> -> repository root (4 levels up) -> src/Catalog.WorkerService.
            var contentRoot = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

            // The TEMPLATE, not appsettings.json. The shipped base file no longer carries the
            // broker credential — a tracked file must not — and the template is the tracked artefact
            // that documents the development values a local run is expected to copy. Everything this
            // test actually asserts, that RabbitMqSettings binds Host, UserName, Password,
            // VirtualHost and the retry policy out of a real file on disk, is still proven.
            return new ConfigurationBuilder()
                .SetBasePath(contentRoot)
                .AddJsonFile(Path.Combine("src", "Catalog.WorkerService", "appsettings.Development.template.json"))
                .Build();
        }
    }
}
