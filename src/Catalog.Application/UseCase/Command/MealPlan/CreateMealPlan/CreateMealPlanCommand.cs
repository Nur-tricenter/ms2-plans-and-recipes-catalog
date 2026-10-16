using Catalog.Application.Utils;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Catalog.Application.Abstractions;

namespace Catalog.Application.UseCase.Command.MealPlan.CreateMealPlan
{
    public class CreateMealPlanCommand : ICommand<Result<Guid>>
    {
        public string Name { get; set; } = null!;
        public string DurationType { get; set; } = null!;
        public int MealsPerDay { get; set; }
    }
}