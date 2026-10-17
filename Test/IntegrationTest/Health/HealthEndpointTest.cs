using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Catalog.Infrastructure.Consul;
using Catalog.Tests.IntegrationTest.Setup;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Catalog.Tests.IntegrationTest.Health
{
    /// <summary>
    /// Proves the host still comes up, and still reports itself healthy, when NOTHING is configured
    /// for service discovery.
    /// </summary>
    /// <remarks>
    /// This is the guard for every other integration test in the suite. <c>WebApplicationFactory</c>
    /// boots the real host in the <c>Development</c> environment against the WebApi content root, so
    /// without an explicit override it would load <c>appsettings.Development.json</c> — which points
    /// at a real Consul — and try to register this suite in the developer's live registry. If the
    /// gating ever regresses, the failure shows up here first, as a host that either will not start
    /// or reports Unhealthy because it is probing a database that the InMemory provider replaced.
    /// </remarks>
    public class HealthEndpointTest : IClassFixture<HealthEndpointTest.HealthWebApplicationFactory>
    {
        private readonly HealthWebApplicationFactory _factory;

        public HealthEndpointTest(HealthWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Health_WithoutServiceRegistrationConfiguration_ReturnsHealthy()
        {
            //Arrange
            var client = _factory.CreateClient();

            //Act
            var response = await client.GetAsync("/health");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = document.RootElement;

            Assert.True(root.GetProperty("success").GetBoolean());
            Assert.Equal("Healthy", root.GetProperty("data").GetProperty("status").GetString());

            // No checks were registered, which is the whole point: this host has no PostgreSQL to
            // reach and no registry to answer for, so it must not invent either dependency.
            Assert.Empty(root.GetProperty("data").GetProperty("entries").EnumerateArray());
        }

        [Fact]
        public void Host_WithoutServiceRegistrationConfiguration_RegistersNoConsulHostedService()
        {
            //Arrange
            var client = _factory.CreateClient();
            using var scope = _factory.Services.CreateScope();

            //Act
            var hostedServices = scope.ServiceProvider.GetServices<IHostedService>();

            //Assert
            Assert.NotNull(client);
            Assert.DoesNotContain(hostedServices, hosted => hosted is ServiceRegistration);
        }

        /// <summary>
        /// A private factory rather than a shared one: this suite asserts on host COMPOSITION, and
        /// borrowing the Food controller's fixture would couple that assertion to a class whose name
        /// says nothing about health checks.
        /// </summary>
        // Fully qualified: an unqualified 'Program' inside this nested type would bind to the Test
            // assembly's own entry point instead of the WebApi one.
        public class HealthWebApplicationFactory : WebApplicationFactory<Catalog.WebApi.Program>
        {
            private readonly string _dbName = $"HealthTests_{Guid.NewGuid()}";

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.DisableServiceDiscovery();

                builder.ConfigureTestServices(services =>
                {
                    services.ReplaceInfrastructureWithInMemory(_dbName);
                });
            }
        }
    }
}