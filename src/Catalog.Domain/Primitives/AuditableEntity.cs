using Joseco.DDD.Core.Abstractions;

namespace Catalog.Domain.Primitives
{
    public abstract class AuditableAggregateRoot : AggregateRoot, IAuditableEntity
    {
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsDeleted { get; set; }
    }

    public abstract class AuditableEntity : Entity, IAuditableEntity
    {
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
