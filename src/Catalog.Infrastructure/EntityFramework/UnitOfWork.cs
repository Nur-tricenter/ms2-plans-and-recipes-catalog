using Joseco.DDD.Core.Abstractions;
using Catalog.Infrastructure.EntityFramework.Context;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure.EntityFramework
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly WriteDbContext _context;
        private readonly IMediator _mediator;

        public UnitOfWork(WriteDbContext context, IMediator mediator)
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
