using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.Contracts.Service;

namespace Catalog.Infrastructure.Observability;

/// <summary>
/// Stamps the ambient distributed trace onto every outbox message before it is staged, so the
/// correlation identifier survives the jump from the WebApi process to the WorkerService process.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists.</b> <see cref="Joseco.Outbox.Contracts.Model.OutboxMessage{T}"/>'s single
/// argument constructor leaves <c>CorrelationId</c>, <c>TraceId</c> and <c>SpanId</c> null, and it
/// does not read <see cref="System.Diagnostics.Activity.Current"/> either. Measured against a real
/// database before this decorator existed: 654 of 654 rows in <c>outbox.outboxMessage</c> had
/// <c>correlationId</c> NULL and <c>traceId</c> NULL, so every contract published to RabbitMQ
/// carried an empty correlation id.
/// </para>
/// <para>
/// <b>Why a decorator and not the 11 call sites.</b> The alternative is changing
/// <c>new OutboxMessage&lt;DomainEvent&gt;(notification)</c> in every <c>SaveInOutboxWhen*</c>
/// handler, which spreads one piece of cross-cutting knowledge across eleven files that otherwise
/// know nothing about tracing. A decorator keeps the write in exactly one place, at the boundary
/// where the row is produced, and a handler added later gets correlation for free.
/// </para>
/// <para>
/// <b>Why the values come from <see cref="System.Diagnostics.Activity.Current"/>.</b> Because the
/// correlation identifier IS the trace identifier: one identifier then ties the HTTP request, the
/// database row, the worker's log line and the published contract together, with no convention of
/// our own. The domain is deliberately not involved. Aggregates, domain events and handlers must
/// not know that tracing exists, so the read happens here, in the infrastructure layer, at the one
/// place that writes the row.
/// </para>
/// <para>
/// <b>Known limit, not a bug.</b> <c>IExternalPublisher.PublishAsync</c> accepts no headers, so
/// trace context can only travel as a JSON property inside <c>IntegrationMessage.CorrelationId</c>.
/// A consumer cannot inherit a W3C <c>traceparent</c> automatically and must read that property.
/// Changing that needs an overload in <c>Joseco.Communication.External.Contracts</c>.
/// </para>
/// </remarks>
/// <remarks>
/// Implements <see cref="IOutboxService{TEvent}"/> only. Reading rows back is
/// <see cref="IOutboxRepository{TEvent}"/>, which is a separate interface resolved on the same
/// <c>EfCoreOutboxService</c> instance and must not be stamped: a row the worker reads was stamped
/// when it was written, and re-stamping it here would overwrite the originating request's trace with
/// the worker's poll activity.
/// </remarks>
/// <typeparam name="TEvent">The domain event type carried by the outbox.</typeparam>
internal sealed class OutboxTracingService<TEvent> : IOutboxService<TEvent>
{
    private readonly IOutboxService<TEvent> _inner;

    public OutboxTracingService(IOutboxService<TEvent> inner)
    {
        _inner = inner;
    }

    public async Task AddAsync(OutboxMessage<TEvent> message)
    {
        Stamp(message);
        await _inner.AddAsync(message);
    }

    /// <summary>
    /// Copies the ambient trace onto the message.
    /// </summary>
    /// <remarks>
    /// The properties are plain mutable properties with public setters, so this works whether or not
    /// the caller already filled them; that is what makes the decorator able to correct a message
    /// rather than only decorate a blank one.
    ///
    /// A missing <see cref="System.Diagnostics.Activity"/> is normal, not exceptional: the worker
    /// reads rows it did not write, and a background poll has no incoming HTTP activity. Leaving the
    /// values null in that case is correct and keeps the column honest instead of inventing a
    /// correlation id that relates to nothing.
    /// </remarks>
    private static void Stamp(OutboxMessage<TEvent> message)
    {
        var activity = System.Diagnostics.Activity.Current;
        if (activity is null)
        {
            return;
        }

        message.TraceId = activity.TraceId.ToString();
        message.SpanId = activity.SpanId.ToString();

        // TraceId doubles as the correlation id: one identifier across request, row and message.
        // OpenTelemetry keeps it null rather than substituting a fresh id, because a correlation id
        // nobody can join on is worse than an absent one.
        message.CorrelationId ??= activity.TraceId.ToString();
    }
}