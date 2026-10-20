using Catalog.Domain;
using Catalog.Domain.Primitives;
using Catalog.Domain.ValueObjects;
using Joseco.DDD.Core.Abstractions;
using Xunit;

namespace Catalog.Tests.Domain
{
    public class PlanDurationTests
    {
        [Theory]
        [InlineData(DurationType.FIFTEEN_DAYS, 15)]
        [InlineData(DurationType.THIRTY_DAYS, 30)]
        public void Constructor_ValidDuration_SetsTipoAndDays(DurationType type, int expectedDays)
        {
            // Act
            var duration = new PlanDuration(type);

            // Assert
            Assert.Equal(type, duration.Type);
            Assert.True(duration.IsValid());
            Assert.Equal(expectedDays, duration.Days());
        }

        [Fact]
        public void Constructor_InvalidDuration_ThrowsException()
        {
            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() => new PlanDuration((DurationType)99));
            Assert.Equal("PlanDuration must be FIFTEEN_DAYS or THIRTY_DAYS", ex.Details);
        }

        [Fact]
        public void Equality_SameTipo_AreEqual()
        {
            // Arrange
            var a = new PlanDuration(DurationType.FIFTEEN_DAYS);
            var b = new PlanDuration(DurationType.FIFTEEN_DAYS);

            // Assert
            Assert.Equal(a, b);
            Assert.NotEqual(a, new PlanDuration(DurationType.THIRTY_DAYS));
        }
    }
}
