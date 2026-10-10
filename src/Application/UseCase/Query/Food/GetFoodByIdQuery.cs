using Catalog.Application.Dto;
using Catalog.Application.Utils;
using Shared.Core;
using MediatR;

namespace Catalog.Application.UseCase.Query.Food
{
    public class GetFoodByIdQuery : IRequest<Result<FoodDto>>
    {
        public Guid Id { get; set; }
        public GetFoodByIdQuery() { }
        public GetFoodByIdQuery(Guid id) => Id = id;
    }
}