namespace Catalog.Infrastructure.Observability;

/// <summary>
/// Telemetry identity shared by every host in this service.
/// </summary>
/// <remarks>
/// One constant in <c>Catalog.Infrastructure</c>, referenced by both <c>Catalog.App</c> and
/// <c>Catalog.WorkerService</c>, because the two are processes of a single service rather than two
/// services. A trace that leaves an HTTP request in the API, lands in an outbox row and is picked up
/// by the worker has to read as one trace of one service; two different names split it in the
/// collector and make the hop look unrelated.
/// </remarks>
public static class ObservabilityOptions
{
    /// <summary>
    /// Logical service name reported to the collector.
    /// </summary>
    /// <remarks>
    /// Matches the meter name by design. They are two different concepts that happen to want the
    /// same identity: the meter namespace is what the provider collects, the service name is what
    /// the collector labels the resource, and a mismatch between them is how a trace and its metrics
    /// end up impossible to join.
    /// </remarks>
    public const string ServiceName = "Store.Catalog";
}