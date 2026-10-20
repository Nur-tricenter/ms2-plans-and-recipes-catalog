using Catalog.Domain.ValueObjects;
using Catalog.Domain.Primitives;
using Joseco.DDD.Core.Abstractions;
using Xunit;

namespace Catalog.Tests.Domain
{
    public class NutritionalInfoTests
    {
        [Fact]
        public void Constructor_ValidData_SetsAllValues()
        {
            // Act
            var info = new NutritionalInfo(100, 52, 0.3m, 14, 0.2m);

            // Assert
            Assert.Equal(100m, info.Quantity);
            Assert.Equal(52m, info.Calories);
            Assert.Equal(0.3m, info.Protein);
            Assert.Equal(14m, info.Carbs);
            Assert.Equal(0.2m, info.Fat);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-0.5)]
        [InlineData(-100)]
        public void Constructor_NonPositiveCantidad_ThrowsException(decimal quantity)
        {
            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() =>
                new NutritionalInfo(quantity, 52, 0.3m, 14, 0.2m));
            Assert.Equal("NutritionalInfo quantity must be greater than zero", ex.Details);
        }

        [Fact]
        public void Equality_SameValues_AreEqual()
        {
            // Arrange
            var a = new NutritionalInfo(100, 52, 0.3m, 14, 0.2m);
            var b = new NutritionalInfo(100, 52, 0.3m, 14, 0.2m);

            // Assert
            Assert.Equal(a, b);
            Assert.NotEqual(a, new NutritionalInfo(100, 60, 0.3m, 14, 0.2m));
        }
    }
}
