using System.Diagnostics;
using Joseco.Communication.External.Contracts.Message;
using Joseco.Communication.External.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Observability;

/// <summary>
/// Re-attaches the publisher's distributed trace to every inbound integration message, so one
/// trace spans both services instead of two unrelated ones.
/// </summary>
/// <typeparam name="T">The integration contract being consumed.</typeparam>
/// <typeparam name="TInner">
/// The decorated consumer's CONCRETE type, for the same reason
/// <c>IdempotentConsumer&lt;T, TInner&gt;</c> takes one: an interface-typed inner dependency
/// collides with the registration that names this decorator.
/// </typeparam>
/// <remarks>
/// <para>
/// The receiving twin of <c>OutboxTracingService</c>. That decorator stamps the outgoing outbox
/// row so the correlation id survives the hop; this one reads it back so the consumer's span joins
/// the producer's.
/// </para>
/// <para>
/// <b>Why it is not automatic.</b> RabbitMQ carries no trace context: the AMQP headers are not a
/// W3C <c>traceparent</c>, and the publisher had to put the id in a JSON property for this to be
/// possible at all. Without reading that property back, the correlation id arrives on the message
/// but there is no span parented to it — the collector shows two traces where there should be one,
/// and nothing warns about it.
/// </para>
/// <para>
/// <b>The relationship to the inbox.</b> The two decorators answer different questions and neither
/// replaces the other. The inbox asks "has this message been handled?" and makes the claim atomic.
/// This asks "which trace does this work belong to?" and nests the span. A message can be a
/// duplicate and still need its trace, and a trace can be absent while the message is genuinely
/// new.
/// </para>
/// </remarks>
public sealed class TracingConsumer<T, TInner> : IIntegrationMessageConsumer<T>
    where T : IntegrationMessage
    where TInner : class, IIntegrationMessageConsumer<T>
{
    /// <summary>
    /// The span name a consumer produces. Fixed rather than derived from the message type so the
    /// consumer side of every service aggregates under one name in the collector, instead of
    /// scattering one span per contract type.
    /// </summary>
    public const string SpanName = "rabbitmq.consume";

    private readonly TInner _inner;
    private readonly ILogger<TracingConsumer<T, TInner>> _logger;

    public TracingConsumer(
        TInner inner,
        ILogger<TracingConsumer<T, TInner>> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task HandleAsync(T message, CancellationToken cancellationToken = default)
    {
        // A correlation id is not necessarily a trace id. The publisher writes its TraceId there,
        // but a message that arrived without one — from a service that does not stamp, or from a
        // replayed exchange — has to still produce a span rather than be dropped from tracing.
        var parent = TryParseTraceParent(message.CorrelationId);

        using var activity = parent is null
            ? new Activity(SpanName)
            : new Activity(SpanName).SetParentId(parent);

        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        activity.SetTag("messaging.system", "rabbitmq");
        activity.SetTag("messaging.operation.name", "consume");
        activity.SetTag("messaging.destination.name", typeof(T).Name);
        activity.SetTag("messaging.message.id", message.Id);

        if (message.CorrelationId is not null)
        {
            activity.SetTag("messaging.correlation_id", message.CorrelationId);
        }

        if (message.Source is not null)
        {
            activity.SetTag("messaging.source", message.Source);
        }

        if (activity.Id is not null)
        {
            _logger.LogDebug(
                "Consuming {MessageType} {MessageId} in span {SpanId} (parent {TraceParent}).",
                typeof(T).Name, message.Id, activity.Id, parent ?? "<none>");
        }

        try
        {
            await _inner.HandleAsync(message, cancellationToken);
        }
        catch (Exception exception)
        {
            // Recorded on the span before it rethrows: an exception that only reaches the log is
            // invisible to the trace, and the trace is what an operator is looking at when a
            // message fails in a service they are not running.
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity.AddEvent(new ActivityEvent(
                "exception",
                tags: new ActivityTagsCollection
                {
                    { "exception.type", exception.GetType().FullName },
                    { "exception.message", exception.Message },
                    { "exception.stacktrace", exception.ToString() }
                }));

            throw;
        }
        finally
        {
            activity.Stop();
        }
    }

    /// <summary>
    /// Reads the publisher's trace id out of the correlation id, or returns null when it is not one.
    /// </summary>
    /// <remarks>
    /// Tolerant by design. The field is a plain string on a wire contract, so it may be a bare
    /// 32-hex trace id, a full <c>traceparent</c>, or something else entirely if a producer did not
    /// follow the convention. A malformed value yields null and the span starts fresh, which is
    /// the correct outcome — a wrong parent id would attach this work to an unrelated trace, and
    /// that is worse than having no parent at all.
    /// </remarks>
    private static string? TryParseTraceParent(string? correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            return null;
        }

        var candidate = correlationId.Trim();

        // Already a full traceparent: pass it through untouched.
        if (candidate.StartsWith("00-", StringComparison.Ordinal))
        {
            return candidate;
        }

        // A bare 32-hex trace id is a valid traceparent once wrapped.
        if (candidate.Length == 32 && candidate.All(Uri.IsHexDigit))
        {
            return $"00-{candidate}-{candidate[..16]}-01";
        }

        return null;
    }
}