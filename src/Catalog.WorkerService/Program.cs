using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.EFCore;
using Catalog.Application;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Observability;
using Catalog.WorkerService;
using Nur.Tricenter.Observability.Tracing;
using Serilog;
using Serilog.Enrichers.Span;

var builder = Host.CreateApplicationBuilder(args);

// Structured logging with the ambient trace stamped onto every event. Applied before the service
// registrations so the bootstrap log lines are enriched too.
//
// Serilog is configured HERE rather than through the shared kernel's ObservabilityExtensions
// .UseLogging(...) because that extension is declared on IHostBuilder, and
// Host.CreateApplicationBuilder returns a HostApplicationBuilder -- a different type, with no
// IHostBuilder to hang the extension off. Switching this host to Host.CreateDefaultBuilder just
// to get the extension would be a larger change than the logging wiring is worth, so the same
// enrichers the kernel applies are spelled out directly. Keep this list in step with
// Nur.Tricenter.Observability.Tracing.ObservabilityExtensions.UseLogging.
builder.Services.AddSerilog((services, configuration) => configuration
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithSpan()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProcessId()
    .Enrich.WithProperty("ServiceName", ObservabilityOptions.ServiceName)
    .WriteTo.Console());

// AddInfrastructure() calls AddApplication() internally (Catalog.Infrastructure/Extensions.cs),
// so this host gets the factories and the MediatR pipeline without a direct project reference to
// Catalog.Application. Calling it again here would be redundant.
builder.Services.AddInfrastructure(builder.Configuration);

// Same service name as the WebApi on purpose: a trace that crosses the outbox from the API to the
// worker has to read as one service's trace, not two.
//
// shouldIncludeHttpInstrumentation is FALSE: this host serves no HTTP traffic, so ASP.NET Core and
// HttpClient instrumentation would only add noise. Runtime and meter instrumentation stay on, which
// is what makes the outbox poll and the RabbitMQ publishes visible.
builder.Services.AddObservability(
    ObservabilityOptions.ServiceName,
    shouldIncludeHttpInstrumentation: false);

// RabbitMQ producer side: binds RabbitMqSettings, registers it as a singleton and then the
// publisher. See RabbitMqExtensions.AddCatalogRabbitMq for why no consumer is registered.
builder.Services.AddCatalogRabbitMq(builder.Configuration);

// The WorkerService is the only host that runs OutboxProcessor<DomainEvent>, so it is the only
// host that needs the outbox-to-broker publishers. AddApplication() deliberately leaves them out.
builder.Services.AddIntegrationMessaging();

builder.Services.AddOutboxBackgroundService<DomainEvent>();

// Declares (and never consumes) the single `catalog.default` queue, bound to every catalog
// exchange, so the broker keeps the complete record of everything published.
builder.Services.AddHostedService<DefaultQueueRegistrar>();

var host = builder.Build();
host.Run();
