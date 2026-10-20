using Catalog.Application.Utils;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Catalog.Domain;
using Catalog.Domain.Factory.MealPlan;
using Catalog.Domain.Repository.MealPlan;
using Catalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.UseCase.Command.MealPlan.CreateMealPlan
{
    public class CreateMealPlanHandler : IRequestHandler<CreateMealPlanCommand, Result<Guid>>
    {
        private readonly IMealPlanRepository _repository;
        private readonly IMealPlanFactory _factory;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateMealPlanHandler> _logger;

        public CreateMealPlanHandler(
            IMealPlanRepository repository,
            IMealPlanFactory factory,
            IUnitOfWork unitOfWork,
            ILogger<CreateMealPlanHandler> logger)
        {
            _repository = repository;
            _factory = factory;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(CreateMealPlanCommand request, CancellationToken cancellationToken = default)
        {
            try
            {
                var duration = new PlanDuration(
                    Enum.Parse<DurationType>(request.DurationType)
                );

                var mealPlan = _factory.Create(request.Name, duration, request.MealsPerDay);

                await _repository.AddAsync(mealPlan);
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<Guid>.Ok(mealPlan.Id, "Meal plan created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating meal plan");
                return Result.Fail<Guid>(ex.Message);
            }
        }
    }
}