using Joseco.Communication.External.Contracts.Services;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Catalog.Domain.Event.MealPlan;
using Catalog.Domain.Repository.MealPlan;
using Catalog.Domain.Repository.Recipe;
using Nur.Tricenter.Integration.Messages.Catalog;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.UseCase.DomainEventHandler
{
    /// <summary>
    /// Translates the outbox messages that carry <see cref="MealTimeAddedToPlan"/>,
    /// <see cref="RecipeAssignedToMealTime"/> and <see cref="RecipeRemovedFromMealTime"/> into the
    /// <see cref="MealPlanCompositionChanged"/> external contract and publishes it to RabbitMQ.
    /// </summary>
    /// <remarks>
    /// The message is a FULL snapshot of the plan, built from the write-side aggregate (and the
    /// recipe names from the write-side recipes) rather than from the read model, so it never lags
    /// behind a projection. Because every assignment emits one snapshot, several messages fly while
    /// a plan is being composed; consumers keep the latest by <c>CreatedAt</c>.
    /// </remarks>
    public class PublishIntegrationEventWhenMealPlanCompositionChanged
        : INotificationHandler<OutboxMessage<MealTimeAddedToPlan>>,
          INotificationHandler<OutboxMessage<RecipeAssignedToMealTime>>,
          INotificationHandler<OutboxMessage<RecipeRemovedFromMealTime>>
    {
        /// <summary>Exchange/routing destination for this contract. Always passed explicitly.</summary>
        public const string Destination = "meal-plan-composition-changed";

        /// <summary>Logical producer name stamped into every published contract.</summary>
        public const string Source = "catalog";

        private readonly ILogger<PublishIntegrationEventWhenMealPlanCompositionChanged> _logger;
        private readonly IExternalPublisher _publisher;
        private readonly IMealPlanRepository _mealPlans;
        private readonly IRecipeRepository _recipes;

        public PublishIntegrationEventWhenMealPlanCompositionChanged(
            ILogger<PublishIntegrationEventWhenMealPlanCompositionChanged> logger,
            IExternalPublisher publisher,
            IMealPlanRepository mealPlans,
            IRecipeRepository recipes)
        {
            _logger = logger;
            _publisher = publisher;
            _mealPlans = mealPlans;
            _recipes = recipes;
        }

        public Task Handle(OutboxMessage<MealTimeAddedToPlan> notification, CancellationToken cancellationToken = default)
            => PublishSnapshotAsync(notification.Content.PlanId, notification.CorrelationId);

        public Task Handle(OutboxMessage<RecipeAssignedToMealTime> notification, CancellationToken cancellationToken = default)
            => PublishSnapshotAsync(notification.Content.PlanId, notification.CorrelationId);

        public Task Handle(OutboxMessage<RecipeRemovedFromMealTime> notification, CancellationToken cancellationToken = default)
            => PublishSnapshotAsync(notification.Content.PlanId, notification.CorrelationId);

        private async Task PublishSnapshotAsync(Guid planId, string? correlationId)
        {
            var plan = await _mealPlans.GetByIdAsync(planId, readOnly: true);
            if (plan is null)
            {
                // The plan was removed after the event was recorded: there is no composition left
                // to announce, and retrying would loop forever on a row that can never succeed.
                _logger.LogWarning("Plan {PlanId} no longer exists; composition snapshot skipped", planId);
                return;
            }

            var recipeNames = new Dictionary<Guid, string>();
            foreach (var recipeId in plan.PlanDays
                         .SelectMany(d => d.MealTimes)
                         .SelectMany(t => t.AssignedRecipes)
                         .Select(a => a.RecipeId)
                         .Distinct())
            {
                var recipe = await _recipes.GetByIdAsync(recipeId, readOnly: true);
                recipeNames[recipeId] = recipe?.Name ?? string.Empty;
            }

            var message = new MealPlanCompositionChanged(correlationId, Source)
            {
                PlanId = plan.Id,
                Days = plan.PlanDays
                    .OrderBy(d => d.DayNumber)
                    .Select(d => new MealPlanDay
                    {
                        DayNumber = d.DayNumber,
                        MealTimes = d.MealTimes
                            .OrderBy(t => t.Order)
                            .Select(t => new MealPlanMealTime
                            {
                                Name = t.Name,
                                Order = t.Order,
                                Recipes = t.AssignedRecipes
                                    .Select(a => new MealPlanRecipe
                                    {
                                        RecipeId = a.RecipeId,
                                        RecipeName = recipeNames[a.RecipeId],
                                        Rations = a.Ration.Quantity
                                    })
                                    .ToList()
                            })
                            .ToList()
                    })
                    .ToList()
            };

            _logger.LogInformation(
                "Publishing {EventType} for plan {PlanId} ({DayCount} days)",
                message.EventType, message.PlanId, message.Days.Count);

            // declareDestination MUST be true: see PublishIntegrationEventWhenMealPlanCreated.
            await _publisher.PublishAsync(message, Destination, declareDestination: true);
        }
    }
}
