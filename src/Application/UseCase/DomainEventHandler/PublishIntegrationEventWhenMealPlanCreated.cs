using Shared.Core;
using MediatR;
using Catalog.Domain.Event.MealPlan;
using Shared.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.UseCase.DomainEventHandler
{
    public class PublishIntegrationEventWhenMealPlanCreated
        : INotificationHandler<MealPlanCreated>
    {
        private readonly ILogger<PublishIntegrationEventWhenMealPlanCreated> _logger;

        public PublishIntegrationEventWhenMealPlanCreated(
            ILogger<PublishIntegrationEventWhenMealPlanCreated> logger)
        {
            _logger = logger;
        }

        public async Task Handle(MealPlanCreated notification, CancellationToken cancellationToken = default)
        {
            var integrationEvent = new CatalogoV1PlanPublicado
            {
                PlanId = notification.PlanId,
                Name = notification.Name,
                DurationType = notification.DurationType,
                TotalDays = notification.TotalDays,
                MealsPerDay = notification.MealsPerDay
            };

            _logger.LogInformation(
                "Published {EventType} for plan {PlanId}: {Name}",
                integrationEvent.EventType, integrationEvent.PlanId, integrationEvent.Name);

            await Task.CompletedTask;
        }
    }
}