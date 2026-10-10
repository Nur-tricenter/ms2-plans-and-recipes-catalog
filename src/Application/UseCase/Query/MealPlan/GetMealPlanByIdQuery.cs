using Catalog.Application.Dto;
using Catalog.Application.Utils;
using Shared.Core;
using MediatR;

namespace Catalog.Application.UseCase.Query.MealPlan
{
    public class GetMealPlanByIdQuery : IRequest<Result<MealPlanDto>>
    {
        public Guid Id { get; set; }
        public GetMealPlanByIdQuery() { }
        public GetMealPlanByIdQuery(Guid id) => Id = id;
    }
}