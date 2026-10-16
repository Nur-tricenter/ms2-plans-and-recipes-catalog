using Catalog.Application.Dto;
using Catalog.Application.Utils;
using Catalog.Application.UseCase.Query.MealPlan;
using Catalog.Infrastructure.EntityFramework.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Query.MealPlan
{
    public class ListMealPlansHandler : IRequestHandler<ListMealPlansQuery, Result<IEnumerable<MealPlanDto>>>
    {
        private readonly ReadDbContext _dbContext;
        private readonly ILogger<ListMealPlansHandler> _logger;

        public ListMealPlansHandler(ReadDbContext dbContext, ILogger<ListMealPlansHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Result<IEnumerable<MealPlanDto>>> Handle(ListMealPlansQuery request, CancellationToken cancellationToken = default)
        {
            try
            {
                var mealPlans = await _dbContext.MealPlans.ToListAsync(cancellationToken);
                var dtoItems = mealPlans.Select(p => new MealPlanDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    DurationType = p.DurationType,
                    TotalDays = p.DurationType == "FIFTEEN_DAYS" ? 15 : 30,
                    MealsPerDay = p.MealsPerDay
                }).ToList();

                return Result.Ok<IEnumerable<MealPlanDto>>(dtoItems, "Meal plan list retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing meal plans");
                return Result.Fail<IEnumerable<MealPlanDto>>(ex.Message);
            }
        }
    }
}