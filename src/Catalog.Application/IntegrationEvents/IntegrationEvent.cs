namespace Catalog.Application.IntegrationEvents
{
    public abstract record IntegrationEvent
    {
        public DateTime OccuredOn { get; }
        public Guid EventId { get; set; }

        protected IntegrationEvent()
        {
            EventId = Guid.NewGuid();
            OccuredOn = DateTime.UtcNow;
        }
    }
}
