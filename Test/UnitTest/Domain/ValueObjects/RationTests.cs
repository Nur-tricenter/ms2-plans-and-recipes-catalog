using Catalog.Domain.ValueObjects;
using Catalog.Domain.Primitives;
using Joseco.DDD.Core.Abstractions;
using Xunit;

namespace Catalog.Tests.Domain
{
    public class RationTests
    {
        [Fact]
        public void Constructor_PositiveQuantity_SetsValue()
        {
            // Act
            var ration = new Ration(2);

            // Assert
            Assert.Equal(2, ration.Quantity);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void Constructor_NonPositiveQuantity_ThrowsException(int quantity)
        {
            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() => new Ration(quantity));
            Assert.Equal("Ration must be greater than zero", ex.Details);
        }

        [Fact]
        public void Equality_SameQuantity_AreEqual()
        {
            // Arrange
            var a = new Ration(2);
            var b = new Ration(2);

            // Assert
            Assert.Equal(a, b);
            Assert.NotEqual(a, new Ration(3));
        }
    }
}
