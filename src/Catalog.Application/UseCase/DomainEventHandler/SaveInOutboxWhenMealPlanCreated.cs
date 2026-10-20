using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.Contracts.Service;
using MediatR;
using Catalog.Domain.Event.MealPlan;

namespace Catalog.Application.UseCase.DomainEventHandler
{
    public class SaveInOutboxWhenMealPlanCreated : INotificationHandler<MealPlanCreated>
    {
        private readonly IOutboxService<DomainEvent> _outboxService;

        public SaveInOutboxWhenMealPlanCreated(IOutboxService<DomainEvent> outboxService)
        {
            _outboxService = outboxService;
        }

        public async Task Handle(MealPlanCreated notification, CancellationToken cancellationToken = default)
        {
            await _outboxService.AddAsync(new OutboxMessage<DomainEvent>(notification));
        }
    }
}