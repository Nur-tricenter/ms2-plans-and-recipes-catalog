using Catalog.Domain.ValueObjects;
using Shared.Core;
using Xunit;

namespace Catalog.Tests.Domain
{
    public class MealPlanNameTests
    {
        [Fact]
        public void Constructor_ValidName_SetsValue()
        {
            // Act
            var nombre = new MealPlanName("Plan Saludable");

            // Assert
            Assert.Equal("Plan Saludable", nombre.Name);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Constructor_NullOrEmptyName_ThrowsException(string? name)
        {
            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() => new MealPlanName(name!));
            Assert.Equal("string cannot be null", ex.Details);
        }

        [Fact]
        public void Constructor_MoreThan200Characters_ThrowsException()
        {
            // Arrange
            var longName = new string('a', 201);

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() => new MealPlanName(longName));
            Assert.Equal("Meal plan name cannot be more than 200 characters", ex.Details);
        }

        [Fact]
        public void Constructor_WhitespaceName_DoesNotThrow()
        {
            // Act
            var nombre = new MealPlanName("   ");

            // Assert
            Assert.Equal("   ", nombre.Name);
        }

        [Fact]
        public void Equality_SameName_AreEqual()
        {
            // Arrange
            var a = new MealPlanName("Plan Saludable");
            var b = new MealPlanName("Plan Saludable");

            // Assert
            Assert.Equal(a, b);
            Assert.NotEqual(a, new MealPlanName("Plan Deportivo"));
        }

        [Fact]
        public void ImplicitConversion_StringToName_Works()
        {
            // Act
            MealPlanName nombre = "Plan Saludable";

            // Assert
            Assert.Equal("Plan Saludable", nombre.Name);
            string valor = nombre;
            Assert.Equal("Plan Saludable", valor);
        }
    }
}