using Catalog.Application.Utils;
using Shared.Core;
using MediatR;
using Catalog.Domain.Repository.Recipe;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.UseCase.Command.Recipe.RemoveIngredient
{
    public class RemoveIngredientHandler : IRequestHandler<RemoveIngredientCommand, Result>
    {
        private readonly IRecipeRepository _recipeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RemoveIngredientHandler> _logger;

        public RemoveIngredientHandler(
            IRecipeRepository recipeRepository,
            IUnitOfWork unitOfWork,
            ILogger<RemoveIngredientHandler> logger)
        {
            _recipeRepository = recipeRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(RemoveIngredientCommand request, CancellationToken cancellationToken = default)
        {
            try
            {
                var recipe = await _recipeRepository.FindByIdAsync(request.RecipeId);
                if (recipe == null)
                    return Result.Fail("Recipe not found");

                recipe.RemoveIngredient(request.FoodId);

                await _unitOfWork.Commit();
                return Result.Ok("Ingredient removed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing ingredient");
                return Result.Fail(ex.Message);
            }
        }
    }
}