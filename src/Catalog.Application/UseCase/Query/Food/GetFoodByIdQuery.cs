using Catalog.Application.Dto;
using Catalog.Application.Utils;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Catalog.Application.Abstractions;

namespace Catalog.Application.UseCase.Query.Food
{
    public class GetFoodByIdQuery : IQuery<Result<FoodDto>>
    {
        public Guid Id { get; set; }
        public GetFoodByIdQuery() { }
        public GetFoodByIdQuery(Guid id) => Id = id;
    }
}