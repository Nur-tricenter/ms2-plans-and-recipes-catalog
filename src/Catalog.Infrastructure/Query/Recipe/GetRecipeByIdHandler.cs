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
    public class GetRecipeByIdHandler : IRequestHandler<GetRecipeByIdQuery, Result<RecipeDto>>
    {
        private readonly StoredDbContext _dbContext;
        private readonly ILogger<GetRecipeByIdHandler> _logger;

        public GetRecipeByIdHandler(StoredDbContext dbContext, ILogger<GetRecipeByIdHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Result<RecipeDto>> Handle(GetRecipeByIdQuery request, CancellationToken cancellationToken = default)
        {
            try
            {
                var recipe = await _dbContext.Recipes
                    .Include(r => r.Ingredients)
                    .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

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
                    var food = await _dbContext.Foods.FindAsync(new object?[] { ing.FoodId }, cancellationToken);
                    dto.Ingredients.Add(new IngredientDto
                    {
                        FoodId = ing.FoodId,
                        FoodName = food?.Name ?? "Unknown",
                        Quantity = ing.PortionQuantity
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
