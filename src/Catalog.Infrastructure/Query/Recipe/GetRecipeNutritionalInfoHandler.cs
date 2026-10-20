using Catalog.Application.Dto;
using Catalog.Application.UseCase.Query.Recipe;
using Catalog.Application.Utils;
using Catalog.Infrastructure.Persistence.StoredModel;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Query.Recipe
{
    public class GetRecipeNutritionalInfoHandler : IRequestHandler<GetRecipeNutritionalInfoQuery, Result<NutritionalInfoDto>>
    {
        private readonly StoredDbContext _dbContext;
        private readonly ILogger<GetRecipeNutritionalInfoHandler> _logger;

        public GetRecipeNutritionalInfoHandler(StoredDbContext dbContext, ILogger<GetRecipeNutritionalInfoHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Result<NutritionalInfoDto>> Handle(GetRecipeNutritionalInfoQuery request, CancellationToken cancellationToken = default)
        {
            try
            {
                var recipe = await _dbContext.Recipes
                    .Include(r => r.Ingredients)
                    .FirstOrDefaultAsync(r => r.Id == request.RecipeId, cancellationToken);

                if (recipe == null)
                    return Result.Fail<NutritionalInfoDto>("Recipe not found");

                decimal totalQuantity = 0;
                decimal totalCalories = 0;
                decimal totalProtein = 0;
                decimal totalCarbs = 0;
                decimal totalFat = 0;

                foreach (var ing in recipe.Ingredients)
                {
                    var food = await _dbContext.Foods.FindAsync(new object?[] { ing.FoodId }, cancellationToken);
                    if (food == null) continue;

                    var portion = ing.PortionQuantity;
                    totalQuantity += portion;
                    totalCalories += portion * food.Calories / 100;
                    totalProtein += portion * food.Protein / 100;
                    totalCarbs += portion * food.Carbs / 100;
                    totalFat += portion * food.Fat / 100;
                }

                return Result.Ok<NutritionalInfoDto>(new NutritionalInfoDto
                {
                    Quantity = totalQuantity,
                    Calories = totalCalories,
                    Protein = totalProtein,
                    Carbs = totalCarbs,
                    Fat = totalFat
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
