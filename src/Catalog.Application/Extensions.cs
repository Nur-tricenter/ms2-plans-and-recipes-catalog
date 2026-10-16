using Catalog.Application.Behaviors;
using Catalog.Application.UseCase.DomainEventHandler;
using Catalog.Domain.Factory.Food;
using Catalog.Domain.Factory.MealPlan;
using Catalog.Domain.Factory.Recipe;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Catalog.Application
{
    public static class Extensions
    {
        /// <summary>
        /// Registers the application layer: domain factories and the MediatR pipeline.
        /// </summary>
        /// <remarks>
        /// NOTE: MediatR's assembly scan also picks up the outbox-publishing handlers, but they are
        /// REMOVED here on purpose. They depend on <c>IExternalPublisher</c>, which is bound only by
        /// <c>Catalog.WorkerService</c> — the single host that runs <c>OutboxProcessor&lt;DomainEvent&gt;</c>
        /// and therefore the single host that ever dispatches <c>OutboxMessage&lt;DomainEvent&gt;</c>.
        /// Leaving them registered in the WebApi would make DI validation fail there (nothing to
        /// construct them with) for handlers that can never run. Hosts that own the outbox processor
        /// must call <see cref="AddIntegrationMessaging"/> instead.
        /// </remarks>
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddTransient<IMealPlanFactory, MealPlanFactory>();
            services.AddTransient<IRecipeFactory, RecipeFactory>();
            services.AddTransient<IFoodFactory, FoodFactory>();

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(Extensions).Assembly);
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(MetricsBehavior<,>));
            });

            // See the remarks above: the outbox publishers belong to the WorkerService host only.
            services.RemoveAll<INotificationHandler<OutboxMessage<DomainEvent>>>();

            return services;
        }

        /// <summary>
        /// Registers the outbox-to-broker publishing handlers. Call this ONLY from a host that
        /// runs the outbox processor and that has already registered an
        /// <c>Joseco.Communication.External.Contracts.Services.IExternalPublisher</c>.
        /// </summary>
        public static IServiceCollection AddIntegrationMessaging(this IServiceCollection services)
        {
            services.AddTransient<INotificationHandler<OutboxMessage<DomainEvent>>, PublishIntegrationEventWhenMealPlanCreated>();
            services.AddTransient<INotificationHandler<OutboxMessage<DomainEvent>>, PublishIntegrationEventWhenFoodNutritionalInfoUpdated>();
            services.AddTransient<INotificationHandler<OutboxMessage<DomainEvent>>, PublishIntegrationEventWhenRecipeCreated>();
            services.AddTransient<INotificationHandler<OutboxMessage<DomainEvent>>, PublishIntegrationEventWhenRecipeUpdated>();
            services.AddTransient<INotificationHandler<OutboxMessage<DomainEvent>>, PublishIntegrationEventWhenFoodCreated>();

            return services;
        }
    }
}
