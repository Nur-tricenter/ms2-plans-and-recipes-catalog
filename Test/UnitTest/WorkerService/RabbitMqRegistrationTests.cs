using Catalog.Application;
using Catalog.Application.UseCase.DomainEventHandler;
using DomainFoodEvent = Catalog.Domain.Event.Food.FoodCreated;
using DomainFoodNutritionEvent = Catalog.Domain.Event.Food.FoodNutritionalInfoUpdated;
using DomainMealPlanCreated = Catalog.Domain.Event.MealPlan.MealPlanCreated;
using DomainRecipeCreated = Catalog.Domain.Event.Recipe.RecipeCreated;
using DomainRecipeUpdated = Catalog.Domain.Event.Recipe.RecipeUpdated;
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
            // Asked for the CONCRETE types, not OutboxMessage<DomainEvent>. The publishers
            // register against the exact event they translate ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â that is what makes MediatR
            // deliver to them at all, because the outbox processor dispatches with the runtime
            // type of the message's content. Resolving the base type finds nothing, which is why
            // this assertion used to fail with an empty collection.
            Assert.IsType<PublishIntegrationEventWhenMealPlanCreated>(
                Resolve<DomainMealPlanCreated>(scope));
            Assert.IsType<PublishIntegrationEventWhenFoodNutritionalInfoUpdated>(
                Resolve<DomainFoodNutritionEvent>(scope));
            Assert.IsType<PublishIntegrationEventWhenRecipeCreated>(
                Resolve<DomainRecipeCreated>(scope));
            Assert.IsType<PublishIntegrationEventWhenRecipeUpdated>(
                Resolve<DomainRecipeUpdated>(scope));
            Assert.IsType<PublishIntegrationEventWhenFoodCreated>(
                Resolve<DomainFoodEvent>(scope));
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
            // Counted per concrete event type, for the same reason as above: there is no single
            // service type that all five publishers share, by design.
            Assert.Equal(5, new object?[]
            {
                Resolve<DomainMealPlanCreated>(scope),
                Resolve<DomainFoodNutritionEvent>(scope),
                Resolve<DomainRecipeCreated>(scope),
                Resolve<DomainRecipeUpdated>(scope),
                Resolve<DomainFoodEvent>(scope)
            }.Count(handler => handler is not null));

            Assert.NotNull(scope.ServiceProvider.GetService<IExternalPublisher>());
            Assert.NotNull(scope.ServiceProvider.GetService<IMediator>());
        }

        /// <summary>
        /// Resolves the handler registered for one concrete outbox message type.
        /// </summary>
        /// <remarks>
        /// Written as a helper because the type argument differs per event, and repeating the
        /// `GetServices&lt;INotificationHandler&lt;OutboxMessage&lt;T&gt;&gt;&gt;` call at each site is
        /// where the base-type mistake originally crept in.
        /// </remarks>
        private static INotificationHandler<OutboxMessage<TEvent>>? Resolve<TEvent>(
            IServiceScope scope)
            where TEvent : DomainEvent =>
            scope.ServiceProvider
                .GetServices<INotificationHandler<OutboxMessage<TEvent>>>()
                .FirstOrDefault();

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
