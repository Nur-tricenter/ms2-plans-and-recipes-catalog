using Catalog.Application.Dto;
using Catalog.Application.Utils;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Catalog.Application.Abstractions;

namespace Catalog.Application.UseCase.Query.MealPlan
{
    public class GetMealPlanByIdQuery : IQuery<Result<MealPlanDto>>
    {
        public Guid Id { get; set; }
        public GetMealPlanByIdQuery() { }
        public GetMealPlanByIdQuery(Guid id) => Id = id;
    }
}