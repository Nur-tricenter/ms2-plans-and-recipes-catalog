using MediatR;

namespace Shared.Core
{
    public abstract record DomainEvent : INotification
    {
        public DateTime OccuredOn { get; }
        public Guid Id { get; }
        public bool Consumed { get; set; }

        protected DomainEvent(DateTime occuredOn)
        {
            OccuredOn = occuredOn;
            Id = Guid.NewGuid();
            Consumed = false;
        }

        public void MarkAsConsumed()
        {
            Consumed = true;
        }
    }
}