using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.EFCore;
using Catalog.Application;
using Catalog.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication()
                .AddInfrastructure(builder.Configuration);

builder.Services.AddOutboxBackgroundService<DomainEvent>();

var host = builder.Build();
host.Run();