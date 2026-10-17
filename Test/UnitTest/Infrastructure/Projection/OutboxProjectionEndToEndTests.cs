using Catalog.Domain.Event.Recipe;
using Catalog.Domain.Model.Foods;
using Catalog.Domain.Model.Recipes;
using Catalog.Domain.ValueObjects;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Persistence.DomainModel;
using Catalog.Infrastructure.Persistence.StoredModel;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.EFCore.Procesor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Catalog.Tests.Infrastructure.Projection
{
    /// <summary>
    /// End-to-end proof that the whole read-model path is wired: the write side stages an outbox row,
    /// the outbox processor running in the worker republishes it, and the projection handler writes it
    /// into the stored/read context. It uses the real service graph, the real unit of work and the real
    /// outbox processor; only the two database providers are swapped for in-memory ones.
    /// </summary>
    public class OutboxProjectionEndToEndTests
    {
        [Fact]
        public async Task Process_WithAStagedRecipeCreated_WritesTheStoredRecipeModel()
        {
            // Arrange
            using var provider = BuildProvider();
            using var scope = provider.CreateScope();
            var storedDbContext = scope.ServiceProvider.GetRequiredService<StoredDbContext>();
            var recipe = new Recipe(new RecipeName("Ensalada de frutas"), "Picar y mezclar.");
            scope.ServiceProvider.GetRequiredService<DomainDbContext>().Recipes.Add(recipe);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync();
            var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor<DomainEvent>>();

            // Act
            await processor.Process(CancellationToken.None);

            // Assert
            var stored = Assert.Single(await storedDbContext.Recipes.ToListAsync());
            Assert.Equal(recipe.Id, stored.Id);
            Assert.Equal("Ensalada de frutas", stored.Name);
        }

        [Fact]
        public async Task Process_WhenTheSameOutboxRowIsProcessedAgain_KeepsOneStoredRecipeModel()
        {
            // Arrange
            using var provider = BuildProvider();
            using var scope = provider.CreateScope();
            var storedDbContext = scope.ServiceProvider.GetRequiredService<StoredDbContext>();
            var recipe = new Recipe(new RecipeName("Ensalada de frutas"), "Picar y mezclar.");
            scope.ServiceProvider.GetRequiredService<DomainDbContext>().Recipes.Add(recipe);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync();
            var domainDbContext = scope.ServiceProvider.GetRequiredService<DomainDbContext>();
            var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor<DomainEvent>>();
            await processor.Process(CancellationToken.None);

            // Act
            var message = await domainDbContext.OutboxMessages.FirstAsync();
            message.Processed = false;
            await domainDbContext.SaveChangesAsync();
            await processor.Process(CancellationToken.None);

            // Assert
            Assert.Single(await storedDbContext.Recipes.ToListAsync());
        }

        /// <summary>
        /// The Food path is the one that carries a value object inside the event payload, so it is
        /// the one that breaks when the outbox cannot rebuild the event. Before the domain events
        /// lost their public parameterless constructors this row arrived with a null
        /// <c>NutritionalInfo</c> and the projection threw a <c>NullReferenceException</c> before
        /// issuing any INSERT. The recipe test above cannot see that class of bug because no recipe
        /// payload carries a value object.
        /// </summary>
        [Fact]
        public async Task Process_WithAStagedFoodCreated_WritesTheStoredFoodModelWithItsNutrition()
        {
            // Arrange
            using var provider = BuildProvider();
            using var scope = provider.CreateScope();
            var storedDbContext = scope.ServiceProvider.GetRequiredService<StoredDbContext>();
            var food = new Food(
                new FoodName("Carne de res"),
                new CategoryName("Carnes"),
                UnitOfMeasure.Gram,
                new NutritionalInfo(100m, 250m, 26m, 0m, 15m));
            scope.ServiceProvider.GetRequiredService<DomainDbContext>().Foods.Add(food);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync();
            var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor<DomainEvent>>();

            // Act
            await processor.Process(CancellationToken.None);

            // Assert
            var stored = Assert.Single(await storedDbContext.Foods.ToListAsync());
            Assert.Equal(food.Id, stored.Id);
            Assert.Equal("Carne de res", stored.Name);
            Assert.Equal("Carnes", stored.Category);
            Assert.Equal(250m, stored.Calories);
            Assert.Equal(26m, stored.Protein);
            Assert.Equal(15m, stored.Fat);
        }

        private static ServiceProvider BuildProvider()
        {
            var databaseName = StoredDbContextFactory.NewDatabaseName();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructure(BuildConfiguration());

            services.RemoveAll<DbContextOptions<DomainDbContext>>();
            services.AddScoped(_ => new DomainDbContext(
                new DbContextOptionsBuilder<DomainDbContext>()
                    .UseInMemoryDatabase(databaseName)
                    .Options));

            services.RemoveAll<DbContextOptions<StoredDbContext>>();
            services.AddScoped(_ => new StoredDbContext(
                new DbContextOptionsBuilder<StoredDbContext>()
                    .UseInMemoryDatabase(databaseName)
                    .Options));

            return services.BuildServiceProvider();
        }

        private static IConfiguration BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:CatalogDbConnectionString"] = "Host=localhost;Database=catalog;Username=test;Password=test"
                })
                .Build();
        }
    }
}
