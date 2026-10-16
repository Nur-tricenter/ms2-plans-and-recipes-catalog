using System.Text;
using Catalog.Application.IntegrationEvents;
using Catalog.WorkerService;
using Joseco.Communication.External.Contracts.Message;
using Joseco.Communication.External.Contracts.Services;
using Joseco.Communication.External.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Catalog.Tests.IntegrationTest.RabbitMq
{
    /// <summary>
    /// Proves against a REAL broker the central claim of the <c>Default</c> queue: one fanout
    /// exchange per event, all bound to the single <c>catalog.default</c> queue, which therefore
    /// accumulates the COMPLETE RECORD of everything published — one message per publish, and each
    /// body still deserializable back into the contract it was published as.
    /// </summary>
    /// <remarks>
    /// The topology is declared by the REAL <see cref="DefaultQueueRegistrar"/> hosted service, not
    /// by a test copy of its logic, so a drift between the registrar and this test is impossible.
    /// The publishes go through the REAL <see cref="IExternalPublisher"/>, so the fanout exchanges
    /// under test are the ones the publishers declare at runtime.
    /// <para>
    /// The queue and the five exchanges are deleted before AND after the run: a previous run that
    /// was killed before its <c>finally</c> would otherwise leave messages behind and make the
    /// depth assertion pass or fail for the wrong reason.
    /// </para>
    /// </remarks>
    public class DefaultQueueAccumulationTests
    {
        private static readonly TimeSpan TopologyTimeout = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan DeliveryTimeout = TimeSpan.FromSeconds(30);

        private const string QueueName = CatalogExchanges.DefaultQueue;

        private readonly ITestOutputHelper _output;

        public DefaultQueueAccumulationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [RequiresRabbitMqFact]
        public async Task PublishOneMessagePerExchange_RealBroker_TheDefaultQueueAccumulatesEveryEvent()
        {
            // Arrange
            var settings = RabbitMqTestSettings.Create();
            var exchanges = CatalogExchanges.All;

            using var host = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddLogging();
                    // Must precede AddRabbitMQ: the library uses TryAddSingleton for the settings.
                    services.AddSingleton(settings);
                    services.AddRabbitMQ(settings);
                    // The production topology owner, run for real.
                    services.AddHostedService<DefaultQueueRegistrar>();
                })
                .Build();

            await using var admin = await RabbitMqBrokerAdmin.ConnectAsync();

            // One contract per exchange, with an identifying payload so the drained bodies can be
            // matched to the contract that produced them.
            var mealPlan = new MealPlanPublished("correlation-default-queue", "catalog")
            {
                PlanId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
                Name = "Plan Default Queue",
                DurationType = "FIFTEEN_DAYS",
                TotalDays = 15,
                MealsPerDay = 3
            };
            var nutritionalInfo = new FoodNutritionalInfoUpdated("correlation-default-queue", "catalog")
            {
                FoodId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"),
                PreviousCalories = 52,
                NewCalories = 60
            };
            var recipePublished = new RecipePublished("correlation-default-queue", "catalog")
            {
                RecipeId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003"),
                Name = "Ensalada",
                Instructions = "Picar y mezclar."
            };
            var recipeUpdated = new RecipeUpdated("correlation-default-queue", "catalog")
            {
                RecipeId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000004"),
                Name = "Ensalada Caesar",
                Instructions = "Agregar pollo."
            };
            var foodPublished = new FoodPublished("correlation-default-queue", "catalog")
            {
                FoodId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000005"),
                Name = "MANZANA",
                Category = "FRUTAS",
                Quantity = 100,
                Calories = 52,
                Protein = 0.3,
                Fat = 0.2,
                Carbs = 14,
                UnitOfMeasure = "Gram"
            };

            try
            {
                // Clean slate: remove leftovers from a previous killed run.
                await admin.DeleteQueueAsync(QueueName);
                foreach (var exchange in exchanges)
                {
                    await admin.DeleteExchangeAsync(exchange);
                }

                // Act - start the real registrar and wait until it has declared the topology,
                // otherwise a fanout published before the bindings exist is dropped silently.
                await host.StartAsync();

                var declared = await admin.WaitForMessageCountAsync(QueueName, expectedMessages: 0, TopologyTimeout);
                Assert.Equal(0u, declared);
                _output.WriteLine(
                    $"[topology] registrar declared '{QueueName}' (depth={declared}) bound to {exchanges.Count} exchange(s): {string.Join(", ", exchanges)}");

                using (var scope = host.Services.CreateScope())
                {
                    var publisher = scope.ServiceProvider.GetRequiredService<IExternalPublisher>();

                    await publisher.PublishAsync(mealPlan, CatalogExchanges.MealPlanPublished, declareDestination: true);
                    await publisher.PublishAsync(nutritionalInfo, CatalogExchanges.FoodNutritionalInfoUpdated, declareDestination: true);
                    await publisher.PublishAsync(recipePublished, CatalogExchanges.RecipePublished, declareDestination: true);
                    await publisher.PublishAsync(recipeUpdated, CatalogExchanges.RecipeUpdated, declareDestination: true);
                    await publisher.PublishAsync(foodPublished, CatalogExchanges.FoodPublished, declareDestination: true);
                }

                // Assert - one message per publish, and nothing lost. On a mismatch the broker state
                // is reported, so a failure explains itself instead of showing a bare count.
                var expectedMessages = 5u;
                var depth = await admin.WaitForMessageCountAsync(QueueName, (int)expectedMessages, DeliveryTimeout);
                Assert.True(
                    depth == expectedMessages,
                    $"Expected {expectedMessages} message(s) in '{QueueName}' after publishing one per " +
                    $"exchange, observed {depth?.ToString() ?? "no such queue"}. " +
                    $"Broker state: {await admin.DescribeQueueAsync(QueueName)}");
                _output.WriteLine(
                    $"[accumulation] 1 message published per exchange -> observed depth of '{QueueName}' = {depth}");

                // Assert - every body is still a deserializable instance of the contract it was
                // published as, with its payload intact.
                var received = await DrainAsync(admin, QueueName, expectedMessages, _output);
                foreach (var (eventType, contract) in received.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                {
                    _output.WriteLine($"[drained] {eventType,-30} -> {contract.GetType().Name} (Id={contract.Id}, CorrelationId={contract.CorrelationId}, Source={contract.Source})");
                }

                var publishedPlan = Assert.IsType<MealPlanPublished>(received[mealPlan.EventType]);
                Assert.Equal(mealPlan.PlanId, publishedPlan.PlanId);
                Assert.Equal(mealPlan.Name, publishedPlan.Name);
                Assert.Equal(mealPlan.Id, publishedPlan.Id);
                Assert.Equal("catalog", publishedPlan.Source);

                var publishedNutritionalInfo = Assert.IsType<FoodNutritionalInfoUpdated>(received[nutritionalInfo.EventType]);
                Assert.Equal(nutritionalInfo.FoodId, publishedNutritionalInfo.FoodId);
                Assert.Equal(nutritionalInfo.NewCalories, publishedNutritionalInfo.NewCalories);

                var publishedRecipe = Assert.IsType<RecipePublished>(received[recipePublished.EventType]);
                Assert.Equal(recipePublished.RecipeId, publishedRecipe.RecipeId);
                Assert.Equal(recipePublished.Name, publishedRecipe.Name);
                Assert.Equal(recipePublished.Instructions, publishedRecipe.Instructions);

                var publishedRecipeUpdate = Assert.IsType<RecipeUpdated>(received[recipeUpdated.EventType]);
                Assert.Equal(recipeUpdated.RecipeId, publishedRecipeUpdate.RecipeId);
                Assert.Equal(recipeUpdated.Name, publishedRecipeUpdate.Name);
                Assert.Equal(recipeUpdated.Instructions, publishedRecipeUpdate.Instructions);

                var publishedFood = Assert.IsType<FoodPublished>(received[foodPublished.EventType]);
                Assert.Equal(foodPublished.FoodId, publishedFood.FoodId);
                Assert.Equal(foodPublished.Name, publishedFood.Name);
                Assert.Equal(foodPublished.Category, publishedFood.Category);
                Assert.Equal(foodPublished.Quantity, publishedFood.Quantity);
                Assert.Equal(foodPublished.Calories, publishedFood.Calories);
                Assert.Equal(foodPublished.Protein, publishedFood.Protein);
                Assert.Equal(foodPublished.Fat, publishedFood.Fat);
                Assert.Equal(foodPublished.Carbs, publishedFood.Carbs);
                Assert.Equal(foodPublished.UnitOfMeasure, publishedFood.UnitOfMeasure);

                Assert.Equal(5, received.Count);
            }
            finally
            {
                await host.StopAsync();
                _output.WriteLine("[cleanup] deleting the queue and the five exchanges declared by the run");
                await admin.DeleteQueueAsync(QueueName);
                foreach (var exchange in exchanges)
                {
                    await admin.DeleteExchangeAsync(exchange);
                }
            }
        }

        /// <summary>
        /// Takes every ready message, keyed by the <c>EventType</c> discriminator carried on the wire.
        /// The discriminator is what a real consumer switches on, and it is the only reliable way to
        /// pair the bodies: five messages arriving from five different fanout exchanges have no
        /// meaningful relative order.
        /// </summary>
        private static async Task<Dictionary<string, IntegrationMessage>> DrainAsync(
            RabbitMqBrokerAdmin admin,
            string queue,
            uint expectedMessages,
            ITestOutputHelper output)
        {
            var received = new Dictionary<string, IntegrationMessage>(StringComparer.Ordinal);

            for (var index = 0; index < expectedMessages; index++)
            {
                var delivery = await admin.TakeAsync(queue);
                Assert.True(
                    delivery is not null,
                    $"The {queue} queue ran dry after {received.Count} of {expectedMessages} message(s).");

                var json = Encoding.UTF8.GetString(delivery!.Body.ToArray());
                var payload = JObject.Parse(json);
                output.WriteLine($"[raw body] {json}");

                var eventType = payload["EventType"]?.Value<string>();
                Assert.False(string.IsNullOrEmpty(eventType), $"Body without an EventType discriminator: {json}");

                var contractType = ResolveContractType(eventType!);
                Assert.True(contractType is not null, $"No contract is registered for EventType '{eventType}'. Body: {json}");

                received[eventType!] = (IntegrationMessage)payload.ToObject(contractType!)!;
            }

            return received;
        }

        /// <summary>
        /// Maps a wire discriminator to the contract that owns it. Hand-maintained on purpose: it is
        /// the consumer-side switch a downstream microservice would write, so keeping it explicit
        /// means a new contract that nobody thought about is a visible gap instead of a silent
        /// fallthrough.
        /// </summary>
        private static Type? ResolveContractType(string eventType) => eventType switch
        {
            "MealPlan.Published" => typeof(MealPlanPublished),
            "Food.NutritionalInfoUpdated" => typeof(FoodNutritionalInfoUpdated),
            "Recipe.Published" => typeof(RecipePublished),
            "Recipe.Updated" => typeof(RecipeUpdated),
            "Food.Published" => typeof(FoodPublished),
            _ => null
        };
    }
}