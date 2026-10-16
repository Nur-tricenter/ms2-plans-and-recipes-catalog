using Catalog.Application.Dto;
using Catalog.Application.UseCase.Query.Recipe;
using Catalog.Application.Utils;
using Catalog.Domain.Repository.Food;
using Catalog.Domain.Repository.Recipe;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Query.Recipe
{
    public class GetRecipeNutritionalInfoHandler : IRequestHandler<GetRecipeNutritionalInfoQuery, Result<NutritionalInfoDto>>
    {
        private readonly IRecipeRepository _recipeRepository;
        private readonly IFoodRepository _foodRepository;
        private readonly ILogger<GetRecipeNutritionalInfoHandler> _logger;

        public GetRecipeNutritionalInfoHandler(
            IRecipeRepository recipeRepository,
            IFoodRepository foodRepository,
            ILogger<GetRecipeNutritionalInfoHandler> logger)
        {
            _recipeRepository = recipeRepository;
            _foodRepository = foodRepository;
            _logger = logger;
        }

        public async Task<Result<NutritionalInfoDto>> Handle(GetRecipeNutritionalInfoQuery request, CancellationToken cancellationToken = default)
        {
            try
            {
                var recipe = await _recipeRepository.GetByIdAsync(request.RecipeId);
                if (recipe == null)
                    return Result.Fail<NutritionalInfoDto>("Recipe not found");

                var info = recipe.CalculateTotalNutritionalInfo(id =>
                {
                    var task = _foodRepository.GetByIdAsync(id);
                    task.Wait();
                    return task.Result!;
                });

                return Result.Ok<NutritionalInfoDto>(new NutritionalInfoDto
                {
                    Quantity = info.Quantity,
                    Calories = info.Calories,
                    Protein = info.Protein,
                    Carbs = info.Carbs,
                    Fat = info.Fat
                }, "Nutritional information retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating nutritional info for recipe {RecipeId}", request.RecipeId);
                return Result.Fail<NutritionalInfoDto>(ex.Message);
            }
        }
    }
}