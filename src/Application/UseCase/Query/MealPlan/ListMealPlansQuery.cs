using Catalog.Application.Dto;
using Catalog.Application.Utils;
using Shared.Core;
using MediatR;

namespace Catalog.Application.UseCase.Query.MealPlan
{
    public class ListMealPlansQuery : IRequest<Result<IEnumerable<MealPlanDto>>>
    {
    }
}