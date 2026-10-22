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
                // The author is taken from the BEARER TOKEN, which the middleware has already
                // validated against the identity service, and NOT from the request body. That is
                // the whole point: a body field would let any authenticated caller name somebody
                // else as the plan's owner, and a plan's author is an authorization fact, not an
                // input.
                //
                // There is deliberately no lookup of PlanAuthors here. The token IS the proof the
                // account exists; a second check against an eventually-consistent projection would
                // turn transient event lag into a user-visible failure and would stop a caller from
                // creating a plan right after registering.
                if (request.AuthorIdFromToken == Guid.Empty)
                {
                    return Result.Fail<Guid>(
                        "The request must be authenticated: the plan's author is taken from the " +
                        "bearer token.");
                }

                var duration = new PlanDuration(
                    Enum.Parse<DurationType>(request.DurationType)
                );

                var mealPlan = _factory.Create(
                    request.Name, duration, request.MealsPerDay, request.AuthorIdFromToken);

                await _repository.AddAsync(mealPlan);
                await _unitOfWork.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Meal plan {PlanId} created by account {AuthorId}.",
                    mealPlan.Id, request.AuthorIdFromToken);

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