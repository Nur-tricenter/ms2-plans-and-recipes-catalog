using Catalog.Application.Dto;
using Catalog.Application.Utils;
using Catalog.Application.UseCase.Query.Food;
using Catalog.Domain.ValueObjects;
using Catalog.Infrastructure.EntityFramework.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Query.Food
{
    public class SearchFoodsByCategoryHandler : IRequestHandler<SearchFoodsByCategoryQuery, Result<PagedList<FoodDto>>>
    {
        private readonly ReadDbContext _dbContext;
        private readonly ILogger<SearchFoodsByCategoryHandler> _logger;

        public SearchFoodsByCategoryHandler(ReadDbContext dbContext, ILogger<SearchFoodsByCategoryHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Result<PagedList<FoodDto>>> Handle(SearchFoodsByCategoryQuery request, CancellationToken cancellationToken = default)
        {
            try
            {
                var foods = await _dbContext.Foods
                    .Where(f => f.Category == request.Category)
                    .ToListAsync(cancellationToken);
                var dtoItems = foods.Select(f => new FoodDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Category = f.Category,
                    UnitOfMeasure = ((UnitOfMeasure)f.UnitOfMeasure).ToString(),
                    Quantity = f.Quantity,
                    Calories = f.Calories,
                    Protein = f.Protein,
                    Carbs = f.Carbs,
                    Fat = f.Fat
                }).AsQueryable();

                return Result.Ok<PagedList<FoodDto>>(
                    PagedList<FoodDto>.Create(dtoItems, request.Page, request.PageSize),
                    "Search by category successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching foods by category");
                return Result.Fail<PagedList<FoodDto>>(ex.Message);
            }
        }
    }
}