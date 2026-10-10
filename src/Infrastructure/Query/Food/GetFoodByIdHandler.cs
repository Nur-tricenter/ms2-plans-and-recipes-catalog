using Catalog.Application.Dto;
using Catalog.Application.UseCase.Query.Food;
using Catalog.Application.Utils;
using Catalog.Domain.Repository.Food;
using Shared.Core;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Query.Food
{
    public class GetFoodByIdHandler : IRequestHandler<GetFoodByIdQuery, Result<FoodDto>>
    {
        private readonly IFoodRepository _repository;
        private readonly ILogger<GetFoodByIdHandler> _logger;

        public GetFoodByIdHandler(IFoodRepository repository, ILogger<GetFoodByIdHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Result<FoodDto>> Handle(GetFoodByIdQuery request, CancellationToken cancellationToken = default)
        {
            try
            {
                var food = await _repository.FindByIdAsync(request.Id);
                if (food == null)
                    return Result.Fail<FoodDto>("Food not found");

                return Result.Ok<FoodDto>(new FoodDto
                {
                    Id = food.Id,
                    Name = food.Name,
                    Category = food.Category,
                    UnitOfMeasure = food.UnitOfMeasure.ToString(),
                    Quantity = food.BaseNutritionalInfo.Quantity,
                    Calories = food.BaseNutritionalInfo.Calories,
                    Protein = food.BaseNutritionalInfo.Protein,
                    Carbs = food.BaseNutritionalInfo.Carbs,
                    Fat = food.BaseNutritionalInfo.Fat
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