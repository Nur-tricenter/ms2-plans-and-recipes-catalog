using Catalog.Application.Dto;
using Catalog.Application.UseCase.Query.Recipe;
using Catalog.Application.Utils;
using Catalog.Domain.Repository.Food;
using Catalog.Domain.Repository.Recipe;
using Shared.Core;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Query.Recipe
{
    public class GetRecipeByIdHandler : IRequestHandler<GetRecipeByIdQuery, Result<RecipeDto>>
    {
        private readonly IRecipeRepository _repository;
        private readonly IFoodRepository _foodRepository;
        private readonly ILogger<GetRecipeByIdHandler> _logger;

        public GetRecipeByIdHandler(
            IRecipeRepository repository,
            IFoodRepository foodRepository,
            ILogger<GetRecipeByIdHandler> logger)
        {
            _repository = repository;
            _foodRepository = foodRepository;
            _logger = logger;
        }

        public async Task<Result<RecipeDto>> Handle(GetRecipeByIdQuery request, CancellationToken cancellationToken = default)
        {
            try
            {
                var recipe = await _repository.FindByIdAsync(request.Id);
                if (recipe == null)
                    return Result.Fail<RecipeDto>("Recipe not found");

                var dto = new RecipeDto
                {
                    Id = recipe.Id,
                    Name = recipe.Name,
                    Instructions = recipe.Instructions
                };

                foreach (var ing in recipe.Ingredients)
                {
                    var food = await _foodRepository.FindByIdAsync(ing.FoodId);
                    dto.Ingredients.Add(new IngredientDto
                    {
                        FoodId = ing.FoodId,
                        FoodName = food?.Name ?? "Unknown",
                        Quantity = ing.Portion.Quantity
                    });
                }

                return Result.Ok<RecipeDto>(dto, "Recipe retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recipe with id: {RecipeId}", request.Id);
                return Result.Fail<RecipeDto>(ex.Message);
            }
        }
    }
}