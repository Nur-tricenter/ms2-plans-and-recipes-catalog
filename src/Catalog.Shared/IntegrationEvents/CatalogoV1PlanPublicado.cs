using Shared.Core;

namespace Shared.IntegrationEvents
{
    public record CatalogoV1PlanPublicado : IntegrationEvent
    {
        public string EventType => "CatalogoV1.PlanPublished";
        public Guid PlanId { get; init; }
        public string Name { get; init; } = null!;
        public string DurationType { get; init; } = null!;
        public int TotalDays { get; init; }
        public int MealsPerDay { get; init; }
    }
}