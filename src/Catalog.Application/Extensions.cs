using Catalog.Application.Behaviors;
using Catalog.Application.UseCase.DomainEventHandler;
using Catalog.Domain.Event.Food;
using Catalog.Domain.Event.MealPlan;
using Catalog.Domain.Event.Recipe;
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
        /// and therefore the single host that ever dispatches <c>OutboxMessage&lt;…&gt;</c>. Leaving
        /// them registered in the WebApi would make DI validation fail there (nothing to construct
        /// them with) for handlers that can never run. Hosts that own the outbox processor must call
        /// <see cref="AddIntegrationMessaging"/> instead.
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
            // Each is registered against the concrete event type because the outbox processor
            // dispatches messages with their runtime type (OutboxMessage<FoodCreated>, etc.),
            // not the base OutboxMessage<DomainEvent>. Remove each from the WebApi scope so DI
            // validation does not require IExternalPublisher to be available there.
            services.RemoveAll<INotificationHandler<OutboxMessage<FoodCreated>>>();
            services.RemoveAll<INotificationHandler<OutboxMessage<Catalog.Domain.Event.Food.FoodNutritionalInfoUpdated>>>();
            services.RemoveAll<INotificationHandler<OutboxMessage<RecipeCreated>>>();
            services.RemoveAll<INotificationHandler<OutboxMessage<Catalog.Domain.Event.Recipe.RecipeUpdated>>>();
            services.RemoveAll<INotificationHandler<OutboxMessage<MealPlanCreated>>>();
            services.RemoveAll<INotificationHandler<OutboxMessage<MealTimeAddedToPlan>>>();
            services.RemoveAll<INotificationHandler<OutboxMessage<RecipeAssignedToMealTime>>>();
            services.RemoveAll<INotificationHandler<OutboxMessage<RecipeRemovedFromMealTime>>>();

            return services;
        }

        /// <summary>
        /// Registers the outbox-to-broker publishing handlers. Call this ONLY from a host that
        /// runs the outbox processor and that has already registered an
        /// <c>Joseco.Communication.External.Contracts.Services.IExternalPublisher</c>.
        /// </summary>
        public static IServiceCollection AddIntegrationMessaging(this IServiceCollection services)
        {
            services.AddTransient<INotificationHandler<OutboxMessage<MealPlanCreated>>, PublishIntegrationEventWhenMealPlanCreated>();
            services.AddTransient<INotificationHandler<OutboxMessage<Catalog.Domain.Event.Food.FoodNutritionalInfoUpdated>>, PublishIntegrationEventWhenFoodNutritionalInfoUpdated>();
            services.AddTransient<INotificationHandler<OutboxMessage<RecipeCreated>>, PublishIntegrationEventWhenRecipeCreated>();
            services.AddTransient<INotificationHandler<OutboxMessage<Catalog.Domain.Event.Recipe.RecipeUpdated>>, PublishIntegrationEventWhenRecipeUpdated>();
            services.AddTransient<INotificationHandler<OutboxMessage<FoodCreated>>, PublishIntegrationEventWhenFoodCreated>();
            services.AddTransient<INotificationHandler<OutboxMessage<MealTimeAddedToPlan>>, PublishIntegrationEventWhenMealPlanCompositionChanged>();
            services.AddTransient<INotificationHandler<OutboxMessage<RecipeAssignedToMealTime>>, PublishIntegrationEventWhenMealPlanCompositionChanged>();
            services.AddTransient<INotificationHandler<OutboxMessage<RecipeRemovedFromMealTime>>, PublishIntegrationEventWhenMealPlanCompositionChanged>();

            return services;
        }
    }
}
