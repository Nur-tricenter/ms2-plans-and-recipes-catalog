using Joseco.Communication.External.Contracts.Services;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Catalog.Application.IntegrationEvents;
using Microsoft.Extensions.Logging;

// The domain event (Catalog.Domain.Event.Recipe.RecipeUpdated) and the integration contract
// (Catalog.Application.IntegrationEvents.RecipeUpdated) deliberately share this name, the same way
// FoodNutritionalInfoUpdated does. The alias pins the wire type; the bare name keeps meaning the
// domain event.
using IntegrationContract = Catalog.Application.IntegrationEvents.RecipeUpdated;

namespace Catalog.Application.UseCase.DomainEventHandler
{
    /// <summary>
    /// Translates the outbox message that carries the domain event
    /// <see cref="Catalog.Domain.Event.Recipe.RecipeUpdated"/> into the
    /// <see cref="Catalog.Application.IntegrationEvents.RecipeUpdated"/> external contract and
    /// publishes it to RabbitMQ.
    /// </summary>
    /// <remarks>
    /// Published to <see cref="CatalogExchanges.RecipeUpdated"/>, which catering and delivery bind
    /// their own queues to.
    /// </remarks>
    public class PublishIntegrationEventWhenRecipeUpdated
        : INotificationHandler<OutboxMessage<DomainEvent>>
    {
        /// <summary>
        /// Exchange/routing destination for this contract. It MUST be passed explicitly: the
        /// library derives the default destination from <c>typeof(T)</c>, which for a runtime
        /// type that is not the compile-time generic argument silently routes to the wrong name.
        /// </summary>
        public const string Destination = CatalogExchanges.RecipeUpdated;

        /// <summary>Logical producer name stamped into every published contract.</summary>
        public const string Source = "catalog";

        private readonly ILogger<PublishIntegrationEventWhenRecipeUpdated> _logger;
        private readonly IExternalPublisher _publisher;

        public PublishIntegrationEventWhenRecipeUpdated(
            ILogger<PublishIntegrationEventWhenRecipeUpdated> logger,
            IExternalPublisher publisher)
        {
            _logger = logger;
            _publisher = publisher;
        }

        public async Task Handle(OutboxMessage<DomainEvent> notification, CancellationToken cancellationToken = default)
        {
            // Qualified on purpose: the bare name is ambiguous (see the IntegrationContract alias).
            if (notification.Content is not Catalog.Domain.Event.Recipe.RecipeUpdated recipeUpdated)
            {
                return;
            }

            var message = new IntegrationContract(notification.CorrelationId, Source)
            {
                RecipeId = recipeUpdated.RecipeId,
                Name = recipeUpdated.Name,
                Instructions = recipeUpdated.Instructions
            };

            _logger.LogInformation(
                "Publishing {EventType} for recipe {RecipeId}",
                message.EventType, message.RecipeId);

            // declareDestination MUST be true: the publish is mandatory, so an undeclared
            // exchange makes the call fail with a channel error instead of silently dropping.
            // NOTE: IExternalPublisher opens a new AMQP connection per message. That is the
            // library's behaviour (see Joseco.Communication.External.RabbitMQ 1.1.0) and is not
            // something this handler can change; revisit it if publish throughput ever matters.
            await _publisher.PublishAsync(message, Destination, declareDestination: true);
        }
    }
}