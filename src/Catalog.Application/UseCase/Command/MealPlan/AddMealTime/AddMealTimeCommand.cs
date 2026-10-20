using Catalog.Application.Utils;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Catalog.Application.Abstractions;

namespace Catalog.Application.UseCase.Command.MealPlan.AddMealTime
{
    public class AddMealTimeCommand : ICommand
    {
        public Guid MealPlanId { get; set; }
        public int DayNumber { get; set; }
        public int MealTimeType { get; set; }
    }
}