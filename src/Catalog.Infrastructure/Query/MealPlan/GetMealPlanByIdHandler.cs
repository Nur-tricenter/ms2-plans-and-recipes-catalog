using Catalog.Application.Dto;
using Catalog.Application.UseCase.Query.MealPlan;
using Catalog.Application.Utils;
using Catalog.Infrastructure.Persistence.StoredModel;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Query.MealPlan
{
    public class GetMealPlanByIdHandler : IRequestHandler<GetMealPlanByIdQuery, Result<MealPlanDto>>
    {
        private readonly StoredDbContext _dbContext;
        private readonly ILogger<GetMealPlanByIdHandler> _logger;

        public GetMealPlanByIdHandler(StoredDbContext dbContext, ILogger<GetMealPlanByIdHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Result<MealPlanDto>> Handle(GetMealPlanByIdQuery request, CancellationToken cancellationToken = default)
        {
            try
            {
                var mealPlan = await _dbContext.MealPlans
                    .Include(p => p.PlanDays)
                    .ThenInclude(d => d.MealTimes)
                    .ThenInclude(m => m.AssignedRecipes)
                    .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

                if (mealPlan == null)
                    return Result.Fail<MealPlanDto>("Meal plan not found");

                var dto = new MealPlanDto
                {
                    Id = mealPlan.Id,
                    Name = mealPlan.Name,
                    DurationType = mealPlan.DurationType,
                    TotalDays = mealPlan.PlanDays.Count,
                    MealsPerDay = mealPlan.MealsPerDay
                };

                foreach (var day in mealPlan.PlanDays.OrderBy(d => d.DayNumber))
                {
                    var dayDto = new PlanDayDto { DayNumber = day.DayNumber };
                    foreach (var mealTime in day.MealTimes.OrderBy(t => t.Order))
                    {
                        var mtDto = new MealTimeDto
                        {
                            Name = mealTime.Name,
                            Order = mealTime.Order
                        };
                        foreach (var asig in mealTime.AssignedRecipes)
                        {
                            var recipe = await _dbContext.Recipes.FindAsync(new object?[] { asig.RecipeId }, cancellationToken);
                            mtDto.Recipes.Add(new AssignedRecipeDto
                            {
                                RecipeId = asig.RecipeId,
                                RecipeName = recipe?.Name ?? "Unknown",
                                Ration = asig.RationQuantity
                            });
                        }
                        dayDto.MealTimes.Add(mtDto);
                    }
                    dto.Days.Add(dayDto);
                }

                return Result.Ok<MealPlanDto>(dto, "Meal plan retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving meal plan with id: {MealPlanId}", request.Id);
                return Result.Fail<MealPlanDto>(ex.Message);
            }
        }
    }
}
