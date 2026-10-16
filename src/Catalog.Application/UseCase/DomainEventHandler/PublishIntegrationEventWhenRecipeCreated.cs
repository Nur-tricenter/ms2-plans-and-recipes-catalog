using Joseco.Communication.External.Contracts.Services;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Catalog.Domain.Event.Recipe;
using Catalog.Application.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.UseCase.DomainEventHandler
{
    /// <summary>
    /// Translates the outbox message that carries <see cref="RecipeCreated"/> into the
    /// <see cref="RecipePublished"/> external contract and publishes it to RabbitMQ.
    /// </summary>
    /// <remarks>
    /// Published to <see cref="CatalogExchanges.RecipePublished"/>, which catering and delivery
    /// bind their own queues to.
    /// <para>
    /// The contract deliberately carries only RecipeId/Name/Instructions because that is all
    /// <see cref="RecipeCreated"/> exposes at creation time — see the limitations documented on
    /// <see cref="RecipePublished"/>.
    /// </para>
    /// </remarks>
    public class PublishIntegrationEventWhenRecipeCreated
        : INotificationHandler<OutboxMessage<DomainEvent>>
    {
        /// <summary>
        /// Exchange/routing destination for this contract. It MUST be passed explicitly: the
        /// library derives the default destination from <c>typeof(T)</c>, which for a runtime
        /// type that is not the compile-time generic argument silently routes to the wrong name.
        /// </summary>
        public const string Destination = CatalogExchanges.RecipePublished;

        /// <summary>Logical producer name stamped into every published contract.</summary>
        public const string Source = "catalog";

        private readonly ILogger<PublishIntegrationEventWhenRecipeCreated> _logger;
        private readonly IExternalPublisher _publisher;

        public PublishIntegrationEventWhenRecipeCreated(
            ILogger<PublishIntegrationEventWhenRecipeCreated> logger,
            IExternalPublisher publisher)
        {
            _logger = logger;
            _publisher = publisher;
        }

        public async Task Handle(OutboxMessage<DomainEvent> notification, CancellationToken cancellationToken = default)
        {
            if (notification.Content is not RecipeCreated recipeCreated)
            {
                return;
            }

            var message = new RecipePublished(notification.CorrelationId, Source)
            {
                RecipeId = recipeCreated.RecipeId,
                Name = recipeCreated.Name,
                Instructions = recipeCreated.Instructions
            };

            _logger.LogInformation(
                "Publishing {EventType} for recipe {RecipeId}: {Name}",
                message.EventType, message.RecipeId, message.Name);

            // declareDestination MUST be true: the publish is mandatory, so an undeclared
            // exchange makes the call fail with a channel error instead of silently dropping.
            // NOTE: IExternalPublisher opens a new AMQP connection per message. That is the
            // library's behaviour (see Joseco.Communication.External.RabbitMQ 1.1.0) and is not
            // something this handler can change; revisit it if publish throughput ever matters.
            await _publisher.PublishAsync(message, Destination, declareDestination: true);
        }
    }
}