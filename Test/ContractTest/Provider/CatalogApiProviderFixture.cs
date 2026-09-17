using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Catalog.Tests.ContractTest.Provider
{
    /// <summary>
    /// Hosts the Catalog API on a real TCP socket (Kestrel) for the duration of the provider
    /// verification. Pact's Rust core calls the provider out-of-process, so a
    /// <c>WebApplicationFactory</c>/<c>TestServer</c> (in-memory only) cannot be used here.
    /// A dynamic port (<c>:0</c>) avoids collisions with other services or CI jobs.
    /// </summary>
    public class CatalogApiProviderFixture : IDisposable
    {
        private readonly IHost _host;

        public Uri ServerUri { get; }

        public CatalogApiProviderFixture()
        {
            var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
            var webApiRoot = Path.Combine(repositoryRoot, "WebApi");

            _host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://127.0.0.1:0");
                    webBuilder.UseContentRoot(webApiRoot);
                    webBuilder.UseStartup<ContractPactStartup>();
                })
                .Build();

            _host.Start();

            var server = _host.Services.GetRequiredService<IServer>();
            var address = server.Features.Get<IServerAddressesFeature>()!.Addresses.First();
            ServerUri = new Uri(address);
        }

        public void Dispose()
        {
            _host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            _host.Dispose();
        }
    }
}