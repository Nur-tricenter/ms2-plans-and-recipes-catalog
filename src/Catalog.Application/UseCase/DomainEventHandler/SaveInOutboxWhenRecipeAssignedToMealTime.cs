using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.Contracts.Service;
using MediatR;
using Catalog.Domain.Event.MealPlan;

namespace Catalog.Application.UseCase.DomainEventHandler
{
    public class SaveInOutboxWhenRecipeAssignedToMealTime : INotificationHandler<RecipeAssignedToMealTime>
    {
        private readonly IOutboxService<DomainEvent> _outboxService;

        public SaveInOutboxWhenRecipeAssignedToMealTime(IOutboxService<DomainEvent> outboxService)
        {
            _outboxService = outboxService;
        }

        public async Task Handle(RecipeAssignedToMealTime notification, CancellationToken cancellationToken = default)
        {
            await _outboxService.AddAsync(new OutboxMessage<DomainEvent>(notification));
        }
    }
}