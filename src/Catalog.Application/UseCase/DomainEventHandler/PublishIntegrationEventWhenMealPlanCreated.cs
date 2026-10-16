using Joseco.Communication.External.Contracts.Services;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Catalog.Domain.Event.MealPlan;
using Catalog.Application.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.UseCase.DomainEventHandler
{
    /// <summary>
    /// Translates the outbox message that carries <see cref="MealPlanCreated"/> into the
    /// <see cref="MealPlanPublished"/> external contract and publishes it to RabbitMQ.
    /// </summary>
    public class PublishIntegrationEventWhenMealPlanCreated
        : INotificationHandler<OutboxMessage<DomainEvent>>
    {
        /// <summary>
        /// Exchange/routing destination for this contract. It MUST be passed explicitly: the
        /// library derives the default destination from <c>typeof(T)</c>, which for a runtime
        /// type that is not the compile-time generic argument silently routes to the wrong name.
        /// </summary>
        public const string Destination = CatalogExchanges.MealPlanPublished;

        /// <summary>Logical producer name stamped into every published contract.</summary>
        public const string Source = "catalog";

        private readonly ILogger<PublishIntegrationEventWhenMealPlanCreated> _logger;
        private readonly IExternalPublisher _publisher;

        public PublishIntegrationEventWhenMealPlanCreated(
            ILogger<PublishIntegrationEventWhenMealPlanCreated> logger,
            IExternalPublisher publisher)
        {
            _logger = logger;
            _publisher = publisher;
        }

        public async Task Handle(OutboxMessage<DomainEvent> notification, CancellationToken cancellationToken = default)
        {
            if (notification.Content is not MealPlanCreated mealPlanCreated)
            {
                return;
            }

            var message = new MealPlanPublished(notification.CorrelationId, Source)
            {
                PlanId = mealPlanCreated.PlanId,
                Name = mealPlanCreated.Name,
                DurationType = mealPlanCreated.DurationType,
                TotalDays = mealPlanCreated.TotalDays,
                MealsPerDay = mealPlanCreated.MealsPerDay
            };

            _logger.LogInformation(
                "Publishing {EventType} for plan {PlanId}: {Name}",
                message.EventType, message.PlanId, message.Name);

            // declareDestination MUST be true: the publish is mandatory, so an undeclared
            // exchange makes the call fail with a channel error instead of silently dropping.
            // NOTE: IExternalPublisher opens a new AMQP connection per message. That is the
            // library's behaviour (see Joseco.Communication.External.RabbitMQ 1.1.0) and is not
            // something this handler can change; revisit it if publish throughput ever matters.
            await _publisher.PublishAsync(message, Destination, declareDestination: true);
        }
    }
}
