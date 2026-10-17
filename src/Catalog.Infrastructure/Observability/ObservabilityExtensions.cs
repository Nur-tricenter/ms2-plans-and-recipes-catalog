using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;

namespace Catalog.Infrastructure.Observability;

/// <summary>
/// Traces, metrics and structured logging, wired the same way in both hosts so the WebApi and the
/// WorkerService report under one service name and one set of instruments.
/// </summary>
/// <remarks>
/// <para>
/// <b>Hand-written rather than taken from the reference library.</b> MS2026 exposes this as
/// <c>Nur.Store2026.Observability</c> 1.0.0, but that package ships a <c>net8.0</c> dependency
/// group only and drags OpenTelemetry <b>1.12.0</b> plus two beta exporters
/// (<c>OpenTelemetry.Instrumentation.Process</c> 1.12.0-beta.1 and
/// <c>OpenTelemetry.Exporter.Prometheus.AspNetCore</c> 1.12.0-beta.1) into a graph that
/// deliberately pins <c>OpenTelemetry.Api</c> above the advisory <c>GHSA-g94r-2vxg-569j</c>. The
/// body is small enough to own, and owning it means net10-native packages, no beta dependencies
/// and no third-party dependency on the meter prefix list, which is the one piece of it that
/// actually matters here.
/// </para>
/// <para>
/// <b>Not adopted: Seq.</b> The reference also wires a <c>Serilog.Sinks.Seq</c> sink. There is no
/// Seq server in this project's infrastructure, so a sink pointed at one is configuration that
/// cannot work. Logs go to the console and, through the OTLP exporter, to whatever collector the
/// environment points at.
/// </para>
/// </remarks>
public static class ObservabilityExtensions
{
    /// <summary>
    /// Meter names the provider collects. Wildcards are supported by OpenTelemetry.
    /// </summary>
    /// <remarks>
    /// <c>Store.*</c> is the convention that makes our own meters visible: business meters are
    /// published under the <c>Store.</c> prefix and collected with the wildcard. A meter named
    /// after an assembly matches nothing here and every instrument on it is emitted correctly and
    /// collected nowhere, which is indistinguishable from a bug until someone reads an empty
    /// dashboard. See <c>CatalogMetrics.MeterName</c>.
    /// </remarks>
    private static readonly string[] CollectedMeters =
    [
        "Joseco.Communication.RabbitMQ",
        "Joseco.Outbox",
        "Npgsql",
        "Store.*"
    ];

    /// <summary>
    /// Registers tracing, metrics and logging for <paramref name="serviceName"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceName">
    /// Logical name reported to the collector. Must be the same in both hosts so a trace that
    /// crosses from WebApi to WorkerService is one service's trace, not two.
    /// </param>
    /// <param name="configuration">
    /// Used to decide whether this is an HTTP host, which decides whether ASP.NET Core
    /// instrumentation applies.
    /// </param>
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        string serviceName,
        IConfiguration configuration)
    {
        var isWebApplication = configuration
            .GetValue("Observability:IsWebApplication", defaultValue: true);

        var environment =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Unknown";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: serviceName,
                    serviceNamespace: "store",
                    serviceVersion: typeof(ObservabilityOptions).Assembly.GetName().Version?.ToString())
                .AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("deployment.environment", environment)
                }))
            .WithTracing(tracing => ConfigureTracing(tracing, isWebApplication))
            .WithMetrics(metrics => ConfigureMetrics(metrics, isWebApplication));

        return services;
    }

    private static void ConfigureTracing(TracerProviderBuilder tracing, bool isWebApplication)
    {
        // ASP.NET Core instrumentation is what creates the inbound Activity in the first place and
        // what activates W3C traceparent propagation. Without it there is no Activity.Current for
        // OutboxTracingService to read, so the correlation id would still be null.
        if (isWebApplication)
        {
            tracing.AddAspNetCoreInstrumentation();
        }

        tracing.AddHttpClientInstrumentation()
               .AddOtlpExporter();
    }

    private static void ConfigureMetrics(MeterProviderBuilder metrics, bool isWebApplication)
    {
        if (isWebApplication)
        {
            metrics.AddAspNetCoreInstrumentation();
        }

        metrics.AddRuntimeInstrumentation()
              .AddMeter(CollectedMeters)
              .AddOtlpExporter();
    }

    /// <summary>
    /// Replaces the default logging providers with Serilog.
    /// </summary>
    /// <remarks>
    /// <c>Enrich.WithSpan()</c> is the reason this is worth a dependency: it stamps
    /// <c>TraceId</c> and <c>SpanId</c> from the ambient activity onto every log event, so a log line
    /// and the trace it belongs to can be joined without any code of ours.
    /// </remarks>
    public static IHostBuilder UseCatalogLogging(this IHostBuilder builder, string serviceName)
        => builder.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithSpan()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProcessId()
            .Enrich.WithProperty("ServiceName", serviceName)
            .WriteTo.Console());
}