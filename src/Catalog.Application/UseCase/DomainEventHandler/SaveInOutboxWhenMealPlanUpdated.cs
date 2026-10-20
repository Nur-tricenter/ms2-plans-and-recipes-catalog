using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.Contracts.Service;
using MediatR;
using Catalog.Domain.Event.MealPlan;

namespace Catalog.Application.UseCase.DomainEventHandler
{
    public class SaveInOutboxWhenMealPlanUpdated : INotificationHandler<MealPlanUpdated>
    {
        private readonly IOutboxService<DomainEvent> _outboxService;

        public SaveInOutboxWhenMealPlanUpdated(IOutboxService<DomainEvent> outboxService)
        {
            _outboxService = outboxService;
        }

        public async Task Handle(MealPlanUpdated notification, CancellationToken cancellationToken = default)
        {
            await _outboxService.AddAsync(new OutboxMessage<DomainEvent>(notification));
        }
    }
}