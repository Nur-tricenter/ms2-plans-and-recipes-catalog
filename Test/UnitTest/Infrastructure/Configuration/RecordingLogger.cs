using Microsoft.Extensions.Logging;

namespace Catalog.Tests.Infrastructure.Configuration
{
    /// <summary>
    /// Captures the formatted log messages a component under test emits.
    /// </summary>
    /// <remarks>
    /// Used where the observable contract IS the log: <see cref="DatabaseMigrator"/> promises it
    /// says out loud what it did and, just as importantly, that it skipped a context and why. A
    /// <c>NullLogger</c> would make every one of those assertions vacuous, and Moq over
    /// <c>ILogger&lt;T&gt;</c> cannot see the formatted text because the message template and its
    /// arguments are separate arguments.
    /// <para>
    /// <see cref="IsEnabled"/> answers <c>true</c> for every level on purpose: the default minimum
    /// level of a bare <c>ILogger&lt;T&gt;</c> implementation is up to the implementation, and a
    /// recorder that filtered would make these tests pass or fail depending on nothing.
    /// </para>
    /// </remarks>
    public sealed class RecordingLogger<T> : ILogger<T>
    {
        private readonly List<string> _messages = new();
        private readonly object _gate = new();

        public IReadOnlyList<string> Messages
        {
            get
            {
                lock (_gate)
                {
                    return _messages.ToArray();
                }
            }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (_gate)
            {
                _messages.Add(formatter(state, exception));
            }
        }
    }
}
