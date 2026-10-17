using System.Net;
using Catalog.Infrastructure.Consul;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Catalog.Tests.Infrastructure
{
    public class ServiceRegistrationTests
    {
        private readonly Mock<IConsulClient> _consulClientMock = new();
        private readonly Mock<IAgentEndpoint> _agentMock = new();
        private readonly Mock<IServerPortResolver> _portResolverMock = new();

        public ServiceRegistrationTests()
        {
            _consulClientMock.Setup(c => c.Agent).Returns(_agentMock.Object);
            _agentMock
                .Setup(a => a.ServiceRegister(It.IsAny<AgentServiceRegistration>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WriteResult<AgentServiceRegistration>());
            _agentMock
                .Setup(a => a.ServiceDeregister(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WriteResult<bool>());
        }

        [Fact]
        public async Task StartAsync_WithValidConfiguration_RegistersServiceWithHealthCheckOnOwnHealthEndpoint()
        {
            //Arrange
            AgentServiceRegistration? captured = null;
            _agentMock
                .Setup(a => a.ServiceRegister(It.IsAny<AgentServiceRegistration>(), It.IsAny<CancellationToken>()))
                .Callback<AgentServiceRegistration, CancellationToken>((registration, _) => captured = registration)
                .ReturnsAsync(new WriteResult<AgentServiceRegistration>());

            var sut = BuildSut(new ServiceRegistrationOptions
            {
                ServiceDiscoveryAddress = "http://localhost:8500",
                ServiceId = "catalog-service-1",
                ServiceName = "catalog-service",
                Host = "catalog",
                Port = 8080
            });

            //Act
            await sut.StartAsync(CancellationToken.None);

            //Assert
            Assert.NotNull(captured);
            Assert.Equal("catalog-service-1", captured!.ID);
            Assert.Equal("catalog-service", captured.Name);
            Assert.Equal("catalog", captured.Address);
            Assert.Equal(8080, captured.Port);
            Assert.Equal("http://catalog:8080/health", captured.Check!.HTTP);
        }

        [Fact]
        public async Task StartAsync_WithValidConfiguration_AdvertisesDeregisterCriticalServiceAfter30Seconds()
        {
            //Arrange
            AgentServiceRegistration? captured = null;
            _agentMock
                .Setup(a => a.ServiceRegister(It.IsAny<AgentServiceRegistration>(), It.IsAny<CancellationToken>()))
                .Callback<AgentServiceRegistration, CancellationToken>((registration, _) => captured = registration)
                .ReturnsAsync(new WriteResult<AgentServiceRegistration>());

            var sut = BuildSut(new ServiceRegistrationOptions
            {
                ServiceDiscoveryAddress = "http://localhost:8500",
                ServiceId = "catalog-service-1",
                ServiceName = "catalog-service",
                Host = "catalog",
                Port = 8080
            });

            //Act
            await sut.StartAsync(CancellationToken.None);

            //Assert
            Assert.NotNull(captured);
            Assert.Equal(TimeSpan.FromSeconds(30), captured!.Check!.DeregisterCriticalServiceAfter);
            Assert.Equal(ServiceRegistration.DeregisterCriticalServiceAfter, captured.Check.DeregisterCriticalServiceAfter);
        }

        [Fact]
        public async Task StopAsync_AfterSuccessfulRegistration_DeregistersTheService()
        {
            //Arrange
            var sut = BuildSut(new ServiceRegistrationOptions
            {
                ServiceDiscoveryAddress = "http://localhost:8500",
                ServiceId = "catalog-service-1",
                ServiceName = "catalog-service",
                Host = "catalog",
                Port = 8080
            });
            await sut.StartAsync(CancellationToken.None);

            //Act
            await sut.StopAsync(CancellationToken.None);

            //Assert
            _agentMock.Verify(a => a.ServiceDeregister("catalog-service-1", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task StartAsync_WithoutServiceDiscoveryAddress_DoesNotRegisterAndDoesNotThrow()
        {
            //Arrange
            var sut = BuildSut(new ServiceRegistrationOptions
            {
                ServiceId = "catalog-service-1",
                ServiceName = "catalog-service",
                Host = "localhost",
                Port = 8080
            });

            //Act
            var exception = await Record.ExceptionAsync(() => sut.StartAsync(CancellationToken.None));

            //Assert
            Assert.Null(exception);
            _agentMock.Verify(
                a => a.ServiceRegister(It.IsAny<AgentServiceRegistration>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task StartAsync_WhenConsulThrows_RetriesAndDoesNotThrow()
        {
            //Arrange
            _agentMock
                .Setup(a => a.ServiceRegister(It.IsAny<AgentServiceRegistration>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ConsulRequestException("consul is down", HttpStatusCode.InternalServerError));

            var sut = BuildSut(new ServiceRegistrationOptions
            {
                ServiceDiscoveryAddress = "http://localhost:8500",
                ServiceId = "catalog-service-1",
                ServiceName = "catalog-service",
                Host = "catalog",
                Port = 8080
            });

            //Act
            var exception = await Record.ExceptionAsync(() => sut.StartAsync(CancellationToken.None));

            //Assert
            Assert.Null(exception);
            // Retried rather than abandoned on the first failure, because a Consul agent restarting
            // independently of this process is the common case, not an exception.
            _agentMock.Verify(
                a => a.ServiceRegister(It.IsAny<AgentServiceRegistration>(), It.IsAny<CancellationToken>()),
                Times.Exactly(3));
        }

        [Fact]
        public async Task StopAsync_WhenConsulThrows_DoesNotThrow()
        {
            //Arrange
            _agentMock
                .Setup(a => a.ServiceDeregister(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ConsulRequestException("consul is down", HttpStatusCode.InternalServerError));

            var sut = BuildSut(new ServiceRegistrationOptions
            {
                ServiceDiscoveryAddress = "http://localhost:8500",
                ServiceId = "catalog-service-1",
                ServiceName = "catalog-service",
                Host = "catalog",
                Port = 8080
            });
            await sut.StartAsync(CancellationToken.None);

            //Act
            var exception = await Record.ExceptionAsync(() => sut.StopAsync(CancellationToken.None));

            //Assert
            Assert.Null(exception);
        }

        [Fact]
        public async Task StartAsync_WithoutConfiguredPort_FallsBackToThePortTheServerBoundTo()
        {
            //Arrange
            AgentServiceRegistration? captured = null;
            _agentMock
                .Setup(a => a.ServiceRegister(It.IsAny<AgentServiceRegistration>(), It.IsAny<CancellationToken>()))
                .Callback<AgentServiceRegistration, CancellationToken>((registration, _) => captured = registration)
                .ReturnsAsync(new WriteResult<AgentServiceRegistration>());

            _portResolverMock.Setup(r => r.ResolveBoundPort()).Returns(51234);

            var sut = BuildSut(new ServiceRegistrationOptions
            {
                ServiceDiscoveryAddress = "http://localhost:8500",
                ServiceId = "catalog-service-1",
                ServiceName = "catalog-service",
                Host = "localhost"
            });

            //Act
            await sut.StartAsync(CancellationToken.None);

            //Assert
            Assert.NotNull(captured);
            Assert.Equal(51234, captured!.Port);
            Assert.Equal("http://localhost:51234/health", captured.Check!.HTTP);
        }

        [Fact]
        public async Task StartAsync_WithNeitherConfiguredNorBoundPort_DoesNotRegister()
        {
            //Arrange
            _portResolverMock.Setup(r => r.ResolveBoundPort()).Returns((int?)null);

            var sut = BuildSut(new ServiceRegistrationOptions
            {
                ServiceDiscoveryAddress = "http://localhost:8500",
                ServiceId = "catalog-service-1",
                ServiceName = "catalog-service",
                Host = "localhost"
            });

            //Act
            var exception = await Record.ExceptionAsync(() => sut.StartAsync(CancellationToken.None));

            //Assert
            Assert.Null(exception);
            _agentMock.Verify(
                a => a.ServiceRegister(It.IsAny<AgentServiceRegistration>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public void AddConsulServiceRegistration_WithoutServiceDiscoveryAddress_RegistersNoHostedService()
        {
            //Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["ServiceRegistration:ServiceId"] = "catalog-service-1",
                ["ServiceRegistration:Port"] = "8080"
            });

            //Act
            services.AddConsulServiceRegistration(configuration);

            //Assert
            using var provider = services.BuildServiceProvider();
            Assert.Empty(provider.GetServices<IHostedService>());
            Assert.Null(provider.GetService<IConsulClient>());
        }

        [Fact]
        public void AddConsulServiceRegistration_WithServiceDiscoveryAddress_RegistersTheHostedService()
        {
            //Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["ServiceRegistration:ServiceDiscoveryAddress"] = "http://localhost:8500",
                ["ServiceRegistration:ServiceId"] = "catalog-service-1",
                ["ServiceRegistration:ServiceName"] = "catalog-service",
                ["ServiceRegistration:Host"] = "catalog",
                ["ServiceRegistration:Port"] = "8080"
            });

            //Act
            services.AddConsulServiceRegistration(configuration);

            //Assert
            using var provider = services.BuildServiceProvider();
            Assert.Contains(provider.GetServices<IHostedService>(), hosted => hosted is ServiceRegistration);
            Assert.NotNull(provider.GetService<IConsulClient>());
        }

        private ServiceRegistration BuildSut(ServiceRegistrationOptions options) => new(
            _consulClientMock.Object,
            Options.Create(options),
            _portResolverMock.Object,
            NullLogger<ServiceRegistration>.Instance);

        private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
            new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}