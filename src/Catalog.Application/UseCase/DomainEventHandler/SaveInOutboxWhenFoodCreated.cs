using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.Contracts.Service;
using MediatR;
using Catalog.Domain.Event.Food;

namespace Catalog.Application.UseCase.DomainEventHandler
{
    public class SaveInOutboxWhenFoodCreated : INotificationHandler<FoodCreated>
    {
        private readonly IOutboxService<DomainEvent> _outboxService;

        public SaveInOutboxWhenFoodCreated(IOutboxService<DomainEvent> outboxService)
        {
            _outboxService = outboxService;
        }

        public async Task Handle(FoodCreated notification, CancellationToken cancellationToken = default)
        {
            await _outboxService.AddAsync(new OutboxMessage<DomainEvent>(notification));
        }
    }
}