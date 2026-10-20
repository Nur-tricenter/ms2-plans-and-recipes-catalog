using Joseco.DDD.Core.Abstractions;
using Catalog.Infrastructure.Persistence.DomainModel;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.EFCore.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork, IOutboxDatabase<DomainEvent>
    {
        private const int MaxDrainRounds = 10;

        private readonly DomainDbContext _context;
        private readonly IMediator _mediator;

        private bool _isCommitting;

        public UnitOfWork(DomainDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        public DbSet<OutboxMessage<DomainEvent>> GetOutboxMessages() => _context.OutboxMessages;

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_isCommitting)
                return;

            _isCommitting = true;

            try
            {
                await DrainDomainEventsAsync(cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                _context.ChangeTracker.Clear();
                throw;
            }
            finally
            {
                _isCommitting = false;
            }
        }

        private async Task DrainDomainEventsAsync(CancellationToken cancellationToken)
        {
            for (var round = 0; round < MaxDrainRounds; round++)
            {
                var domainEvents = CollectDomainEvents();

                if (domainEvents.Length == 0)
                    return;

                foreach (var domainEvent in domainEvents)
                {
                    await _mediator.Publish(domainEvent, cancellationToken);
                }
            }

            throw new InvalidOperationException(
                $"Domain events kept being raised after {MaxDrainRounds} drain rounds, which indicates an event cycle.");
        }

        private DomainEvent[] CollectDomainEvents()
        {
            var collected = new List<DomainEvent>();

            foreach (var entity in _context.ChangeTracker.Entries<Entity>().Select(entry => entry.Entity))
            {
                if (!entity.DomainEvents.Any())
                    continue;

                collected.AddRange(entity.DomainEvents);
                entity.ClearDomainEvents();
            }

            return collected.ToArray();
        }
    }
}