using System.Net;
using Catalog.Tests.IntegrationTest.Setup;
using Catalog.WebApi.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Catalog.Tests.IntegrationTest.Configuration
{
    /// <summary>
    /// Proves the two pipeline decisions that used to be read off <c>env.IsDevelopment()</c> are now
    /// read off configuration, end to end through the real host.
    /// </summary>
    /// <remarks>
    /// These are integration tests rather than unit tests on purpose. The defect was not that a
    /// boolean was read from the wrong place; it was that a deployed, published binary mapped (or
    /// failed to map) two endpoints depending on a string in an environment variable. The only way
    /// to catch that class of bug is to boot the real host, flip the switch, and ask for the
    /// endpoint.
    /// </remarks>
    public class HostPipelineConfigurationTest
    {
        [Fact]
        public async Task GetSwaggerDocument_WhenSwaggerIsEnabled_ReturnsTheOpenApiDocument()
        {
            //Arrange
            using var factory = new ConfiguredWebApplicationFactory(
                swaggerEnabled: true, httpsRedirectionEnabled: false, httpsPort: null);

            var client = factory.CreateClient();

            //Act
            var response = await client.GetAsync("/swagger/v1/swagger.json");

            //Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task GetSwaggerDocument_WhenSwaggerIsDisabled_ReturnsNotFound()
        {
            //Arrange
            using var factory = new ConfiguredWebApplicationFactory(
                swaggerEnabled: false, httpsRedirectionEnabled: false, httpsPort: null);

            var client = factory.CreateClient();

            //Act
            var response = await client.GetAsync("/swagger/v1/swagger.json");

            //Assert
            // The shipped appsettings.json defaults Swagger to off, so this is what a deployment that
            // changed nothing gets. Before the change the same deployment answered only if it
            // happened to be running with ASPNETCORE_ENVIRONMENT=Development.
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetHealth_WhenHttpsRedirectionIsDisabled_ServesTheRequestOverPlainHttp()
        {
            //Arrange
            using var factory = new ConfiguredWebApplicationFactory(
                swaggerEnabled: false, httpsRedirectionEnabled: false, httpsPort: null);

            var client = factory.CreateClient();

            //Act
            var response = await client.GetAsync("/health");

            //Assert
            // The container case: TLS terminates upstream, this process speaks plain HTTP.
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetHealth_WhenHttpsRedirectionIsEnabled_RedirectsToTheHttpsPort()
        {
            //Arrange
            using var factory = new ConfiguredWebApplicationFactory(
                swaggerEnabled: false, httpsRedirectionEnabled: true, httpsPort: 443);

            // AllowAutoRedirect MUST be off here. WebApplicationFactory's client follows redirects by
            // default, and TestServer ignores the request scheme entirely, so the followed https
            // request would be served over plain http and the test would read the 200 of the
            // redirect TARGET as the answer — which is exactly the "307 became invisible" failure
            // this whole change exists to remove.
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            //Act
            var response = await client.GetAsync("/health");

            //Assert
            // Only correct where this process really is the TLS edge. Unconditional, as it used to
            // be, this is the 307 that made every plain-HTTP containerised request fail.
            Assert.Equal(HttpStatusCode.TemporaryRedirect, response.StatusCode);
            Assert.Equal("https", response.Headers.Location?.Scheme);
        }

        /// <summary>
        /// Boots the real <c>Catalog.WebApi.Startup</c> with two configuration values injected.
        /// </summary>
        /// <remarks>
        /// The overrides are appended through <c>ConfigureAppConfiguration</c>, not
        /// <c>UseSetting</c>, for the reason <see cref="TestServiceExtensions"/> documents:
        /// <c>UseSetting</c> loses to the appsettings files, so a test that used it would be
        /// asserting against whatever the developer's own <c>appsettings.Development.json</c>
        /// happened to say. It also calls <c>DisableServiceDiscovery()</c> and replaces the
        /// persistence layer, so no test here can reach a real Consul or a real PostgreSQL.
        /// </remarks>
        private sealed class ConfiguredWebApplicationFactory : WebApplicationFactory<Catalog.WebApi.Program>
        {
            private readonly bool _swaggerEnabled;
            private readonly bool _httpsRedirectionEnabled;
            private readonly int? _httpsPort;
            private readonly string _dbName = $"PipelineConfigTests_{Guid.NewGuid():N}";

            public ConfiguredWebApplicationFactory(
                bool swaggerEnabled,
                bool httpsRedirectionEnabled,
                int? httpsPort)
            {
                _swaggerEnabled = swaggerEnabled;
                _httpsRedirectionEnabled = httpsRedirectionEnabled;
                _httpsPort = httpsPort;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.DisableServiceDiscovery();

                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    var overrides = new Dictionary<string, string?>
                    {
                        [$"{SwaggerOptions.SectionName}:{nameof(SwaggerOptions.Enabled)}"] = _swaggerEnabled.ToString(),
                        [$"{HttpsRedirectionOptions.SectionName}:{nameof(HttpsRedirectionOptions.Enabled)}"] = _httpsRedirectionEnabled.ToString()
                    };

                    if (_httpsPort.HasValue)
                    {
                        // UseHttpsRedirectionMiddleware reads this key out of the APP configuration,
                        // after its own options and after the server's addresses have both come up
                        // empty. UseSetting would put it in the host configuration instead, which the
                        // appsettings files outrank — the same trap TestServiceExtensions documents
                        // for ServiceDiscoveryAddress.
                        overrides["https_port"] = _httpsPort.Value.ToString();
                    }

                    configuration.AddInMemoryCollection(overrides);
                });

                builder.ConfigureTestServices(services =>
                {
                    services.ReplaceInfrastructureWithInMemory(_dbName);
                });
            }
        }
    }
}
