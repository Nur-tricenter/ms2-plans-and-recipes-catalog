using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.EFCore;
using Catalog.Application;
using Catalog.Infrastructure;
using Catalog.WorkerService;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication()
                .AddInfrastructure(builder.Configuration);

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
