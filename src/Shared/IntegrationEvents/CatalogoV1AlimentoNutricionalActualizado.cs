using Shared.Core;

namespace Shared.IntegrationEvents
{
    public record CatalogoV1AlimentoNutricionalActualizado : IntegrationEvent
    {
        public string EventType => "CatalogoV1.FoodNutritionalInfoUpdated";
        public Guid FoodId { get; init; }
        public double PreviousCalories { get; init; }
        public double PreviousProtein { get; init; }
        public double PreviousFat { get; init; }
        public double PreviousCarbs { get; init; }
        public double NewCalories { get; init; }
        public double NewProtein { get; init; }
        public double NewFat { get; init; }
        public double NewCarbs { get; init; }
    }
}