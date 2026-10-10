using Catalog.Application.Utils;
using Shared.Core;
using MediatR;
using Catalog.Domain.Repository.MealPlan;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.UseCase.Command.MealPlan.RemoveRecipeFromMealTime
{
    public class RemoveRecipeFromMealTimeHandler : IRequestHandler<RemoveRecipeFromMealTimeCommand, Result>
    {
        private readonly IMealPlanRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RemoveRecipeFromMealTimeHandler> _logger;

        public RemoveRecipeFromMealTimeHandler(
            IMealPlanRepository repository,
            IUnitOfWork unitOfWork,
            ILogger<RemoveRecipeFromMealTimeHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(RemoveRecipeFromMealTimeCommand request, CancellationToken cancellationToken = default)
        {
            try
            {
                var mealPlan = await _repository.FindByIdAsync(request.MealPlanId);
                if (mealPlan == null)
                    return Result.Fail("Meal plan not found");

                mealPlan.RemoveRecipeFromMealTime(request.MealTimeId, request.RecipeId);

                await _unitOfWork.Commit();
                return Result.Ok("Recipe removed from meal time successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing recipe from meal time");
                return Result.Fail(ex.Message);
            }
        }
    }
}