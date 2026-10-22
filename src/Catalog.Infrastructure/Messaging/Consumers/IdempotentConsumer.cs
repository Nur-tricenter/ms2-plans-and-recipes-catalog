using Catalog.Infrastructure.Persistence.StoredModel;
using Catalog.Infrastructure.Persistence.StoredModel.Entities;
using Joseco.Communication.External.Contracts.Message;
using Joseco.Communication.External.Contracts.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Messaging.Consumers;

/// <summary>
/// Runs the decorated consumer at most once per message id, using an inbox table as the atomic
/// claim.
/// </summary>
/// <typeparam name="T">The integration contract being consumed.</typeparam>
/// <remarks>
/// <para>
/// <b>The race this exists to close.</b> RabbitMQ delivers at least once. A handler that does
/// "look, then act" has a window: delivery A checks the inbox, finds nothing, and starts work;
/// delivery B — the redelivery after a crash — checks the same inbox, also finds nothing, and does
/// the same work. Both succeed. The projection ends up with one row only because its write was an
/// idempotent upsert, which is luck rather than design.
/// </para>
/// <para>
/// <b>How the claim is atomic.</b> The inbox row is INSERTED FIRST, in its own transaction, before
/// the wrapped consumer runs. Two concurrent deliveries both attempt the insert; the database
/// serialises them and the loser fails on the primary key. That failure IS the duplicate signal —
/// not a read that could have been stale, not a flag that could have been written twice.
/// </para>
/// <para>
/// <b>Why the claim commits before the work.</b> If both were one transaction, a crash during
/// the handler would roll the claim back and the redelivery would retry — which is what you want.
/// That is why <see cref="HasProcessedAsync"/> is a separate read rather than part of the claim.
/// The trade is deliberate: a crash between claiming and completing loses the message's effect
/// (the row exists, so the redelivery is skipped). For a projection, losing that is recoverable by
/// replaying the exchange; for a side effect like a charge it would not be. A consumer with
/// non-idempotent effects needs the claim and the work in one transaction, which means this
/// decorator is the wrong tool for it.
/// </para>
/// </remarks>
/// <typeparam name="TInner">
/// The decorated consumer's CONCRETE type.
/// </typeparam>
/// <remarks>
/// <b>Why the concrete type and not the interface.</b> <c>AddRabbitMqConsumer&lt;TMessage,
/// THandler&gt;</c> registers <c>THandler</c> as the implementation of
/// <c>IIntegrationMessageConsumer&lt;TMessage&gt;</c>. A decorator that took
/// <c>IIntegrationMessageConsumer&lt;T&gt;</c> as its inner dependency therefore depends on the
/// registration that names the decorator itself, and DI reports a circular dependency at startup.
/// Taking <typeparamref name="TInner"/> makes each layer construct the one below it, with no
/// interface in the chain and nothing to resolve ambiguously.
/// </remarks>
public sealed class IdempotentConsumer<T, TInner> : IIntegrationMessageConsumer<T>
    where T : IntegrationMessage
    where TInner : class, IIntegrationMessageConsumer<T>
{
    private readonly TInner _inner;
    private readonly StoredDbContext _context;
    private readonly ILogger<IdempotentConsumer<T, TInner>> _logger;

    public IdempotentConsumer(
        TInner inner,
        StoredDbContext context,
        ILogger<IdempotentConsumer<T, TInner>> logger)
    {
        _inner = inner;
        _context = context;
        _logger = logger;
    }

    public async Task HandleAsync(T message, CancellationToken cancellationToken = default)
    {
        if (await HasProcessedAsync(message.Id, cancellationToken))
        {
            _logger.LogInformation(
                "Skipping {MessageType} {MessageId}: already in the inbox.",
                typeof(T).Name, message.Id);
            return;
        }

        // Claim first, in its own SaveChanges. This is the atomic step: the primary key on
        // MessageId is what turns two concurrent deliveries into one winner and one duplicate.
        _context.InboxMessages.Add(new InboxMessage(
            message.Id,
            typeof(T).Name,
            message.CorrelationId,
            message.CreatedAt));

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Lost the race. Someone else claimed it, so this delivery is a duplicate and the
            // correct action is to do nothing at all — not to retry, and not to run the handler.
            _logger.LogInformation(
                "{MessageType} {MessageId} was claimed by a concurrent delivery; skipping.",
                typeof(T).Name, message.Id);
            return;
        }

        _logger.LogInformation(
            "Handling {MessageType} {MessageId} from {Source}.",
            typeof(T).Name, message.Id, message.Source);

        await _inner.HandleAsync(message, cancellationToken);
    }

    private Task<bool> HasProcessedAsync(Guid messageId, CancellationToken cancellationToken) =>
        _context.InboxMessages
            .AsNoTracking()
            .AnyAsync(m => m.MessageId == messageId, cancellationToken);
}