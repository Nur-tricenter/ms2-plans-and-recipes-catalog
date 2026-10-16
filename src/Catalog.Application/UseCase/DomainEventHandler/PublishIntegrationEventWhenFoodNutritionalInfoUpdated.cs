using Joseco.DDD.Core.Abstractions;
using MediatR;
using Catalog.Domain.Event.Food;
using Catalog.Application.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.UseCase.DomainEventHandler
{
    public class PublishIntegrationEventWhenFoodNutritionalInfoUpdated
        : INotificationHandler<FoodNutritionalInfoUpdated>
    {
        private readonly ILogger<PublishIntegrationEventWhenFoodNutritionalInfoUpdated> _logger;

        public PublishIntegrationEventWhenFoodNutritionalInfoUpdated(
            ILogger<PublishIntegrationEventWhenFoodNutritionalInfoUpdated> logger)
        {
            _logger = logger;
        }

        public async Task Handle(FoodNutritionalInfoUpdated notification, CancellationToken cancellationToken = default)
        {
            var integrationEvent = new CatalogoV1AlimentoNutricionalActualizado
            {
                FoodId = notification.FoodId,
                PreviousCalories = (double)notification.PreviousNutritionalInfo.Calories,
                PreviousProtein = (double)notification.PreviousNutritionalInfo.Protein,
                PreviousFat = (double)notification.PreviousNutritionalInfo.Fat,
                PreviousCarbs = (double)notification.PreviousNutritionalInfo.Carbs,
                NewCalories = (double)notification.NewNutritionalInfo.Calories,
                NewProtein = (double)notification.NewNutritionalInfo.Protein,
                NewFat = (double)notification.NewNutritionalInfo.Fat,
                NewCarbs = (double)notification.NewNutritionalInfo.Carbs
            };

            _logger.LogInformation(
                "Published {EventType} for food {FoodId}",
                integrationEvent.EventType, integrationEvent.FoodId);

            await Task.CompletedTask;
        }
    }
}