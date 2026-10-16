using Catalog.Application.Dto;
using Catalog.Application.Utils;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Catalog.Application.Abstractions;

namespace Catalog.Application.UseCase.Query.Recipe
{
    public class GetRecipeByIdQuery : IQuery<Result<RecipeDto>>
    {
        public Guid Id { get; set; }
        public GetRecipeByIdQuery() { }
        public GetRecipeByIdQuery(Guid id) => Id = id;
    }
}