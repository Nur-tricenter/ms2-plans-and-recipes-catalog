using System.Reflection;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Xunit;

namespace Catalog.Tests.Application.DomainEventHandler;

/// <summary>
/// The outbox chain has three links and no framework enforces them: a domain event is raised,
/// a <c>SaveInOutbox*</c> handler stages it in the outbox table, and a <c>Project*</c> handler
/// applies it to the read model. MediatR discovers handlers by convention, so a missing link is
/// not a compile error and not a DI error — the event simply never reaches the outbox and the
/// projection never runs.
/// </summary>
/// <remarks>
/// This test exists because that failure already happened once: <c>FoodUpdated</c> had a
/// projection handler but no outbox handler, so the read-model drift fix was inert in the real
/// end-to-end flow while the projection's own unit test stayed green. These assertions are about
/// the shape of the assembly, not about any one handler's behaviour.
/// </remarks>
public class OutboxChainCoverageTests
{
    private static readonly Assembly DomainAssembly =
        typeof(Catalog.Domain.Model.Foods.Food).Assembly;

    private static readonly Assembly ApplicationAssembly =
        typeof(Catalog.Application.UseCase.DomainEventHandler.SaveInOutboxWhenFoodCreated).Assembly;

    private static readonly Assembly InfrastructureAssembly =
        typeof(Catalog.Infrastructure.Persistence.StoredModel.StoredDbContext).Assembly;

    /// <summary>Every concrete <c>DomainEvent</c> the service declares.</summary>
    private static IReadOnlyList<Type> DomainEvents() =>
        DomainAssembly.GetTypes()
            .Where(type => !type.IsAbstract &&
                           type.IsSubclassOf(typeof(DomainEvent)) &&
                           type.Namespace!.StartsWith("Catalog.Domain.Event", StringComparison.Ordinal))
            .ToList();

    [Fact]
    public void EveryDomainEventHasASaveInOutboxHandler()
    {
        // Arrange
        var events = DomainEvents();

        // Assert - an event with no outbox handler is raised, drained by the unit of work, and
        // then dropped: nothing ever reads it.
        Assert.NotEmpty(events);

        var unstaged = events
            .Where(domainEvent => !HasHandlerNamed(ApplicationAssembly, "SaveInOutboxWhen", domainEvent))
            .Select(domainEvent => domainEvent.Name)
            .ToList();

        Assert.True(
            unstaged.Count == 0,
            $"Domain events with no SaveInOutbox handler: {string.Join(", ", unstaged)}. " +
            "Without one they never reach the outbox table and nothing downstream sees them.");
    }

    [Fact]
    public void EveryDomainEventHasAProjectionHandler()
    {
        // Arrange
        var events = DomainEvents();

        // Assert - an event with no projection leaves the read model stale for that fact.
        Assert.NotEmpty(events);

        var unprojected = events
            .Where(domainEvent => !HasHandlerForEvent(InfrastructureAssembly, domainEvent))
            .Select(domainEvent => domainEvent.Name)
            .ToList();

        Assert.True(
            unprojected.Count == 0,
            $"Domain events with no projection handler: {string.Join(", ", unprojected)}. " +
            "The read model would keep serving the previous value for those changes.");
    }

    private static bool HasHandlerNamed(Assembly assembly, string prefix, Type domainEvent)
    {
        var expected = $"{prefix}{domainEvent.Name}";

        return assembly.GetTypes()
            .Any(type => type.Name == expected &&
                         // INotificationHandler<T> is closed over the concrete domain event, not
                         // over the DomainEvent base, so SaveInOutboxWhenFoodCreated never
                         // satisfies SaveInOutboxWhenFoodUpdated.
                         type.GetInterfaces().Any(i =>
                             i.IsGenericType &&
                             i.GetGenericTypeDefinition() == typeof(INotificationHandler<>) &&
                             i.GetGenericArguments()[0] == domainEvent));
    }

    private static bool HasHandlerForEvent(Assembly assembly, Type domainEvent)
    {
        return assembly.GetTypes()
            .Any(type => type.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(INotificationHandler<>) &&
                UnwrapsTo(i.GetGenericArguments()[0], domainEvent)));
    }

    /// <summary>
    /// A projection is registered as <c>INotificationHandler&lt;OutboxMessage&lt;TEvent&gt;&gt;</c>
    /// — the worker dispatches the staged outbox row, not the bare event — so the inner generic
    /// argument has to be unwrapped before comparing it with the domain event.
    /// </summary>
    private static bool UnwrapsTo(Type handlerArgument, Type domainEvent)
    {
        if (handlerArgument == domainEvent)
        {
            return true;
        }

        return handlerArgument.IsGenericType
               && handlerArgument.GetGenericTypeDefinition() == typeof(OutboxMessage<>)
               && handlerArgument.GetGenericArguments()[0] == domainEvent;
    }
}