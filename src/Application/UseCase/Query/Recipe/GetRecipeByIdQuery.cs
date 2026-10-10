using Catalog.Application.Dto;
using Catalog.Application.Utils;
using Shared.Core;
using MediatR;

namespace Catalog.Application.UseCase.Query.Recipe
{
    public class GetRecipeByIdQuery : IRequest<Result<RecipeDto>>
    {
        public Guid Id { get; set; }
        public GetRecipeByIdQuery() { }
        public GetRecipeByIdQuery(Guid id) => Id = id;
    }
}