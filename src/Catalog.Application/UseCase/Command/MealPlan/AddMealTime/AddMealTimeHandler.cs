using Catalog.Application.Utils;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Catalog.Domain.Repository.MealPlan;
using Catalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.UseCase.Command.MealPlan.AddMealTime
{
    public class AddMealTimeHandler : IRequestHandler<AddMealTimeCommand, Result>
    {
        private readonly IMealPlanRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AddMealTimeHandler> _logger;

        public AddMealTimeHandler(
            IMealPlanRepository repository,
            IUnitOfWork unitOfWork,
            ILogger<AddMealTimeHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(AddMealTimeCommand request, CancellationToken cancellationToken = default)
        {
            try
            {
                var mealPlan = await _repository.GetByIdAsync(request.MealPlanId);
                if (mealPlan == null)
                    return Result.Fail("Meal plan not found");

                if (!Enum.IsDefined(typeof(MealTimeType), request.MealTimeType))
                {
                    var validValues = string.Join(", ", Enum.GetValues<MealTimeType>().Select(v => $"{(int)v}={v.ToDisplayName()}"));
                    return Result.Fail($"Invalid meal time type: {request.MealTimeType}. Valid values: {validValues}");
                }

                var type = (MealTimeType)request.MealTimeType;
                _logger.LogInformation("Adding meal time: MealPlanId={MealPlanId}, Day={Day}, Type={Type}, Name={Name}, Order={Order}",
                    mealPlan.Id, request.DayNumber, type, type.ToDisplayName(), (int)type);

                var day = mealPlan.PlanDays.FirstOrDefault(d => d.DayNumber == request.DayNumber);
                if (day == null)
                    return Result.Fail($"Day {request.DayNumber} does not exist in the plan");

                if (day.MealTimes.Any(t => t.Order == (int)type))
                    return Result.Fail($"Meal time with order {(int)type} already exists on day {request.DayNumber}");

                mealPlan.AddMealTimeToDay(request.DayNumber, type);

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Ok("Meal time added successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding meal time");
                return Result.Fail(ex.Message);
            }
        }
    }
}