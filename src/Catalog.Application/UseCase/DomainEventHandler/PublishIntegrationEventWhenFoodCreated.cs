using Joseco.Communication.External.Contracts.Services;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Catalog.Domain.Event.Food;
using Catalog.Contracts.Integration;
using Nur.Tricenter.Integration.Messages.Catalog;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.UseCase.DomainEventHandler
{
    /// <summary>
    /// Translates the outbox message that carries <see cref="FoodCreated"/> into the
    /// <see cref="FoodPublished"/> external contract and publishes it to RabbitMQ.
    /// </summary>
    /// <remarks>
    /// Published to <see cref="CatalogExchanges.FoodPublished"/>, which the production microservice
    /// binds its own queue to: an ingredient, once registered, is what production has to buy.
    /// <para>
    /// The handler is typed against <see cref="OutboxMessage{T}"/> for the concrete
    /// <see cref="FoodCreated"/> event rather than <see cref="DomainEvent"/>. The outbox
    /// processor dispatches messages through MediatR with the runtime event type, so a
    /// handler registered against the base <see cref="DomainEvent"/> would never match a
    /// specific <see cref="OutboxMessage{FoodCreated}"/> delivery: per-event handlers are
    /// what MediatR can resolve.
    /// </para>
    /// </remarks>
    public class PublishIntegrationEventWhenFoodCreated
        : INotificationHandler<OutboxMessage<FoodCreated>>
    {
        /// <summary>
        /// Exchange/routing destination for this contract. It MUST be passed explicitly: the
        /// library derives the default destination from <c>typeof(T)</c>, which for a runtime
        /// type that is not the compile-time generic argument silently routes to the wrong name.
        /// </summary>
        public const string Destination = CatalogExchanges.FoodPublished;

        /// <summary>Logical producer name stamped into every published contract.</summary>
        public const string Source = "catalog";

        private readonly ILogger<PublishIntegrationEventWhenFoodCreated> _logger;
        private readonly IExternalPublisher _publisher;

        public PublishIntegrationEventWhenFoodCreated(
            ILogger<PublishIntegrationEventWhenFoodCreated> logger,
            IExternalPublisher publisher)
        {
            _logger = logger;
            _publisher = publisher;
        }

        public async Task Handle(OutboxMessage<FoodCreated> notification, CancellationToken cancellationToken = default)
        {
            var foodCreated = notification.Content;

            var message = new FoodPublished(notification.CorrelationId, Source)
            {
                FoodId = foodCreated.FoodId,
                Name = foodCreated.Name,
                Category = foodCreated.Category,
                Quantity = (double)foodCreated.NutritionalInfo.Quantity,
                Calories = (double)foodCreated.NutritionalInfo.Calories,
                Protein = (double)foodCreated.NutritionalInfo.Protein,
                Fat = (double)foodCreated.NutritionalInfo.Fat,
                Carbs = (double)foodCreated.NutritionalInfo.Carbs,
                UnitOfMeasure = foodCreated.UnitOfMeasure.ToString()
            };

            _logger.LogInformation(
                "Publishing {EventType} for food {FoodId}: {Name}",
                message.EventType, message.FoodId, message.Name);

            // declareDestination MUST be true: the publish is mandatory, so an undeclared
            // exchange makes the call fail with a channel error instead of silently dropping.
            // NOTE: IExternalPublisher opens a new AMQP connection per message. That is the
            // library's behaviour (see Joseco.Communication.External.RabbitMQ 1.1.0) and is not
            // something this handler can change; revisit it if publish throughput ever matters.
            await _publisher.PublishAsync(message, Destination, declareDestination: true);
        }
    }
}