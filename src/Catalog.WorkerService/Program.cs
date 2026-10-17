using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.EFCore;
using Catalog.Application;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Observability;
using Catalog.WorkerService;
using Serilog;
using Serilog.Enrichers.Span;

var builder = Host.CreateApplicationBuilder(args);

// Structured logging with the ambient trace stamped onto every event. Applied before the service
// registrations so the bootstrap log lines are enriched too.
builder.Services.AddSerilog((services, configuration) => configuration
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithSpan()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProcessId()
    .Enrich.WithProperty("ServiceName", ObservabilityOptions.ServiceName)
    .WriteTo.Console());

builder.Services.AddApplication()
                .AddInfrastructure(builder.Configuration);

// Same service name as the WebApi on purpose: a trace that crosses the outbox from the API to the
// worker has to read as one service's trace, not two.
builder.Services.AddObservability(ObservabilityOptions.ServiceName, builder.Configuration);

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
