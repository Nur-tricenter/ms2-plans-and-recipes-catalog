using Joseco.Communication.External.Contracts.Services;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Catalog.Domain.Event.Food;
using Catalog.Contracts.Integration;
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
    /// No name alias is needed here: the domain event is <c>FoodCreated</c> and the contract is
    /// <c>FoodPublished</c>, so the two do not collide (unlike <c>RecipeUpdated</c>).
    /// </para>
    /// </remarks>
    public class PublishIntegrationEventWhenFoodCreated
        : INotificationHandler<OutboxMessage<DomainEvent>>
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

        public async Task Handle(OutboxMessage<DomainEvent> notification, CancellationToken cancellationToken = default)
        {
            if (notification.Content is not FoodCreated foodCreated)
            {
                return;
            }

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