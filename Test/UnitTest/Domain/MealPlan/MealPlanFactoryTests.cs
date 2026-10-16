using Catalog.Domain;
using Catalog.Domain.Factory.MealPlan;
using Catalog.Domain.Model.MealPlans;
using Catalog.Domain.ValueObjects;
using Shared.Core;
using Xunit;

namespace Catalog.Tests.Domain
{
    public class MealPlanFactoryTests
    {
        [Theory]
        [InlineData(DurationType.FIFTEEN_DAYS, 15)]
        [InlineData(DurationType.THIRTY_DAYS, 30)]
        public void Create_ValidDuration_CreatesPlanWithExpectedDays(DurationType type, int expectedDays)
        {
            // Arrange
            var factory = new MealPlanFactory();
            var duration = new PlanDuration(type);

            // Act
            var plan = factory.Create("Plan Saludable", duration, 3);

            // Assert
            Assert.IsType<MealPlan>(plan);
            Assert.NotEqual(Guid.Empty, plan.Id);
            Assert.Equal("Plan Saludable", plan.Name.Name);
            Assert.Equal(3, plan.MealsPerDay);
            Assert.Equal(expectedDays, plan.PlanDays.Count());
            Assert.Single(plan.DomainEvents);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Create_NullOrEmptyName_ThrowsException(string? name)
        {
            // Arrange
            var factory = new MealPlanFactory();
            var duration = new PlanDuration(DurationType.FIFTEEN_DAYS);

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() =>
                factory.Create(name!, duration, 3));
            Assert.Equal("string cannot be null", ex.Details);
        }

        [Fact]
        public void Create_WhitespaceName_CreatesPlan()
        {
            // Arrange
            var factory = new MealPlanFactory();
            var duration = new PlanDuration(DurationType.FIFTEEN_DAYS);

            // Act
            var plan = factory.Create("   ", duration, 3);

            // Assert
            Assert.Equal("   ", plan.Name.Name);
        }

        [Fact]
        public void Create_ZeroMealsPerDay_CreatesPlan()
        {
            // Arrange
            var factory = new MealPlanFactory();
            var duration = new PlanDuration(DurationType.FIFTEEN_DAYS);

            // Act
            var plan = factory.Create("Plan", duration, 0);

            // Assert
            Assert.Equal(0, plan.MealsPerDay);
        }
    }
}