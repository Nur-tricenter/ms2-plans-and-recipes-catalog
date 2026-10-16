using Joseco.DDD.Core.Abstractions;
using Catalog.Domain.Primitives;

namespace Catalog.Domain.ValueObjects
{
    public record PlanDuration : ValueObject
    {
        public DurationType Type { get; }

        public PlanDuration(DurationType type)
        {
            Type = type;
            if (!IsValid())
                throw new BussinessRuleValidationException("PlanDuration must be FIFTEEN_DAYS or THIRTY_DAYS");
        }

        public int Days() => Type switch
        {
            DurationType.FIFTEEN_DAYS => 15,
            DurationType.THIRTY_DAYS => 30,
            _ => throw new BussinessRuleValidationException("Invalid duration type")
        };

        public bool IsValid() => Type == DurationType.FIFTEEN_DAYS || Type == DurationType.THIRTY_DAYS;
    }
}