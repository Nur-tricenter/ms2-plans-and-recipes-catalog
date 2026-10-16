using Catalog.Application.Dto;
using Catalog.Application.Utils;
using Shared.Core;
using MediatR;

namespace Catalog.Application.UseCase.Query.Food
{
    public class SearchFoodsByCategoryQuery : IRequest<Result<PagedList<FoodDto>>>
    {
        public string Category { get; set; } = null!;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public SearchFoodsByCategoryQuery() { }
        public SearchFoodsByCategoryQuery(string category) => Category = category;
    }
}