using Catalog.Application.Dto;
using Catalog.Application.Utils;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Catalog.Application.Abstractions;

namespace Catalog.Application.UseCase.Query.Recipe
{
    public class GetRecipeNutritionalInfoQuery : IQuery<Result<NutritionalInfoDto>>
    {
        public Guid RecipeId { get; set; }
        public GetRecipeNutritionalInfoQuery() { }
        public GetRecipeNutritionalInfoQuery(Guid recipeId) => RecipeId = recipeId;
    }

    public class NutritionalInfoDto
    {
        public decimal Quantity { get; set; }
        public decimal Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbs { get; set; }
        public decimal Fat { get; set; }
    }
}