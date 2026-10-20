using Xunit;

namespace Catalog.Tests.IntegrationTest.RabbitMq
{
    /// <summary>
    /// Data-driven counterpart of <see cref="RequiresRabbitMqFactAttribute"/>: every row of the
    /// theory is skipped unless a live RabbitMQ broker is listening on <c>localhost:5672</c>.
    /// </summary>
    /// <remarks>
    /// xUnit 2.x reports a skipped <see cref="TheoryAttribute"/> once per test case, so setting
    /// <c>Skip</c> here keeps the data visible in the run output instead of silently disappearing.
    /// </remarks>
    public sealed class RequiresRabbitMqTheoryAttribute : TheoryAttribute
    {
        public RequiresRabbitMqTheoryAttribute()
        {
            if (!RabbitMqProbe.IsReachable)
            {
                Skip = RabbitMqProbe.SkipReason;
            }
        }
    }
}