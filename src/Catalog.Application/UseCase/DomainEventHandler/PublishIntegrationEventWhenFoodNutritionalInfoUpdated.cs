using Joseco.Communication.External.Contracts.Services;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Catalog.Domain.Event.Food;
using Catalog.Contracts.Integration;
using Microsoft.Extensions.Logging;

// The domain event (Catalog.Domain.Event.Food.FoodNutritionalInfoUpdated) and the integration
// contract (Catalog.Contracts.Integration.FoodNutritionalInfoUpdated) deliberately share
// this name, mirroring MS2026 where ProductCreated exists in both namespaces. The alias pins the
// wire type; the bare name keeps meaning the domain event.
using IntegrationContract = Catalog.Contracts.Integration.FoodNutritionalInfoUpdated;

namespace Catalog.Application.UseCase.DomainEventHandler
{
    /// <summary>
    /// Translates the outbox message that carries the domain event
    /// <see cref="Catalog.Domain.Event.Food.FoodNutritionalInfoUpdated"/> into the
    /// <see cref="Catalog.Contracts.Integration.FoodNutritionalInfoUpdated"/> external contract
    /// and publishes it to RabbitMQ.
    /// </summary>
    public class PublishIntegrationEventWhenFoodNutritionalInfoUpdated
        : INotificationHandler<OutboxMessage<DomainEvent>>
    {
        /// <summary>
        /// Exchange/routing destination for this contract. It MUST be passed explicitly: the
        /// library derives the default destination from <c>typeof(T)</c>, which for a runtime
        /// type that is not the compile-time generic argument silently routes to the wrong name.
        /// </summary>
        public const string Destination = CatalogExchanges.FoodNutritionalInfoUpdated;

        /// <summary>Logical producer name stamped into every published contract.</summary>
        public const string Source = "catalog";

        private readonly ILogger<PublishIntegrationEventWhenFoodNutritionalInfoUpdated> _logger;
        private readonly IExternalPublisher _publisher;

        public PublishIntegrationEventWhenFoodNutritionalInfoUpdated(
            ILogger<PublishIntegrationEventWhenFoodNutritionalInfoUpdated> logger,
            IExternalPublisher publisher)
        {
            _logger = logger;
            _publisher = publisher;
        }

        public async Task Handle(OutboxMessage<DomainEvent> notification, CancellationToken cancellationToken = default)
        {
            // Qualified on purpose: the bare name is ambiguous (see the IntegrationContract alias).
            if (notification.Content is not Catalog.Domain.Event.Food.FoodNutritionalInfoUpdated nutritionalInfoUpdated)
            {
                return;
            }

            var message = new IntegrationContract(notification.CorrelationId, Source)
            {
                FoodId = nutritionalInfoUpdated.FoodId,
                PreviousCalories = (double)nutritionalInfoUpdated.PreviousNutritionalInfo.Calories,
                PreviousProtein = (double)nutritionalInfoUpdated.PreviousNutritionalInfo.Protein,
                PreviousFat = (double)nutritionalInfoUpdated.PreviousNutritionalInfo.Fat,
                PreviousCarbs = (double)nutritionalInfoUpdated.PreviousNutritionalInfo.Carbs,
                NewCalories = (double)nutritionalInfoUpdated.NewNutritionalInfo.Calories,
                NewProtein = (double)nutritionalInfoUpdated.NewNutritionalInfo.Protein,
                NewFat = (double)nutritionalInfoUpdated.NewNutritionalInfo.Fat,
                NewCarbs = (double)nutritionalInfoUpdated.NewNutritionalInfo.Carbs
            };

            _logger.LogInformation(
                "Publishing {EventType} for food {FoodId}",
                message.EventType, message.FoodId);

            // declareDestination MUST be true: the publish is mandatory, so an undeclared
            // exchange makes the call fail with a channel error instead of silently dropping.
            // NOTE: IExternalPublisher opens a new AMQP connection per message. That is the
            // library's behaviour (see Joseco.Communication.External.RabbitMQ 1.1.0) and is not
            // something this handler can change; revisit it if publish throughput ever matters.
            await _publisher.PublishAsync(message, Destination, declareDestination: true);
        }
    }
}
