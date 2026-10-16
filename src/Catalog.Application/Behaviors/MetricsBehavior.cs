using MediatR;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Catalog.Application.Behaviors
{
    /// <summary>
    /// Observation-only pipeline behavior: it records request duration and outcome on
    /// System.Diagnostics.Metrics instruments and never changes control flow or the result.
    /// </summary>
    public class MetricsBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private static readonly KeyValuePair<string, object?> RequestNameTag =
            new(CatalogMetrics.RequestNameTag, typeof(TRequest).Name);

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var response = await next();
                Record(stopwatch, CatalogMetrics.SuccessOutcome);
                return response;
            }
            catch (Exception)
            {
                Record(stopwatch, CatalogMetrics.FailureOutcome);
                throw;
            }
        }

        private static void Record(Stopwatch stopwatch, string outcome)
        {
            stopwatch.Stop();

            CatalogMetrics.RequestDuration.Record(stopwatch.Elapsed.TotalMilliseconds, RequestNameTag);
            CatalogMetrics.RequestCount.Add(
                1,
                RequestNameTag,
                new KeyValuePair<string, object?>(CatalogMetrics.OutcomeTag, outcome));
        }
    }

    /// <summary>
    /// Owns the single application-wide <see cref="Meter"/> and its instruments. Kept in a
    /// non-generic type so a process registers one meter rather than one per request type.
    /// </summary>
    internal static class CatalogMetrics
    {
        internal const string RequestNameTag = "catalog.request.name";
        internal const string OutcomeTag = "catalog.request.outcome";
        internal const string SuccessOutcome = "success";
        internal const string FailureOutcome = "failure";

        internal static readonly Meter Meter = new Meter("Catalog.Application", "1.0.0");

        internal static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
            "catalog.application.request.duration",
            unit: "ms",
            description: "Elapsed time in milliseconds to handle an Application layer request.");

        internal static readonly Counter<long> RequestCount = Meter.CreateCounter<long>(
            "catalog.application.request.count",
            unit: "{request}",
            description: "Number of Application layer requests handled, tagged by outcome.");
    }
}