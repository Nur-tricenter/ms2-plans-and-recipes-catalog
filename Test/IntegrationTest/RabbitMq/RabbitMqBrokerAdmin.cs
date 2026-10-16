using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Catalog.Tests.IntegrationTest.RabbitMq
{
    /// <summary>
    /// Thin administrative client used by the RabbitMQ integration tests to observe the broker and
    /// to clean up after itself. It talks to the SAME live broker as the code under test, using its
    /// own AMQP connection, so assertions are made on broker state rather than on test doubles.
    /// </summary>
    /// <remarks>
    /// Every operation runs on a short-lived channel. In RabbitMQ a channel error (for example the
    /// 404-NOT_FOUND returned by a passive declare of a queue that does not exist yet) is
    /// channel-scoped and closes only that channel, not the connection. Using one throwaway channel
    /// per probe keeps a "not there yet" poll from poisoning the channel used by the next poll.
    /// </remarks>
    public sealed class RabbitMqBrokerAdmin : IAsyncDisposable
    {
        private readonly IConnection _connection;

        private RabbitMqBrokerAdmin(IConnection connection) => _connection = connection;

        /// <summary>Opens an administrative connection to the broker under test.</summary>
        public static async Task<RabbitMqBrokerAdmin> ConnectAsync()
        {
            var factory = new ConnectionFactory
            {
                HostName = RabbitMqProbe.Host,
                Port = RabbitMqProbe.Port,
                UserName = RabbitMqTestSettings.UserName,
                Password = RabbitMqTestSettings.Password,
                VirtualHost = RabbitMqTestSettings.VirtualHost
            };

            var connection = await factory.CreateConnectionAsync("catalog-tests-admin");
            return new RabbitMqBrokerAdmin(connection);
        }

        /// <summary>
        /// Passive-declares <paramref name="queue"/> and returns its message count, or <c>null</c>
        /// when the queue does not exist yet.
        /// </summary>
        public async Task<uint?> TryGetMessageCountAsync(string queue)
        {
            await using var channel = await _connection.CreateChannelAsync();
            try
            {
                var ok = await channel.QueueDeclarePassiveAsync(queue);
                return ok.MessageCount;
            }
            catch (OperationInterruptedException)
            {
                return null;
            }
        }

        /// <summary>
        /// Waits until <paramref name="queue"/> exists and reports exactly
        /// <paramref name="expectedMessages"/> ready messages. Returns the final observed count, or
        /// <c>null</c> if the queue never showed up before <paramref name="timeout"/>.
        /// </summary>
        public async Task<uint?> WaitForMessageCountAsync(
            string queue,
            int expectedMessages,
            TimeSpan timeout)
        {
            var deadline = DateTimeOffset.UtcNow + timeout;
            uint? observed = null;

            do
            {
                observed = await TryGetMessageCountAsync(queue);
                if (observed == expectedMessages)
                {
                    return observed;
                }

                await Task.Delay(250);
            }
            while (DateTimeOffset.UtcNow < deadline);

            return observed;
        }

        /// <summary>
        /// Takes a single ready message from <paramref name="queue"/> without acknowledging it,
        /// or returns <c>null</c> when the queue is missing or empty. The caller owns the delivery
        /// tag and must ack it (see <see cref="AckAsync"/>).
        /// </summary>
        public async Task<BasicGetResult?> PeekAsync(string queue)
        {
            await using var channel = await _connection.CreateChannelAsync();
            try
            {
                return await channel.BasicGetAsync(queue, autoAck: false);
            }
            catch (OperationInterruptedException)
            {
                return null;
            }
        }

        /// <summary>
        /// Takes and acknowledges a single ready message from <paramref name="queue"/>, or returns
        /// <c>null</c> when the queue is missing or empty. Used to drain and re-check a dead-letter
        /// queue without leaving unacked deliveries behind on a closed channel.
        /// </summary>
        public async Task<BasicGetResult?> TakeAsync(string queue)
        {
            await using var channel = await _connection.CreateChannelAsync();
            try
            {
                var result = await channel.BasicGetAsync(queue, autoAck: false);
                if (result is not null)
                {
                    await channel.BasicAckAsync(result.DeliveryTag, multiple: false);
                }

                return result;
            }
            catch (OperationInterruptedException)
            {
                return null;
            }
        }

        /// <summary>
        /// Produces a human-readable snapshot of a queue (existence, depth and the first ready
        /// message with its headers and body) so a failing assertion can explain itself instead of
        /// reporting a bare timeout.
        /// </summary>
        public async Task<string> DescribeQueueAsync(string queue)
        {
            var count = await TryGetMessageCountAsync(queue);
            if (count is null)
            {
                return $"queue '{queue}' does not exist";
            }

            var peek = await PeekAsync(queue);
            if (peek is null)
            {
                return $"queue '{queue}' exists and is empty";
            }

            var headers = peek.BasicProperties.Headers is null
                ? "<none>"
                : string.Join(", ", peek.BasicProperties.Headers
                    .Select(pair => $"{pair.Key}={AmqpHeaders.AsText(pair.Value)}"));

            return $"queue '{queue}' exists with {count} ready message(s); " +
                   $"headers [{headers}]; " +
                   $"body {System.Text.Encoding.UTF8.GetString(peek.Body.ToArray())}";
        }

        /// <summary>Deletes a queue, tolerating the case where it does not exist.</summary>
        public async Task DeleteQueueAsync(string queue)
        {
            await using var channel = await _connection.CreateChannelAsync();
            try
            {
                await channel.QueueDeleteAsync(queue, ifUnused: false, ifEmpty: false);
            }
            catch (OperationInterruptedException)
            {
                // Queue already gone: nothing to clean up.
            }
        }

        /// <summary>Deletes an exchange, tolerating the case where it does not exist.</summary>
        public async Task DeleteExchangeAsync(string exchange)
        {
            await using var channel = await _connection.CreateChannelAsync();
            try
            {
                await channel.ExchangeDeleteAsync(exchange, ifUnused: false);
            }
            catch (OperationInterruptedException)
            {
                // Exchange already gone: nothing to clean up.
            }
        }

        public async ValueTask DisposeAsync() => await _connection.DisposeAsync();
    }
}