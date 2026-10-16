using Shared.Core;
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

        public async Task Commit()
        {
            var domainEvents = _context.ChangeTracker.Entries<Entity<object>>()
                .Select(x => x.Entity.DomainEvents)
                .SelectMany(x => x)
                .ToArray();

            foreach (var @event in domainEvents)
            {
                await _mediator.Publish(@event);
            }

            // 🔍 DEBUG TEMPORAL — borrar después
            _context.ChangeTracker.DetectChanges();
            foreach (var entry in _context.ChangeTracker.Entries())
            {
                Console.WriteLine($"[TRACKED] {entry.Entity.GetType().Name} | State={entry.State} | PK={string.Join(",", entry.Properties.Where(p => p.Metadata.IsPrimaryKey()).Select(p => p.CurrentValue))}");
            }

            await _context.SaveChangesAsync();
        }
    }
}
