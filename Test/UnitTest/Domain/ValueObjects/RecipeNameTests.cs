using Catalog.Domain.ValueObjects;
using Catalog.Domain.Primitives;
using Joseco.DDD.Core.Abstractions;
using Xunit;

namespace Catalog.Tests.Domain
{
    public class RecipeNameTests
    {
        [Fact]
        public void Constructor_ValidName_SetsValue()
        {
            // Act
            var nombre = new RecipeName("Ensalada");

            // Assert
            Assert.Equal("Ensalada", nombre.Name);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Constructor_NullOrEmptyName_ThrowsException(string? name)
        {
            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() => new RecipeName(name!));
            Assert.Equal("string cannot be null", ex.Details);
        }

        [Fact]
        public void Constructor_MoreThan200Characters_ThrowsException()
        {
            // Arrange
            var longName = new string('a', 201);

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() => new RecipeName(longName));
            Assert.Equal("Recipe name cannot be more than 200 characters", ex.Details);
        }

        [Fact]
        public void Constructor_WhitespaceName_DoesNotThrow()
        {
            // Act
            var nombre = new RecipeName("   ");

            // Assert
            Assert.Equal("   ", nombre.Name);
        }

        [Fact]
        public void Equality_SameName_AreEqual()
        {
            // Arrange
            var a = new RecipeName("Ensalada");
            var b = new RecipeName("Ensalada");

            // Assert
            Assert.Equal(a, b);
            Assert.NotEqual(a, new RecipeName("Sopa"));
        }

        [Fact]
        public void ImplicitConversion_StringToName_Works()
        {
            // Act
            RecipeName nombre = "Ensalada";

            // Assert
            Assert.Equal("Ensalada", nombre.Name);
            string valor = nombre;
            Assert.Equal("Ensalada", valor);
        }
    }
}
