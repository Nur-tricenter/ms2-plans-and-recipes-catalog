using Catalog.Application;
using Catalog.Application.UseCase.DomainEventHandler;
using Catalog.Infrastructure;
using Catalog.WorkerService;
using Joseco.Communication.External.Contracts.Services;
using Joseco.Communication.External.RabbitMQ.Services;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Catalog.Tests.WorkerService
{
    /// <summary>
    /// Proves the WorkerService composition root for RabbitMQ without touching a broker.
    /// Resolving <see cref="IExternalPublisher"/> is safe offline: the library registers
    /// <c>IConnection</c> as a factory delegate and only opens a socket when that delegate is
    /// invoked, which happens inside <c>PublishAsync</c>, not during resolution.
    /// </summary>
    public class RabbitMqRegistrationTests
    {
        [Fact]
        public void AddCatalogRabbitMq_ValidConfiguration_RegistersTheSettingsAsASingleton()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCatalogRabbitMq(BuildConfiguration());
            using var provider = services.BuildServiceProvider();

            // Assert
            var settings = provider.GetService<RabbitMqSettings>();
            Assert.NotNull(settings);
            Assert.Equal("guest", settings.UserName);
            Assert.Same(settings, provider.GetService<RabbitMqSettings>());
        }

        [Fact]
        public void AddCatalogRabbitMq_ValidConfiguration_ResolvesTheExternalPublisher()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddCatalogRabbitMq(BuildConfiguration());
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            // Assert
            Assert.NotNull(scope.ServiceProvider.GetService<IExternalPublisher>());
        }

        [Fact]
        public void AddCatalogRabbitMq_ValidConfiguration_DoesNotRegisterAnyConsumer()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCatalogRabbitMq(BuildConfiguration());

            // Assert
            Assert.DoesNotContain(services, descriptor => descriptor.ServiceType.Name.StartsWith("IIntegrationMessageConsumer"));
            Assert.DoesNotContain(services, descriptor => descriptor.ImplementationType?.Name.Contains("Consumer", StringComparison.Ordinal) == true);
        }

        [Fact]
        public void AddCatalogRabbitMq_MissingConfigurationSection_Throws()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();

            // Act
            var exception = Assert.Throws<InvalidOperationException>(
                () => services.AddCatalogRabbitMq(configuration));

            // Assert
            Assert.Contains("RabbitMqSettings", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void AddApplication_WithoutIntegrationMessaging_DoesNotRegisterTheOutboxPublishers()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddApplication();
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            // Assert
            Assert.Empty(scope.ServiceProvider
                .GetServices<INotificationHandler<OutboxMessage<DomainEvent>>>());
        }

        [Fact]
        public void AddApplication_WithIntegrationMessaging_RegistersTheOutboxPublishers()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddApplication();
            services.AddCatalogRabbitMq(BuildConfiguration());
            services.AddIntegrationMessaging();
            using var provider = services.BuildServiceProvider(validateScopes: true);
            using var scope = provider.CreateScope();

            // Assert
            var handlers = scope.ServiceProvider
                .GetServices<INotificationHandler<OutboxMessage<DomainEvent>>>()
                .ToList();
            Assert.Contains(handlers, handler => handler is PublishIntegrationEventWhenMealPlanCreated);
            Assert.Contains(handlers, handler => handler is PublishIntegrationEventWhenFoodNutritionalInfoUpdated);
            Assert.Contains(handlers, handler => handler is PublishIntegrationEventWhenRecipeCreated);
            Assert.Contains(handlers, handler => handler is PublishIntegrationEventWhenRecipeUpdated);
            Assert.Contains(handlers, handler => handler is PublishIntegrationEventWhenFoodCreated);
        }

        [Fact]
        public void AddApplication_WithInfrastructureAndIntegrationMessaging_BuildsTheFullHostGraph()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddApplication();
            services.AddInfrastructure(BuildConfiguration());
            services.AddCatalogRabbitMq(BuildConfiguration());
            services.AddIntegrationMessaging();
            using var provider = services.BuildServiceProvider(validateScopes: true);
            using var scope = provider.CreateScope();

            // Assert
            Assert.Equal(5, scope.ServiceProvider
                .GetServices<INotificationHandler<OutboxMessage<DomainEvent>>>()
                .Count());
            Assert.NotNull(scope.ServiceProvider.GetService<IExternalPublisher>());
            Assert.NotNull(scope.ServiceProvider.GetService<IMediator>());
        }

        private static IConfiguration BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:CatalogDbConnectionString"] = "Host=localhost;Database=catalog;Username=test;Password=test",
                    ["RabbitMqSettings:Host"] = "localhost",
                    ["RabbitMqSettings:UserName"] = "guest",
                    ["RabbitMqSettings:Password"] = "guest",
                    ["RabbitMqSettings:VirtualHost"] = "/",
                    ["RabbitMqSettings:MaxRetryAttempts"] = "3",
                    ["RabbitMqSettings:RetryBaseDelayMilliseconds"] = "500"
                })
                .Build();
        }
    }
}
