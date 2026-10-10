namespace Shared.Core
{
    public abstract class Entity<TId> : IAuditableEntity
    {
        public TId Id { get; protected set; } = default!;

        private readonly ICollection<DomainEvent> _domainEvents;

        public ICollection<DomainEvent> DomainEvents { get { return _domainEvents; } }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsDeleted { get; set; }

        protected Entity()
        {
            _domainEvents = new List<DomainEvent>();
        }

        public void AddDomainEvent(DomainEvent evento)
        {
            _domainEvents.Add(evento);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }

        protected void CheckRule(IBussinessRule rule)
        {
            if (rule is null)
            {
                throw new ArgumentException("Rule cannot be null");
            }
            if (!rule.IsValid())
            {
                throw new BussinessRuleValidationException(rule);
            }
        }
    }
}