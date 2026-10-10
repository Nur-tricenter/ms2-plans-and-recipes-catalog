using Shared.Core;

namespace Catalog.Domain.Event.PlanAlimentario
{
    public record TiempoComidaAgregadoAPlan : DomainEvent
    {
        public Guid PlanId { get; }
        public int NumeroDia { get; }
        public Guid TiempoDeComidaId { get; }
        public string TipoTiempoComida { get; }
        public int Orden { get; }

        public TiempoComidaAgregadoAPlan(Guid planId, int numeroDia, Guid tiempoDeComidaId, string tipoTiempoComida, int orden)
            : base(DateTime.UtcNow)
        {
            PlanId = planId;
            NumeroDia = numeroDia;
            TiempoDeComidaId = tiempoDeComidaId;
            TipoTiempoComida = tipoTiempoComida;
            Orden = orden;
        }
    }
}