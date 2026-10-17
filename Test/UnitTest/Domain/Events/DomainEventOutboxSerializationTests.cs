using System.Reflection;
using Catalog.Domain.Event.Food;
using Catalog.Domain.Event.MealPlan;
using Catalog.Domain.Event.Recipe;
using Catalog.Domain.ValueObjects;
using Joseco.DDD.Core.Abstractions;
using Newtonsoft.Json;
using Xunit;

namespace Catalog.Tests.Domain.Events
{
    /// <summary>
    /// Guards the round trip the outbox performs for EVERY domain event.
    /// <para>
    /// <c>Joseco.Outbox.EFCore</c> persists <c>OutboxMessage&lt;DomainEvent&gt;.Content</c> through
    /// an internal <c>OutboxMessageConfig&lt;E&gt;</c> whose Newtonsoft
    /// <see cref="JsonSerializerSettings"/> are hardcoded to
    /// <see cref="TypeNameHandling.All"/> and cannot be changed from this solution. When the
    /// WorkerService's <c>OutboxProcessor</c> replays a row it deserializes <c>Content</c> with
    /// exactly those settings and hands the result to the projection handlers, so these tests
    /// reproduce the library's behaviour instead of an idealised one.
    /// </para>
    /// <para>
    /// The regression they exist for: a domain event that offered Newtonsoft a PUBLIC
    /// PARAMETERLESS CONSTRUCTOR together with get-only payload properties deserialized into an
    /// instance whose payload was never written. Newtonsoft picked the parameterless constructor,
    /// found every payload property read-only, skipped each one, and returned an event holding
    /// <c>Guid.Empty</c>, <c>string.Empty</c> and <c>null</c> — with no error. The null
    /// <see cref="NutritionalInfo"/> then threw <see cref="NullReferenceException"/> inside
    /// <c>ProjectFoodModelWhenFoodCreated</c> before any INSERT was issued, leaving
    /// <c>read.Food</c> empty. The other events did not throw, they simply projected empty rows.
    /// </para>
    /// </summary>
    public class DomainEventOutboxSerializationTests
    {
        /// <summary>
        /// The settings <c>Joseco.Outbox.EFCore</c> hardcodes in its internal
        /// <c>OutboxMessageConfig&lt;E&gt;</c>. Mirrored here on purpose.
        /// </summary>
        private static readonly JsonSerializerSettings OutboxSerializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.All
        };

        public static TheoryData<DomainEvent> EveryDomainEvent() => new()
        {
            new FoodCreated(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Manzana",
                "FRUTAS",
                new NutritionalInfo(100m, 52m, 0.3m, 14m, 0.2m),
                UnitOfMeasure.Gram),
            new FoodNutritionalInfoUpdated(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                new NutritionalInfo(100m, 52m, 0.3m, 14m, 0.2m),
                new NutritionalInfo(250m, 130m, 0.8m, 35m, 0.5m)),
            new MealPlanCreated(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "Plan Saludable",
                "FIFTEEN_DAYS",
                15,
                3),
            new MealPlanUpdated(
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                "Plan Balanceado",
                "THIRTY_DAYS",
                30,
                4),
            new MealTimeAddedToPlan(
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                1,
                Guid.Parse("55555555-5555-5555-5555-5555555555aa"),
                "Breakfast",
                1),
            new RecipeAssignedToMealTime(
                Guid.Parse("66666666-6666-6666-6666-666666666666"),
                1,
                Guid.Parse("66666666-6666-6666-6666-6666666666aa"),
                Guid.Parse("66666666-6666-6666-6666-6666666666bb"),
                2),
            new RecipeRemovedFromMealTime(
                Guid.Parse("77777777-7777-7777-7777-777777777777"),
                1,
                Guid.Parse("77777777-7777-7777-7777-7777777777aa"),
                Guid.Parse("77777777-7777-7777-7777-7777777777bb")),
            new RecipeCreated(
                Guid.Parse("88888888-8888-8888-8888-888888888888"),
                "Ensalada de frutas",
                "Picar y mezclar."),
            new RecipeUpdated(
                Guid.Parse("99999999-9999-9999-9999-999999999999"),
                "Ensalada de frutas con miel",
                "Picar, mezclar y endulzar."),
            new IngredientAddedToRecipe(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                2.5m),
            new IngredientRemovedFromRecipe(
                Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                1.5m)
        };

        [Theory]
        [MemberData(nameof(EveryDomainEvent))]
        public void RoundTrip_WithOutboxSerializerSettings_PreservesEveryPayloadMember(DomainEvent domainEvent)
        {
            // Arrange
            var json = JsonConvert.SerializeObject(domainEvent, OutboxSerializerSettings);

            // Act
            var roundTripped = (DomainEvent?)JsonConvert.DeserializeObject(json, domainEvent.GetType(), OutboxSerializerSettings);

            // Assert
            Assert.NotNull(roundTripped);
            Assert.Equal(domainEvent, roundTripped);
            AssertNoNullMember(roundTripped!);
        }

        [Fact]
        public void RoundTrip_FoodCreated_WithOutboxSerializerSettings_PreservesNutritionalInfo()
        {
            // Arrange
            var nutritionalInfo = new NutritionalInfo(100m, 52m, 0.3m, 14m, 0.2m);
            var domainEvent = new FoodCreated(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Manzana",
                "FRUTAS",
                nutritionalInfo,
                UnitOfMeasure.Gram);
            var json = JsonConvert.SerializeObject(domainEvent, OutboxSerializerSettings);

            // Act
            var roundTripped = (FoodCreated?)JsonConvert.DeserializeObject(json, typeof(FoodCreated), OutboxSerializerSettings);

            // Assert
            Assert.NotNull(roundTripped);
            Assert.NotNull(roundTripped!.NutritionalInfo);
            Assert.Equal(nutritionalInfo, roundTripped.NutritionalInfo);
            Assert.Equal(domainEvent.FoodId, roundTripped.FoodId);
            Assert.Equal(domainEvent.Name, roundTripped.Name);
            Assert.Equal(domainEvent.Category, roundTripped.Category);
            Assert.Equal(domainEvent.UnitOfMeasure, roundTripped.UnitOfMeasure);
        }

        [Fact]
        public void RoundTrip_FoodNutritionalInfoUpdated_WithOutboxSerializerSettings_PreservesBothNutritionalInfos()
        {
            // Arrange
            var previous = new NutritionalInfo(100m, 52m, 0.3m, 14m, 0.2m);
            var current = new NutritionalInfo(250m, 130m, 0.8m, 35m, 0.5m);
            var domainEvent = new FoodNutritionalInfoUpdated(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                previous,
                current);
            var json = JsonConvert.SerializeObject(domainEvent, OutboxSerializerSettings);

            // Act
            var roundTripped = (FoodNutritionalInfoUpdated?)JsonConvert.DeserializeObject(json, typeof(FoodNutritionalInfoUpdated), OutboxSerializerSettings);

            // Assert
            Assert.NotNull(roundTripped);
            Assert.NotNull(roundTripped!.PreviousNutritionalInfo);
            Assert.NotNull(roundTripped.NewNutritionalInfo);
            Assert.Equal(previous, roundTripped.PreviousNutritionalInfo);
            Assert.Equal(current, roundTripped.NewNutritionalInfo);
        }

        /// <summary>
        /// Structural guard for the root cause. A public parameterless constructor makes Newtonsoft
        /// choose it as the default creator, after which every get-only payload property is skipped
        /// and the event deserializes empty WITHOUT raising anything. Removing the parameterless
        /// constructors is what forces Newtonsoft onto the fully parameterized constructor, so this
        /// test fails the moment one is reintroduced — the unit tests above only fail once a value
        /// actually happens to be non-default.
        /// </summary>
        [Fact]
        public void EveryDomainEvent_DoesNotExposeAPublicParameterlessConstructor()
        {
            // Arrange
            // typeof(DomainEvent).Assembly is Joseco.DDD.Core; the events live here.
            var domainEventTypes = typeof(FoodCreated).Assembly
                .GetTypes()
                .Where(type => type.IsClass
                               && !type.IsAbstract
                               && typeof(DomainEvent).IsAssignableFrom(type))
                .ToList();

            // Assert
            Assert.NotEmpty(domainEventTypes);
            var offenders = domainEventTypes
                .Where(type => type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes) is not null)
                .Select(type => type.FullName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();
            Assert.Empty(offenders);
        }

        private static void AssertNoNullMember(DomainEvent domainEvent)
        {
            var nullMembers = domainEvent.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.GetValue(domainEvent) is null)
                .Select(property => $"{domainEvent.GetType().Name}.{property.Name}")
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            Assert.Empty(nullMembers);
        }
    }
}
