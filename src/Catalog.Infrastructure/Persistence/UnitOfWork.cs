using Joseco.DDD.Core.Abstractions;
using Catalog.Infrastructure.Persistence.DomainModel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DomainDbContext _context;
        private readonly IMediator _mediator;

        public UnitOfWork(DomainDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            var domainEvents = _context.ChangeTracker.Entries<Entity>()
                .Select(x => x.Entity.DomainEvents)
                .SelectMany(x => x)
                .ToArray();

            foreach (var @event in domainEvents)
            {
                await _mediator.Publish(@event, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
