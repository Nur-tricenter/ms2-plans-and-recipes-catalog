using Catalog.Application.UseCase.DomainEventHandler;
using Catalog.Domain.Event.Food;
using Catalog.Domain.Event.MealPlan;
using Catalog.Domain.Event.Recipe;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Persistence.StoredModel;
using Catalog.Infrastructure.Persistence.StoredModel.Projection;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Catalog.Tests.Infrastructure.Projection
{
    /// <summary>
    /// Proves that the read-model projection handlers are discoverable by MediatR through the real
    /// service graph that <see cref="Extensions.AddInfrastructure(IServiceCollection, IConfiguration)"/>
    /// builds, and that they resolve every collaborator the outbox processor needs in its own scope.
    /// </summary>
    public class ProjectionHandlerRegistrationTests
    {
        [Fact]
        public void AddInfrastructure_RegistersTheProjectionHandlersForEveryDomainEvent()
        {
            // Arrange
            using var provider = BuildProvider();

            // Act
            var foodCreated = provider.GetServices<INotificationHandler<OutboxMessage<FoodCreated>>>();
            var foodUpdated = provider.GetServices<INotificationHandler<OutboxMessage<FoodNutritionalInfoUpdated>>>();
            var recipeCreated = provider.GetServices<INotificationHandler<OutboxMessage<RecipeCreated>>>();
            var recipeUpdated = provider.GetServices<INotificationHandler<OutboxMessage<RecipeUpdated>>>();
            var ingredientAdded = provider.GetServices<INotificationHandler<OutboxMessage<IngredientAddedToRecipe>>>();
            var ingredientRemoved = provider.GetServices<INotificationHandler<OutboxMessage<IngredientRemovedFromRecipe>>>();
            var planCreated = provider.GetServices<INotificationHandler<OutboxMessage<MealPlanCreated>>>();
            var planUpdated = provider.GetServices<INotificationHandler<OutboxMessage<MealPlanUpdated>>>();
            var mealTimeAdded = provider.GetServices<INotificationHandler<OutboxMessage<MealTimeAddedToPlan>>>();
            var recipeAssigned = provider.GetServices<INotificationHandler<OutboxMessage<RecipeAssignedToMealTime>>>();
            var recipeRemoved = provider.GetServices<INotificationHandler<OutboxMessage<RecipeRemovedFromMealTime>>>();

            // Assert
            Assert.Contains(foodCreated, handler => handler is ProjectFoodModelWhenFoodCreated);
            Assert.Contains(foodUpdated, handler => handler is ProjectFoodModelWhenFoodNutritionalInfoUpdated);
            Assert.Contains(recipeCreated, handler => handler is ProjectRecipeModelWhenRecipeCreated);
            Assert.Contains(recipeUpdated, handler => handler is ProjectRecipeModelWhenRecipeUpdated);
            Assert.Contains(ingredientAdded, handler => handler is ProjectIngredientModelWhenIngredientAddedToRecipe);
            Assert.Contains(ingredientRemoved, handler => handler is ProjectIngredientModelWhenIngredientRemovedFromRecipe);
            Assert.Contains(planCreated, handler => handler is ProjectMealPlanModelWhenMealPlanCreated);
            Assert.Contains(planUpdated, handler => handler is ProjectMealPlanModelWhenMealPlanUpdated);
            Assert.Contains(mealTimeAdded, handler => handler is ProjectMealTimeModelWhenMealTimeAddedToPlan);
            Assert.Contains(recipeAssigned, handler => handler is ProjectRecipeAssignmentModelWhenRecipeAssignedToMealTime);
            Assert.Contains(recipeRemoved, handler => handler is ProjectRecipeAssignmentModelWhenRecipeRemovedFromMealTime);
        }

        [Fact]
        public void AddInfrastructure_RegistersBothTheApplicationAndTheInfrastructureHandlerFamilies()
        {
            // Arrange
            using var provider = BuildProvider();

            // Act
            var domainEventHandlers = provider.GetServices<INotificationHandler<RecipeCreated>>();
            var outboxHandlers = provider.GetServices<INotificationHandler<OutboxMessage<RecipeCreated>>>();

            // Assert
            Assert.Contains(domainEventHandlers, handler => handler is SaveInOutboxWhenRecipeCreated);
            Assert.Contains(outboxHandlers, handler => handler is ProjectRecipeModelWhenRecipeCreated);
        }

        [Fact]
        public void AddInfrastructure_KeepsTheWriteSideAndTheProjectionOnDifferentNotifications()
        {
            // Arrange
            using var provider = BuildProvider();

            // Act
            var domainEventHandlers = provider.GetServices<INotificationHandler<RecipeCreated>>();
            var outboxHandlers = provider.GetServices<INotificationHandler<OutboxMessage<RecipeCreated>>>();

            // Assert
            Assert.DoesNotContain(domainEventHandlers, handler => handler is ProjectRecipeModelWhenRecipeCreated);
            Assert.DoesNotContain(outboxHandlers, handler => handler is SaveInOutboxWhenRecipeCreated);
        }

        [Fact]
        public async Task AddInfrastructure_ResolvesAProjectionHandlerWithAWorkingStoredDbContext()
        {
            // Arrange
            using var provider = BuildProvider(useInMemoryStoredDb: true);
            using var scope = provider.CreateScope();
            var handler = scope.ServiceProvider
                .GetRequiredService<INotificationHandler<OutboxMessage<RecipeCreated>>>();
            var dbContext = scope.ServiceProvider.GetRequiredService<StoredDbContext>();
            var recipeCreated = new RecipeCreated(Guid.NewGuid(), "Ensalada de frutas", "Picar la fruta y mezclar.");

            // Act
            await handler.Handle(new OutboxMessage<RecipeCreated>(recipeCreated), CancellationToken.None);

            // Assert
            Assert.Single(await dbContext.Recipes.ToListAsync());
        }

        [Fact]
        public async Task AddInfrastructure_ResolvesAProjectionHandlerThatNeedsALogger()
        {
            // Arrange
            using var provider = BuildProvider(useInMemoryStoredDb: true);
            using var scope = provider.CreateScope();
            var handler = scope.ServiceProvider
                .GetRequiredService<INotificationHandler<OutboxMessage<RecipeUpdated>>>();

            // Act
            await handler.Handle(new OutboxMessage<RecipeUpdated>(
                new RecipeUpdated(Guid.NewGuid(), "Ensalada", "Picar.")), CancellationToken.None);

            // Assert
            var dbContext = scope.ServiceProvider.GetRequiredService<StoredDbContext>();
            Assert.Empty(await dbContext.Recipes.ToListAsync());
        }

        private static ServiceProvider BuildProvider(bool useInMemoryStoredDb = false)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructure(BuildConfiguration());

            if (useInMemoryStoredDb)
            {
                services.RemoveAll<DbContextOptions<StoredDbContext>>();
                services.AddSingleton(new DbContextOptionsBuilder<StoredDbContext>()
                    .UseInMemoryDatabase(StoredDbContextFactory.NewDatabaseName())
                    .Options);
            }

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
