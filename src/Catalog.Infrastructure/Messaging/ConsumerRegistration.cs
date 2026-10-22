using Nur.Tricenter.Integration.Messages.Identity;
using Catalog.Infrastructure.Messaging.Consumers;
using Catalog.Infrastructure.Observability;
using Joseco.Communication.External.Contracts.Services;
using Joseco.Communication.External.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure.Messaging;

/// <summary>
/// Registers the cross-service consumers and the RabbitMQ subscription that feeds them.
/// </summary>
public static class ConsumerRegistration
{
    /// <summary>
    /// The queue the identity service's <c>user-published</c> exchange feeds.
    /// </summary>
    /// <remarks>
    /// Must match the <c>ms2.user-published</c> queue declared in
    /// <c>orchestrator/topology/rabbitmq-definitions.json</c>. They are one contract expressed in
    /// two places ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â a typo here silently creates a second queue that receives the same messages,
    /// so the names match the platform convention <c>{consumer}.{exchange}</c> exactly.
    /// </remarks>
    public const string UserPublishedQueue = "ms2.user-published";

    /// <summary>
    /// Subscribes to the identity service's user events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is what makes the catalog a consumer rather than only a producer, and it is the first
    /// time the platform has exercised the receiving half of the pattern.
    /// </para>
    /// <para>
    /// <b>Only the WorkerService calls this.</b> The WebApi must not: the consumer needs the
    /// broker at startup, which would make RabbitMQ a hard boot dependency of the HTTP process,
    /// and it needs to be the single process that owns this subscription so a message is not
    /// projected twice.
    /// </para>
    /// <para>
    /// <b>The pipeline is three layers, and the order is the design.</b>
    /// </para>
    /// <code>
    ///   RabbitMQ delivery
    ///     -&gt; TracingConsumer          re-attaches the publisher's span
    ///          -&gt; IdempotentConsumer     claims the message in the inbox
    ///               -&gt; UserPublishedConsumer   projects it
    /// </code>
    /// <para>
    /// Tracing is OUTSIDE the inbox on purpose. A redelivery that the inbox skips must still appear
    /// in the trace ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â it is a real delivery, and a redelivery storm is exactly what an operator is
    /// looking for. If the inbox swallowed it first, the duplicate would leave no trace at all.
    /// </para>
    /// <para>
    /// The inbox sits outside the projection because its claim has to be the first thing that
    /// touches the database.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddUserPublishedConsumer(this IServiceCollection services)
    {
        // The concrete inner consumer. NEVER registered against IIntegrationMessageConsumer:
        // both decorators take their inner handler through that interface, so an interface
        // registration here makes each decorator depend on itself and DI reports a circular
        // dependency. Concrete types only ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â the decorators construct their own chain.
        services.AddTransient<UserPublishedConsumer>();

        // INNERMOST FIRST. Each decorator takes the next one's CONCRETE type, so the last one
        // registered is the outermost wrapper.
        //
        //   IdempotentConsumer<UserPublished, UserPublishedConsumer>  wraps the projection
        //   TracingConsumer<UserPublished, IdempotentConsumer<...>>  wraps the inbox
        //
        // Neither decorator takes IIntegrationMessageConsumer<T> as its inner dependency: the
        // registration that AddRabbitMqConsumer makes is for the outermost decorator itself, so an
        // interface-typed inner would make the chain self-referential and DI reports a circular
        // dependency at startup.
        services.AddTransient<IdempotentConsumer<UserPublished, UserPublishedConsumer>>();
        services.AddTransient<TracingConsumer<UserPublished, IdempotentConsumer<UserPublished, UserPublishedConsumer>>>();

        // The handler generic argument must be the CONCRETE outer decorator. The extension
        // registers THandler as the implementation of IIntegrationMessageConsumer<TMessage>.
        services.AddRabbitMqConsumer<
            UserPublished,
            TracingConsumer<UserPublished, IdempotentConsumer<UserPublished, UserPublishedConsumer>>>(
            UserPublishedQueue);

        return services;
    }
}