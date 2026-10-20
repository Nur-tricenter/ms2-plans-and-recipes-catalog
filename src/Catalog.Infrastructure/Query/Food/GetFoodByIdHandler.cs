using Catalog.Application.Dto;
using Catalog.Application.UseCase.Query.Food;
using Catalog.Application.Utils;
using Catalog.Infrastructure.Persistence.StoredModel;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Query.Food
{
    public class GetFoodByIdHandler : IRequestHandler<GetFoodByIdQuery, Result<FoodDto>>
    {
        private readonly StoredDbContext _dbContext;
        private readonly ILogger<GetFoodByIdHandler> _logger;

        public GetFoodByIdHandler(StoredDbContext dbContext, ILogger<GetFoodByIdHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Result<FoodDto>> Handle(GetFoodByIdQuery request, CancellationToken cancellationToken = default)
        {
            try
            {
                var food = await _dbContext.Foods.FindAsync(new object?[] { request.Id }, cancellationToken);
                if (food == null)
                    return Result.Fail<FoodDto>("Food not found");

                return Result.Ok<FoodDto>(new FoodDto
                {
                    Id = food.Id,
                    Name = food.Name,
                    Category = food.Category,
                    UnitOfMeasure = ((Catalog.Domain.ValueObjects.UnitOfMeasure)food.UnitOfMeasure).ToString(),
                    Quantity = food.Quantity,
                    Calories = food.Calories,
                    Protein = food.Protein,
                    Carbs = food.Carbs,
                    Fat = food.Fat
                }, "Food retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving food with id: {FoodId}", request.Id);
                return Result.Fail<FoodDto>(ex.Message);
            }
        }
    }
}
