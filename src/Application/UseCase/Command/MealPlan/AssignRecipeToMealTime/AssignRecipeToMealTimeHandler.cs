using Catalog.Application.Utils;
using Shared.Core;
using MediatR;
using Catalog.Domain.Repository.MealPlan;
using Catalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.UseCase.Command.MealPlan.AssignRecipeToMealTime
{
    public class AssignRecipeToMealTimeHandler : IRequestHandler<AssignRecipeToMealTimeCommand, Result>
    {
        private readonly IMealPlanRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AssignRecipeToMealTimeHandler> _logger;

        public AssignRecipeToMealTimeHandler(
            IMealPlanRepository repository,
            IUnitOfWork unitOfWork,
            ILogger<AssignRecipeToMealTimeHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(AssignRecipeToMealTimeCommand request, CancellationToken cancellationToken = default)
        {
            try
            {
                var mealPlan = await _repository.FindByIdAsync(request.MealPlanId);
                if (mealPlan == null)
                    return Result.Fail("Meal plan not found");

                foreach (var item in request.Recipes)
                {
                    mealPlan.AssignRecipeToMealTime(
                        request.MealTimeId,
                        item.RecipeId,
                        new Ration(item.RationQuantity)
                    );
                }

                await _unitOfWork.Commit();
                return Result.Ok($"{request.Recipes.Count} recipe(s) assigned successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning recipe to meal time");
                return Result.Fail(ex.Message);
            }
        }
    }
}