using Catalog.Application.Utils;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Catalog.Domain.Factory.Food;
using Catalog.Domain.Repository.Food;
using Catalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.UseCase.Command.Food.CreateFood
{
    public class CreateFoodHandler : IRequestHandler<CreateFoodCommand, Result<Guid>>
    {
        private readonly IFoodRepository _repository;
        private readonly IFoodFactory _factory;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateFoodHandler> _logger;

        public CreateFoodHandler(
            IFoodRepository repository,
            IFoodFactory factory,
            IUnitOfWork unitOfWork,
            ILogger<CreateFoodHandler> logger)
        {
            _repository = repository;
            _factory = factory;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(CreateFoodCommand request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Enum.IsDefined(typeof(UnitOfMeasure), request.UnitOfMeasure))
                {
                    var validValues = string.Join(", ", Enum.GetValues<UnitOfMeasure>().Select(v => $"{(int)v}={v}"));
                    return Result.Fail<Guid>($"Invalid unit of measure: {request.UnitOfMeasure}. Valid values: {validValues}");
                }

                var name = request.Name.ToUpperInvariant();
                var category = request.Category.ToUpperInvariant();
                var unitOfMeasure = (UnitOfMeasure)request.UnitOfMeasure;
                var info = new NutritionalInfo(
                    request.Quantity, request.Calories, request.Protein,
                    request.Carbs, request.Fat
                );

                var food = _factory.Create(name, category, unitOfMeasure, info);

                await _repository.AddAsync(food);
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<Guid>.Ok(food.Id, "Food created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating food");
                return Result.Fail<Guid>(ex.Message);
            }
        }
    }
}