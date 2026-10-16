using Catalog.Application.Dto;
using Catalog.Application.Utils;
using Catalog.Application.UseCase.Query.Food;
using Catalog.Domain.ValueObjects;
using Catalog.Infrastructure.Persistence.StoredModel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Query.Food
{
    public class ListFoodsHandler : IRequestHandler<ListFoodsQuery, Result<PagedList<FoodDto>>>
    {
        private readonly StoredDbContext _dbContext;
        private readonly ILogger<ListFoodsHandler> _logger;

        public ListFoodsHandler(StoredDbContext dbContext, ILogger<ListFoodsHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Result<PagedList<FoodDto>>> Handle(ListFoodsQuery request, CancellationToken cancellationToken = default)
        {
            try
            {
                var foods = await _dbContext.Foods.ToListAsync(cancellationToken);
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
                    "Food list retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing foods");
                return Result.Fail<PagedList<FoodDto>>(ex.Message);
            }
        }
    }
}