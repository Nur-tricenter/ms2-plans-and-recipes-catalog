using Shared.Core;

namespace Catalog.Domain.Event.Receta
{
    public record RecetaActualizada : DomainEvent
    {
        public Guid RecetaId { get; }
        public string Nombre { get; }
        public string Instrucciones { get; }

        public RecetaActualizada(Guid recetaId, string nombre, string instrucciones)
            : base(DateTime.UtcNow)
        {
            RecetaId = recetaId;
            Nombre = nombre;
            Instrucciones = instrucciones;
        }
    }
}