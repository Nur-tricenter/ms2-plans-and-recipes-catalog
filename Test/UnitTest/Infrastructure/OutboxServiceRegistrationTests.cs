using Catalog.Application;
using Catalog.Application.UseCase.DomainEventHandler;
using Catalog.Domain.Event.Recipe;
using Catalog.Infrastructure;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Service;
using Joseco.Outbox.EFCore;
using Joseco.Outbox.EFCore.Persistence;
using Joseco.Outbox.EFCore.Procesor;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Catalog.Tests.Infrastructure
{
    public class OutboxServiceRegistrationTests
    {
        [Fact]
        public void AddApplication_RegistersTheOutboxDomainEventHandlers()
        {
            // Arrange
            using var provider = BuildProvider(services =>
            {
                services.AddApplication();
                services.AddInfrastructure(BuildConfiguration());
            });

            // Act
            var handlers = provider.GetServices<INotificationHandler<RecipeCreated>>();

            // Assert
            Assert.Contains(handlers, handler => handler is SaveInOutboxWhenRecipeCreated);
        }

        [Fact]
        public void AddInfrastructure_RegistersTheOutboxPersistenceServices()
        {
            // Arrange
            using var provider = BuildProvider(services =>
            {
                services.AddApplication();
                services.AddInfrastructure(BuildConfiguration());
            });

            // Act
            using var scope = provider.CreateScope();

            // Assert
            Assert.NotNull(scope.ServiceProvider.GetService<IUnitOfWork>());
            Assert.NotNull(scope.ServiceProvider.GetService<IOutboxDatabase<DomainEvent>>());
            Assert.NotNull(scope.ServiceProvider.GetService<IOutboxService<DomainEvent>>());
            Assert.NotNull(scope.ServiceProvider.GetService<IMediator>());
        }

        [Fact]
        public void AddOutboxBackgroundService_RegistersTheOutboxHostedService()
        {
            // Arrange
            using var provider = BuildProvider(services =>
            {
                services.AddApplication();
                services.AddInfrastructure(BuildConfiguration());
                services.AddOutboxBackgroundService<DomainEvent>();
            });

            // Act
            var hostedServices = provider.GetServices<IHostedService>();

            // Assert
            Assert.Contains(hostedServices, service => service is OutboxBackgroundService<DomainEvent>);
            Assert.NotNull(provider.GetService<OutboxProcessor<DomainEvent>>());
        }

        private static ServiceProvider BuildProvider(Action<IServiceCollection> configure)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            configure(services);

            return services.BuildServiceProvider();
        }

        private static IConfiguration BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:CatalogDbConnectionString"] = "Host=localhost;Database=catalog;Username=test;Password=test"
                })
                .Build();
        }
    }
}