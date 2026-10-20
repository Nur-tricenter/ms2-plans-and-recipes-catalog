using Catalog.Application.Utils;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Catalog.Domain.Repository.Food;
using Catalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.UseCase.Command.Food.UpdateFood
{
    public class UpdateFoodHandler : IRequestHandler<UpdateFoodCommand, Result>
    {
        private readonly IFoodRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateFoodHandler> _logger;

        public UpdateFoodHandler(
            IFoodRepository repository,
            IUnitOfWork unitOfWork,
            ILogger<UpdateFoodHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(UpdateFoodCommand request, CancellationToken cancellationToken = default)
        {
            try
            {
                var food = await _repository.GetByIdAsync(request.FoodId);
                if (food == null)
                    return Result.Fail("Food not found");

                if (!Enum.IsDefined(typeof(UnitOfMeasure), request.UnitOfMeasure))
                {
                    var validValues = string.Join(", ", Enum.GetValues<UnitOfMeasure>().Select(v => $"{(int)v}={v}"));
                    return Result.Fail($"Invalid unit of measure: {request.UnitOfMeasure}. Valid values: {validValues}");
                }

                var name = request.Name.ToUpperInvariant();
                var category = request.Category.ToUpperInvariant();
                var unitOfMeasure = (UnitOfMeasure)request.UnitOfMeasure;
                var info = new NutritionalInfo(
                    request.Quantity, request.Calories, request.Protein,
                    request.Carbs, request.Fat
                );

                food.Update(name, category, unitOfMeasure, info);

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Ok("Food updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating food");
                return Result.Fail(ex.Message);
            }
        }
    }
}