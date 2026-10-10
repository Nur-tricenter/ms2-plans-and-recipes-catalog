using Shared.Core;
using Catalog.Domain.ValueObjects;
using Catalog.Domain.Event.PlanAlimentario;

namespace Catalog.Domain.Model.PlanesAlimentarios
{
    public class PlanAlimentario : AggregateRoot<Guid>
    {
        public PlanName Nombre { get; private set; }
        public DuracionPlan Duracion { get; private set; }
        public int ComidasPorDia { get; private set; }
        private readonly ICollection<DiaDelPlan> _diasDelPlan;

        public IEnumerable<DiaDelPlan> DiasDelPlan => _diasDelPlan;

        private PlanAlimentario() { Nombre = null!; Duracion = null!; _diasDelPlan = null!; }

        public PlanAlimentario(PlanName nombre, DuracionPlan duracion, int comidasPorDia)
        {
            if (!duracion.EsValida())
                throw new BussinessRuleValidationException("Invalid plan duration");

            Id = Guid.NewGuid();
            Nombre = nombre;
            Duracion = duracion;
            ComidasPorDia = comidasPorDia;
            _diasDelPlan = new List<DiaDelPlan>();

            for (int i = 1; i <= duracion.Dias(); i++)
            {
                _diasDelPlan.Add(new DiaDelPlan(i));
            }

            AddDomainEvent(new PlanAlimentarioCreado(Id, Nombre, Duracion.Tipo.ToString(), Duracion.Dias(), comidasPorDia));
        }

        public void Actualizar(PlanName nombre, DuracionPlan duracion, int comidasPorDia)
        {
            if (!duracion.EsValida())
                throw new BussinessRuleValidationException("Invalid plan duration");

            Nombre = nombre;
            Duracion = duracion;
            ComidasPorDia = comidasPorDia;

            AddDomainEvent(new PlanAlimentarioActualizado(Id, Nombre, Duracion.Tipo.ToString(), Duracion.Dias(), comidasPorDia));
        }

        public void AgregarTiempoDeComidaADia(int numDia, TipoTiempoComida tipo)
        {
            var dia = _diasDelPlan.FirstOrDefault(d => d.NumeroDia == numDia);
            if (dia == null)
                throw new BussinessRuleValidationException($"Día {numDia} no existe en el plan");

            dia.AgregarTiempoDeComida(tipo);

            var tiempoAgregado = dia.TiemposDeComida.First(t => t.Orden == (int)tipo);
            AddDomainEvent(new TiempoComidaAgregadoAPlan(Id, numDia, tiempoAgregado.Id, tipo.ToDisplayName(), tiempoAgregado.Orden));
        }

        public void AsignarRecetaATiempo(int numDia, Guid tId, Guid recetaId, Racion racion)
        {
            var dia = _diasDelPlan.FirstOrDefault(d => d.NumeroDia == numDia);
            if (dia == null)
                throw new BussinessRuleValidationException($"Día {numDia} no existe en el plan");

            var tiempo = dia.BuscarTiempoDeComida(tId);
            if (tiempo == null)
                throw new BussinessRuleValidationException($"Tiempo de comida {tId} no existe en el día {numDia}");

            tiempo.AsignarReceta(recetaId, racion);

            AddDomainEvent(new RecetaAsignadaATiempo(Id, numDia, tId, recetaId, racion.Cantidad));
        }

        public void AsignarRecetaATiempo(Guid tId, Guid recetaId, Racion racion)
        {
            foreach (var dia in _diasDelPlan)
            {
                var tiempo = dia.BuscarTiempoDeComida(tId);
                if (tiempo != null)
                {
                    tiempo.AsignarReceta(recetaId, racion);
                    AddDomainEvent(new RecetaAsignadaATiempo(Id, dia.NumeroDia, tId, recetaId, racion.Cantidad));
                    return;
                }
            }

            throw new BussinessRuleValidationException($"Tiempo de comida {tId} no existe en el plan");
        }

        public void RemoverRecetaDeTiempo(Guid tId, Guid recetaId)
        {
            foreach (var dia in _diasDelPlan)
            {
                var tiempo = dia.BuscarTiempoDeComida(tId);
                if (tiempo != null)
                {
                    tiempo.RemoverReceta(recetaId);
                    AddDomainEvent(new RecetaRemovidaDeTiempo(Id, dia.NumeroDia, tId, recetaId));
                    return;
                }
            }

            throw new BussinessRuleValidationException($"Tiempo de comida {tId} no existe en el plan");
        }
    }
}
