using Xunit;

namespace Catalog.Tests.IntegrationTest.RabbitMq
{
    /// <summary>
    /// A <see cref="FactAttribute"/> that turns itself into a skip when no live RabbitMQ broker is
    /// listening on <c>localhost:5672</c>.
    /// </summary>
    /// <remarks>
    /// xUnit 2.x cannot skip at runtime: a test either declares a constant <c>Skip</c> or always runs.
    /// Setting <c>Skip</c> from the constructor — which xUnit instantiates during discovery, before
    /// the test is ever scheduled — is the supported way to make a broker-dependent test degrade
    /// gracefully on a machine without Docker while still really executing wherever the broker is up.
    /// </remarks>
    public sealed class RequiresRabbitMqFactAttribute : FactAttribute
    {
        public RequiresRabbitMqFactAttribute()
        {
            if (!RabbitMqProbe.IsReachable)
            {
                Skip = RabbitMqProbe.SkipReason;
            }
        }
    }
}