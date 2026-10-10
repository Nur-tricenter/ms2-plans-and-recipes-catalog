using Catalog.Application.Utils;
using Shared.Core;
using MediatR;

namespace Catalog.Application.UseCase.Command.MealPlan.AddMealTime
{
    public class AddMealTimeCommand : IRequest<Result>
    {
        public Guid MealPlanId { get; set; }
        public int DayNumber { get; set; }
        public int MealTimeType { get; set; }
    }
}