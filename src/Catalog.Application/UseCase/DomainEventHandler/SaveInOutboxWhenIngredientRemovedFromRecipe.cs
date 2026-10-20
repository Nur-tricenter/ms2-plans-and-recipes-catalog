using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.Contracts.Service;
using MediatR;
using Catalog.Domain.Event.Recipe;

namespace Catalog.Application.UseCase.DomainEventHandler
{
    public class SaveInOutboxWhenIngredientRemovedFromRecipe : INotificationHandler<IngredientRemovedFromRecipe>
    {
        private readonly IOutboxService<DomainEvent> _outboxService;

        public SaveInOutboxWhenIngredientRemovedFromRecipe(IOutboxService<DomainEvent> outboxService)
        {
            _outboxService = outboxService;
        }

        public async Task Handle(IngredientRemovedFromRecipe notification, CancellationToken cancellationToken = default)
        {
            await _outboxService.AddAsync(new OutboxMessage<DomainEvent>(notification));
        }
    }
}