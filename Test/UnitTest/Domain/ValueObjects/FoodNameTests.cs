using Catalog.Domain.ValueObjects;
using Shared.Core;
using Xunit;

namespace Catalog.Tests.Domain
{
    public class FoodNameTests
    {
        [Fact]
        public void Constructor_ValidName_SetsValue()
        {
            // Act
            var nombre = new FoodName("Manzana");

            // Assert
            Assert.Equal("Manzana", nombre.Name);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Constructor_NullOrEmptyName_ThrowsException(string? name)
        {
            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() => new FoodName(name!));
            Assert.Equal("string cannot be null", ex.Details);
        }

        [Fact]
        public void Constructor_MoreThan100Characters_ThrowsException()
        {
            // Arrange
            var longName = new string('a', 101);

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() => new FoodName(longName));
            Assert.Equal("Food name cannot be more than 100 characters", ex.Details);
        }

        [Fact]
        public void Constructor_WhitespaceName_DoesNotThrow()
        {
            // Act
            var nombre = new FoodName("   ");

            // Assert
            Assert.Equal("   ", nombre.Name);
        }

        [Fact]
        public void Equality_SameName_AreEqual()
        {
            // Arrange
            var a = new FoodName("Manzana");
            var b = new FoodName("Manzana");

            // Assert
            Assert.Equal(a, b);
            Assert.NotEqual(a, new FoodName("Pera"));
        }

        [Fact]
        public void ImplicitConversion_StringToName_Works()
        {
            // Act
            FoodName nombre = "Manzana";

            // Assert
            Assert.Equal("Manzana", nombre.Name);
            string valor = nombre;
            Assert.Equal("Manzana", valor);
        }
    }
}