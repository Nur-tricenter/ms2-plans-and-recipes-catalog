using Catalog.Application.Dto;
using Catalog.Application.Utils;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Catalog.Application.Abstractions;

namespace Catalog.Application.UseCase.Query.Recipe
{
    public class ListRecipesQuery : IQuery<Result<PagedList<RecipeDto>>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}