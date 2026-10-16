using Catalog.Application.Dto;
using Catalog.Application.Utils;
using Catalog.Application.UseCase.Query.Recipe;
using Catalog.Infrastructure.EntityFramework.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Query.Recipe
{
    public class ListRecipesHandler : IRequestHandler<ListRecipesQuery, Result<PagedList<RecipeDto>>>
    {
        private readonly ReadDbContext _dbContext;
        private readonly ILogger<ListRecipesHandler> _logger;

        public ListRecipesHandler(ReadDbContext dbContext, ILogger<ListRecipesHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Result<PagedList<RecipeDto>>> Handle(ListRecipesQuery request, CancellationToken cancellationToken = default)
        {
            try
            {
                var recipes = await _dbContext.Recipes.Include(r => r.Ingredients).ToListAsync(cancellationToken);
                var dtoItems = recipes.Select(r =>
                {
                    var dto = new RecipeDto
                    {
                        Id = r.Id,
                        Name = r.Name,
                        Instructions = r.Instructions
                    };
                    foreach (var ing in r.Ingredients)
                    {
                        dto.Ingredients.Add(new IngredientDto
                        {
                            FoodId = ing.FoodId,
                            Quantity = ing.PortionQuantity
                        });
                    }
                    return dto;
                }).AsQueryable();

                return Result.Ok<PagedList<RecipeDto>>(
                    PagedList<RecipeDto>.Create(dtoItems, request.Page, request.PageSize),
                    "Recipe list retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing recipes");
                return Result.Fail<PagedList<RecipeDto>>(ex.Message);
            }
        }
    }
}