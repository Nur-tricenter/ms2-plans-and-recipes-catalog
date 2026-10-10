using Catalog.Domain;
using Catalog.Domain.Event.PlanAlimentario;
using Catalog.Domain.Model.PlanesAlimentarios;
using Catalog.Domain.ValueObjects;
using Shared.Core;
using Xunit;

namespace Catalog.Tests.Domain
{
    public class PlanAlimentarioTests
    {
        [Theory]
        [InlineData(TipoDuracion.QUINCENAL, 15)]
        [InlineData(TipoDuracion.MENSUAL, 30)]
        public void Constructor_ValidDuration_CreatesPlanWithExpectedDays(TipoDuracion tipo, int expectedDays)
        {
            // Act
            var plan = new PlanAlimentario(new PlanName("Plan Saludable"), new DuracionPlan(tipo), 3);

            // Assert
            Assert.NotEqual(Guid.Empty, plan.Id);
            Assert.Equal("Plan Saludable", plan.Nombre.Name);
            Assert.Equal(3, plan.ComidasPorDia);
            Assert.Equal(expectedDays, plan.DiasDelPlan.Count());
            var evento = Assert.IsType<PlanAlimentarioCreado>(Assert.Single(plan.DomainEvents));
            Assert.Equal(plan.Id, evento.PlanId);
            Assert.Equal(expectedDays, evento.DiasTotales);
        }

        [Fact]
        public void AgregarTiempoDeComidaADia_ValidDay_AddsMealTime()
        {
            // Arrange
            var plan = new PlanAlimentario(new PlanName("Plan"), new DuracionPlan(TipoDuracion.QUINCENAL), 3);

            // Act
            plan.AgregarTiempoDeComidaADia(1, TipoTiempoComida.Desayuno);

            // Assert
            var dia = plan.DiasDelPlan.First(d => d.NumeroDia == 1);
            var tiempo = Assert.Single(dia.TiemposDeComida);
            Assert.Equal("Desayuno", tiempo.Nombre);
            Assert.Equal(1, tiempo.Orden);
        }

        [Fact]
        public void AgregarTiempoDeComidaADia_InvalidDay_ThrowsException()
        {
            // Arrange
            var plan = new PlanAlimentario(new PlanName("Plan"), new DuracionPlan(TipoDuracion.QUINCENAL), 3);

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() =>
                plan.AgregarTiempoDeComidaADia(16, TipoTiempoComida.Desayuno));
            Assert.Contains("Día 16 no existe en el plan", ex.Details);
        }

        [Fact]
        public void AgregarTiempoDeComidaADia_DuplicateOrder_ThrowsException()
        {
            // Arrange
            var plan = new PlanAlimentario(new PlanName("Plan"), new DuracionPlan(TipoDuracion.QUINCENAL), 3);
            plan.AgregarTiempoDeComidaADia(1, TipoTiempoComida.Desayuno);

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() =>
                plan.AgregarTiempoDeComidaADia(1, TipoTiempoComida.Desayuno));
            Assert.Contains("Ya existe un tiempo de comida con orden 1", ex.Details);
        }

        [Fact]
        public void AsignarRecetaATiempo_WithDayAndExistingTime_AssignsRecipeAndQueuesEvent()
        {
            // Arrange
            var plan = new PlanAlimentario(new PlanName("Plan"), new DuracionPlan(TipoDuracion.QUINCENAL), 3);
            plan.AgregarTiempoDeComidaADia(1, TipoTiempoComida.Desayuno);
            var tiempo = plan.DiasDelPlan.First(d => d.NumeroDia == 1).TiemposDeComida.Single();
            var recetaId = Guid.NewGuid();

            // Act
            plan.AsignarRecetaATiempo(1, tiempo.Id, recetaId, new Racion(2));

            // Assert
            var asignacion = Assert.Single(tiempo.RecetasAsignadas);
            Assert.Equal(recetaId, asignacion.RecetaId);
            Assert.Equal(2, asignacion.Racion.Cantidad);
            // 3 eventos: PlanAlimentarioCreado, TiempoComidaAgregadoAPlan, RecetaAsignadaATiempo
            Assert.Equal(3, plan.DomainEvents.Count);
            var evento = Assert.IsType<RecetaAsignadaATiempo>(plan.DomainEvents.Last());
            Assert.Equal(1, evento.NumeroDia);
            Assert.Equal(recetaId, evento.RecetaId);
        }

        [Fact]
        public void AsignarRecetaATiempo_InvalidDay_ThrowsException()
        {
            // Arrange
            var plan = new PlanAlimentario(new PlanName("Plan"), new DuracionPlan(TipoDuracion.QUINCENAL), 3);

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() =>
                plan.AsignarRecetaATiempo(16, Guid.NewGuid(), Guid.NewGuid(), new Racion(2)));
            Assert.Contains("Día 16 no existe en el plan", ex.Details);
        }

        [Fact]
        public void AsignarRecetaATiempo_TimeNotInDay_ThrowsException()
        {
            // Arrange
            var plan = new PlanAlimentario(new PlanName("Plan"), new DuracionPlan(TipoDuracion.QUINCENAL), 3);

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() =>
                plan.AsignarRecetaATiempo(1, Guid.NewGuid(), Guid.NewGuid(), new Racion(2)));
            Assert.Contains("no existe en el día 1", ex.Details);
        }

        [Fact]
        public void AsignarRecetaATiempo_WithoutDay_FindsTimeInAnyDayAndAssigns()
        {
            // Arrange
            var plan = new PlanAlimentario(new PlanName("Plan"), new DuracionPlan(TipoDuracion.QUINCENAL), 3);
            plan.AgregarTiempoDeComidaADia(3, TipoTiempoComida.Almuerzo);
            var tiempo = plan.DiasDelPlan.First(d => d.NumeroDia == 3).TiemposDeComida.Single();
            var recetaId = Guid.NewGuid();

            // Act
            plan.AsignarRecetaATiempo(tiempo.Id, recetaId, new Racion(1));

            // Assert
            var asignacion = Assert.Single(tiempo.RecetasAsignadas);
            Assert.Equal(recetaId, asignacion.RecetaId);
            var evento = Assert.IsType<RecetaAsignadaATiempo>(plan.DomainEvents.Last());
            Assert.Equal(3, evento.NumeroDia);
        }

        [Fact]
        public void AsignarRecetaATiempo_WithoutDayAndTimeNotFound_ThrowsException()
        {
            // Arrange
            var plan = new PlanAlimentario(new PlanName("Plan"), new DuracionPlan(TipoDuracion.QUINCENAL), 3);

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() =>
                plan.AsignarRecetaATiempo(Guid.NewGuid(), Guid.NewGuid(), new Racion(1)));
            Assert.Contains("no existe en el plan", ex.Details);
        }

        [Fact]
        public void AsignarRecetaATiempo_DuplicateRecipeInTime_ThrowsException()
        {
            // Arrange
            var plan = new PlanAlimentario(new PlanName("Plan"), new DuracionPlan(TipoDuracion.QUINCENAL), 3);
            plan.AgregarTiempoDeComidaADia(1, TipoTiempoComida.Desayuno);
            var tiempo = plan.DiasDelPlan.First(d => d.NumeroDia == 1).TiemposDeComida.Single();
            var recetaId = Guid.NewGuid();
            plan.AsignarRecetaATiempo(tiempo.Id, recetaId, new Racion(1));

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() =>
                plan.AsignarRecetaATiempo(tiempo.Id, recetaId, new Racion(2)));
            Assert.Contains("ya está asignada", ex.Details);
        }

        [Fact]
        public void RemoverRecetaDeTiempo_ExistingAssignment_RemovesAndQueuesEvent()
        {
            // Arrange
            var plan = new PlanAlimentario(new PlanName("Plan"), new DuracionPlan(TipoDuracion.QUINCENAL), 3);
            plan.AgregarTiempoDeComidaADia(1, TipoTiempoComida.Desayuno);
            var tiempo = plan.DiasDelPlan.First(d => d.NumeroDia == 1).TiemposDeComida.Single();
            var recetaId = Guid.NewGuid();
            plan.AsignarRecetaATiempo(tiempo.Id, recetaId, new Racion(1));

            // Act
            plan.RemoverRecetaDeTiempo(tiempo.Id, recetaId);

            // Assert
            Assert.Empty(tiempo.RecetasAsignadas);
            // 4 eventos: PlanAlimentarioCreado, TiempoComidaAgregadoAPlan, RecetaAsignadaATiempo, RecetaRemovidaDeTiempo
            Assert.Equal(4, plan.DomainEvents.Count);
            var evento = Assert.IsType<RecetaRemovidaDeTiempo>(plan.DomainEvents.Last());
            Assert.Equal(recetaId, evento.RecetaId);
        }

        [Fact]
        public void RemoverRecetaDeTiempo_TimeNotFound_ThrowsException()
        {
            // Arrange
            var plan = new PlanAlimentario(new PlanName("Plan"), new DuracionPlan(TipoDuracion.QUINCENAL), 3);

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() =>
                plan.RemoverRecetaDeTiempo(Guid.NewGuid(), Guid.NewGuid()));
            Assert.Contains("no existe en el plan", ex.Details);
        }

        [Fact]
        public void RemoverRecetaDeTiempo_RecipeNotAssigned_ThrowsException()
        {
            // Arrange
            var plan = new PlanAlimentario(new PlanName("Plan"), new DuracionPlan(TipoDuracion.QUINCENAL), 3);
            plan.AgregarTiempoDeComidaADia(1, TipoTiempoComida.Desayuno);
            var tiempo = plan.DiasDelPlan.First(d => d.NumeroDia == 1).TiemposDeComida.Single();

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() =>
                plan.RemoverRecetaDeTiempo(tiempo.Id, Guid.NewGuid()));
            Assert.Contains("no está asignada", ex.Details);
        }
    }
}