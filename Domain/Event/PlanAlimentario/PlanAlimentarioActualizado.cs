using Shared.Core;

namespace Catalog.Domain.Event.PlanAlimentario
{
    public record PlanAlimentarioActualizado : DomainEvent
    {
        public Guid PlanId { get; }
        public string Nombre { get; }
        public string TipoDuracion { get; }
        public int DiasTotales { get; }
        public int ComidasPorDia { get; }

        public PlanAlimentarioActualizado(Guid planId, string nombre, string tipoDuracion, int diasTotales, int comidasPorDia)
            : base(DateTime.UtcNow)
        {
            PlanId = planId;
            Nombre = nombre;
            TipoDuracion = tipoDuracion;
            DiasTotales = diasTotales;
            ComidasPorDia = comidasPorDia;
        }
    }
}