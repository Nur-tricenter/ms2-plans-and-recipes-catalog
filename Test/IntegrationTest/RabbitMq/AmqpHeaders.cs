using System.Text;

namespace Catalog.Tests.IntegrationTest.RabbitMq
{
    /// <summary>
    /// Decodes the values RabbitMQ returns in a message's header table.
    /// </summary>
    /// <remarks>
    /// AMQP header values are typed by the protocol, not by the publisher's intent: a header the
    /// library wrote as a C# string travels as an AMQP <c>longstr</c>, and RabbitMQ.Client 7.x hands
    /// <c>longstr</c> values back as a raw <see cref="byte"/>[] holding the UTF-8 bytes. Comparing
    /// such a value with <c>Assert.Equal(expected, value)</c> would compare a string against
    /// <c>byte[]</c> and always fail, so the wire bytes are decoded first. This is a faithful
    /// decoding of what the broker stored, not a loosened assertion: the decoded text must still
    /// equal the expected value exactly.
    /// </remarks>
    public static class AmqpHeaders
    {
        public const string OriginalQueue = "x-original-queue";
        public const string OriginalExchange = "x-original-exchange";
        public const string FailureReason = "x-failure-reason";
        public const string Attempts = "x-attempts";
        public const string FailedAt = "x-failed-at";

        /// <summary>Renders a header value as text regardless of the AMQP type it arrived as.</summary>
        public static string? AsText(object? value)
        {
            return value switch
            {
                null => null,
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                _ => value.ToString()
            };
        }
    }
}