using Catalog.Application.Utils;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Catalog.Domain.Repository.Food;
using Catalog.Domain.Repository.Recipe;
using Catalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.UseCase.Command.Recipe.AddIngredient
{
    public class AddIngredientHandler : IRequestHandler<AddIngredientCommand, Result>
    {
        private readonly IRecipeRepository _recipeRepository;
        private readonly IFoodRepository _foodRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AddIngredientHandler> _logger;

        public AddIngredientHandler(
            IRecipeRepository recipeRepository,
            IFoodRepository foodRepository,
            IUnitOfWork unitOfWork,
            ILogger<AddIngredientHandler> logger)
        {
            _recipeRepository = recipeRepository;
            _foodRepository = foodRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(AddIngredientCommand request, CancellationToken cancellationToken = default)
        {
            try
            {
                var recipe = await _recipeRepository.GetByIdAsync(request.RecipeId);
                if (recipe == null)
                    return Result.Fail("Recipe not found");

                foreach (var item in request.Ingredients)
                {
                    var food = await _foodRepository.GetByIdAsync(item.FoodId);
                    if (food == null)
                        return Result.Fail($"Food with Id {item.FoodId} does not exist");

                    recipe.AddIngredient(
                        item.FoodId,
                        new Portion(item.Quantity)
                    );
                }

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Ok($"{request.Ingredients.Count} ingredient(s) added successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding ingredient");
                return Result.Fail(ex.Message);
            }
        }
    }
}