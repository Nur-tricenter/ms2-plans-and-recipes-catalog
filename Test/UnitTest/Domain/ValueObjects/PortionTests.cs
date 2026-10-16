using Catalog.Domain.ValueObjects;
using Catalog.Domain.Primitives;
using Joseco.DDD.Core.Abstractions;
using Xunit;

namespace Catalog.Tests.Domain
{
    public class PortionTests
    {
        [Fact]
        public void Constructor_PositiveQuantity_SetsValue()
        {
            // Act
            var portion = new Portion(2.5m);

            // Assert
            Assert.Equal(2.5m, portion.Quantity);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void Constructor_NonPositiveQuantity_ThrowsException(decimal quantity)
        {
            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() => new Portion(quantity));
            Assert.Equal("Portion quantity must be greater than zero", ex.Details);
        }

        [Fact]
        public void Equality_SameQuantity_AreEqual()
        {
            // Arrange
            var a = new Portion(2m);
            var b = new Portion(2m);

            // Assert
            Assert.Equal(a, b);
            Assert.NotEqual(a, new Portion(3m));
        }
    }
}
