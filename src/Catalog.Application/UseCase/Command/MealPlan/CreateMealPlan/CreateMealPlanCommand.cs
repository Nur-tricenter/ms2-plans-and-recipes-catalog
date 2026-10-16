using Catalog.Application.Utils;
using Shared.Core;
using MediatR;

namespace Catalog.Application.UseCase.Command.MealPlan.CreateMealPlan
{
    public class CreateMealPlanCommand : IRequest<Result<Guid>>
    {
        public string Name { get; set; } = null!;
        public string DurationType { get; set; } = null!;
        public int MealsPerDay { get; set; }
    }
}