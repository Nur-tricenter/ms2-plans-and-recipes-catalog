using System.Net.Sockets;

namespace Catalog.Tests.IntegrationTest.RabbitMq
{
    /// <summary>
    /// Cheap, cached reachability probe for the AMQP broker the RabbitMQ integration tests talk to.
    /// </summary>
    /// <remarks>
    /// xUnit 2.x has no runtime skip, so the tests that need a real broker are decorated with
    /// <see cref="RequiresRabbitMqFactAttribute"/> / <see cref="RequiresRabbitMqTheoryAttribute"/>,
    /// which consult this probe at discovery time and set <c>Skip</c> when the broker is absent.
    /// <para>
    /// The probe performs the same opening exchange RabbitMQ.Client 7.x performs, but stops before
    /// authentication: the client writes the AMQP 0-9-1 protocol header, and the broker answers
    /// with a <c>Connection.Start</c> method frame (frame type <c>0x01</c>, class-id <c>0x000A</c>,
    /// method-id <c>0x000A</c>). Verified empirically against rabbitmq:3.13.7: this broker waits
    /// for the client's header before sending anything, and it replies straight with the method
    /// frame instead of echoing its own protocol header. That is enough to tell "a real AMQP broker
    /// answered" apart from "nothing is listening" and "something else grabbed port 5672", while
    /// costing one short round trip and no credentials.
    /// </para>
    /// <para>
    /// The result is memoized in a <see cref="Lazy{T}"/> so a suite with many decorated tests
    /// still probes exactly once, and a missing broker costs one short connect attempt instead of
    /// one per test.
    /// </para>
    /// </remarks>
    public static class RabbitMqProbe
    {
        /// <summary>Host the broker is expected on. Mirrors the WorkerService configuration.</summary>
        public const string Host = "localhost";

        /// <summary>
        /// AMQP port. Hard-coded because Joseco.Communication.External.RabbitMQ 1.1.0 ignores
        /// <c>RabbitMqSettings.Port</c>: it builds its <c>ConnectionFactory</c> from
        /// HostName/UserName/Password only, so host port 5672 is the only one that can work.
        /// </summary>
        public const int Port = 5672;

        private const int TimeoutMilliseconds = 2000;

        private const byte MethodFrameType = 0x01;
        private const ushort ConnectionClassId = 0x000A;
        private const ushort ConnectionStartMethodId = 0x000A;

        /// <summary>AMQP 0-9-1 protocol header: "AMQP" + 0x00 0x00 + major 0, minor 9, revision 1.</summary>
        private static readonly byte[] _protocolHeader = [0x41, 0x4D, 0x51, 0x50, 0x00, 0x00, 0x09, 0x01];

        private static readonly Lazy<bool> _reachable =
            new(Probe, isThreadSafe: true);

        /// <summary>
        /// <c>true</c> when a TCP connection to <see cref="Host"/>:<see cref="Port"/> succeeds and
        /// the peer answers the AMQP opening handshake with a <c>Connection.Start</c> frame.
        /// </summary>
        public static bool IsReachable => _reachable.Value;

        /// <summary>Reason reported by the skip attributes when <see cref="IsReachable"/> is false.</summary>
        public const string SkipReason =
            "Requires a live RabbitMQ broker on localhost:5672 (TCP connect or AMQP handshake failed). " +
            "Start the project-level container 'nur-tricenter-rabbitmq' to run these tests.";

        private static bool Probe()
        {
            try
            {
                using var client = new TcpClient { NoDelay = true };
                using var timeout = new CancellationTokenSource(TimeoutMilliseconds);

                client.ConnectAsync(Host, Port, timeout.Token).GetAwaiter().GetResult();

                using var stream = client.GetStream();
                stream.ReadTimeout = TimeoutMilliseconds;
                stream.WriteTimeout = TimeoutMilliseconds;

                stream.Write(_protocolHeader, 0, _protocolHeader.Length);
                stream.Flush();

                // frame-type (1) + channel (2) + size (4) + class-id (2) + method-id (2).
                var frame = new byte[11];
                if (!ReadExactlyAsync(stream, frame, timeout.Token).GetAwaiter().GetResult())
                {
                    return false;
                }

                ushort classId = (ushort)((frame[7] << 8) | frame[8]);
                ushort methodId = (ushort)((frame[9] << 8) | frame[10]);

                return frame[0] == MethodFrameType
                    && classId == ConnectionClassId
                    && methodId == ConnectionStartMethodId;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static async Task<bool> ReadExactlyAsync(NetworkStream stream, byte[] buffer, CancellationToken cancellationToken)
        {
            var read = 0;
            while (read < buffer.Length)
            {
                var count = await stream.ReadAsync(buffer.AsMemory(read, buffer.Length - read), cancellationToken);
                if (count == 0)
                {
                    return false;
                }

                read += count;
            }

            return true;
        }
    }
}