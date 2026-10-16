using Catalog.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace Catalog.Tests.IntegrationTest.Setup
{
    public class FoodControllerWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"IntTests_{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.ReplaceInfrastructureWithInMemory(_dbName);
            });
        }
    }

    public class RecipeControllerWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"IntTests_{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.ReplaceInfrastructureWithInMemory(_dbName);
            });
        }
    }

    public class MealPlanControllerWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"IntTests_{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.ReplaceInfrastructureWithInMemory(_dbName);
            });
        }
    }
}