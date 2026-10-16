using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.Contracts.Service;
using MediatR;
using Catalog.Domain.Event.Recipe;

namespace Catalog.Application.UseCase.DomainEventHandler
{
    public class SaveInOutboxWhenRecipeCreated : INotificationHandler<RecipeCreated>
    {
        private readonly IOutboxService<DomainEvent> _outboxService;

        public SaveInOutboxWhenRecipeCreated(IOutboxService<DomainEvent> outboxService)
        {
            _outboxService = outboxService;
        }

        public async Task Handle(RecipeCreated notification, CancellationToken cancellationToken = default)
        {
            await _outboxService.AddAsync(new OutboxMessage<DomainEvent>(notification));
        }
    }
}