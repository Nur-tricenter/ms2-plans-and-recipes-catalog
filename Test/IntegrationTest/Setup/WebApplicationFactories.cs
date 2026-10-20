using Catalog.App;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

// Aliased on purpose. The test project references Catalog.WorkerService, whose top-level statements
// produce a SECOND type named Program, so an unqualified `Program` in this file does not reliably
// mean the API's. That ambiguity is the whole subject of CatalogWebApplicationFactory below, and
// leaving it unqualified here would reintroduce it one level down.
using WebApiProgram = Catalog.App.Program;

namespace Catalog.Tests.IntegrationTest.Setup
{
    /// <summary>
    /// Shared base for the controller factories, existing for one reason: to build the host
    /// explicitly instead of letting <c>WebApplicationFactory</c> go looking for it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why.</b> By default <c>WebApplicationFactory&lt;TEntryPoint&gt;</c> locates the entry point
    /// through <c>HostFactoryResolver</c>, which scans the assemblies sitting in the test output for
    /// a <c>Main</c>. This test project references <c>Catalog.WorkerService</c> so the worker's DI
    /// composition can be unit tested, and that reference puts a <b>second</b> entry point next to the
    /// API's. The resolver then takes whichever it finds first, which is how the worker service's
    /// <c>Program.Main</c> ended up booting inside a controller test: the worker demands
    /// <c>RabbitMqSettings</c> that a controller test has no reason to supply, so every controller
    /// test failed with <c>Configuration section 'RabbitMqSettings' is missing or empty</c> from a
    /// stack frame that had nothing to do with the controller under test.
    /// </para>
    /// <para>
    /// <b>Why it surfaced now.</b> The ambiguity is pre-existing. Adding a package to
    /// <c>Catalog.WorkerService</c> changed what the resolver enumerates and tipped it toward the
    /// wrong entry point. A test host that depends on scan order is broken whether or not it happens
    /// to fail today, so the guess is removed instead of the symptom patched.
    /// </para>
    /// </remarks>
    public abstract class CatalogWebApplicationFactory : WebApplicationFactory<WebApiProgram>
    {
        private readonly string _dbName = $"IntTests_{Guid.NewGuid()}";

        /// <summary>
        /// Builds the API host from <see cref="WebApiProgram.CreateHostBuilder"/> directly, which is what
        /// removes the entry-point scan.
        /// </summary>
        /// <remarks>
        /// Still the production composition, so the suite keeps exercising what a deployment runs.
        /// That is the entire value of an integration test in this project: it boots the real
        /// <c>Startup</c>, which is precisely what the bug above was hiding.
        /// </remarks>
        protected override IHostBuilder CreateHostBuilder() =>
            WebApiProgram.CreateHostBuilder(Array.Empty<string>());

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.DisableServiceDiscovery();

            builder.ConfigureTestServices(services =>
            {
                services.ReplaceInfrastructureWithInMemory(_dbName);
            });
        }
    }

    public class FoodControllerWebApplicationFactory : CatalogWebApplicationFactory
    {
    }

    public class RecipeControllerWebApplicationFactory : CatalogWebApplicationFactory
    {
    }

    public class MealPlanControllerWebApplicationFactory : CatalogWebApplicationFactory
    {
    }
}