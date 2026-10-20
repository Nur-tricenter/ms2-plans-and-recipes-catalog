using Catalog.Application.UseCase.Query.MealPlan;
using Catalog.Application.Utils;
using Catalog.Infrastructure.Persistence.StoredModel;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Query.MealPlan
{
    public class GetMealPlanCompositionHandler : IRequestHandler<GetMealPlanCompositionQuery, Result<MealPlanCompositionDto>>
    {
        private readonly StoredDbContext _dbContext;
        private readonly ILogger<GetMealPlanCompositionHandler> _logger;

        public GetMealPlanCompositionHandler(StoredDbContext dbContext, ILogger<GetMealPlanCompositionHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Result<MealPlanCompositionDto>> Handle(GetMealPlanCompositionQuery request, CancellationToken cancellationToken = default)
        {
            try
            {
                var mealPlan = await _dbContext.MealPlans
                    .Include(p => p.PlanDays)
                    .ThenInclude(d => d.MealTimes)
                    .ThenInclude(m => m.AssignedRecipes)
                    .FirstOrDefaultAsync(p => p.Id == request.MealPlanId, cancellationToken);

                if (mealPlan == null)
                    return Result.Fail<MealPlanCompositionDto>("Meal plan not found");

                var dto = new MealPlanCompositionDto
                {
                    MealPlanId = mealPlan.Id,
                    Name = mealPlan.Name
                };

                foreach (var day in mealPlan.PlanDays.OrderBy(d => d.DayNumber))
                {
                    var dayDto = new PlanDayCompositionDto { DayNumber = day.DayNumber };

                    foreach (var mealTime in day.MealTimes.OrderBy(t => t.Order))
                    {
                        var mtDto = new MealTimeCompositionDto
                        {
                            MealTimeId = mealTime.Id,
                            Name = mealTime.Name,
                            Order = mealTime.Order
                        };

                        foreach (var asig in mealTime.AssignedRecipes)
                        {
                            var recipe = await _dbContext.Recipes
                                .Include(r => r.Ingredients)
                                .FirstOrDefaultAsync(r => r.Id == asig.RecipeId, cancellationToken);
                            if (recipe == null) continue;

                            var recipeDto = new AssignedRecipeCompositionDto
                            {
                                RecipeId = recipe.Id,
                                RecipeName = recipe.Name,
                                Ration = new RationDto { Quantity = asig.RationQuantity }
                            };

                            foreach (var ing in recipe.Ingredients)
                            {
                                var food = await _dbContext.Foods.FindAsync(new object?[] { ing.FoodId }, cancellationToken);
                                recipeDto.Ingredients.Add(new IngredientCompositionDto
                                {
                                    FoodId = ing.FoodId,
                                    FoodName = food?.Name ?? "Unknown",
                                    Portion = new PortionDto
                                    {
                                        Quantity = ing.PortionQuantity
                                    }
                                });
                            }

                            mtDto.Recipes.Add(recipeDto);
                        }

                        dayDto.MealTimes.Add(mtDto);
                    }

                    dto.Days.Add(dayDto);
                }

                return Result.Ok<MealPlanCompositionDto>(dto, "Meal plan composition retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting meal plan composition for {MealPlanId}", request.MealPlanId);
                return Result.Fail<MealPlanCompositionDto>(ex.Message);
            }
        }
    }
}
