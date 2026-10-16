using MediatR;

namespace Shared.Core
{
    public class ConfirmedDomainEvent<T> : INotification where T : DomainEvent
    {
        public T DomainEvent { get; }

        public ConfirmedDomainEvent(T domainEvent)
        {
            DomainEvent = domainEvent;
        }
    }
}