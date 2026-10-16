using Catalog.Tests.ContractTest.Consumer;
using PactNet;
using PactNet.Infrastructure.Outputters;
using PactNet.Matchers;
using System.Net;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Catalog.Tests.ContractTest.Consumer
{
    /// <summary>
    /// Consumer side of the contract (Consumer Driven Contract Testing with Pact).
    ///
    /// The consumer - the web/mobile client or another microservice - defines the interactions
    /// it expects from the Catalog API. When these tests run, Pact spins up a mock provider,
    /// the consumer's HTTP client talks to that mock and, on success, a pact file is written to
    /// <see cref="PactPaths.PactDir"/>. That file is the contract the provider must honour.
    ///
    /// Only the happy path / the contract shape is asserted here, never infrastructure errors.
    /// </summary>
    public class CatalogApiConsumerTests
    {
        private static readonly Guid FoodId = Guid.Parse("3f2504e0-4f89-41d3-9a0c-0305e82c3301");
        private static readonly Guid RecipeId = Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff");

        private readonly IPactBuilderV4 _pactBuilder;

        public CatalogApiConsumerTests(ITestOutputHelper output)
        {
            var pact = Pact.V4(PactPaths.Consumer, PactPaths.Provider, new PactConfig
            {
                PactDir = PactPaths.PactDir,
                Outputters = new IOutput[] { new XUnitOutput(output) },
                DefaultJsonSettings = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }
            });

            _pactBuilder = pact.WithHttpInteractions();
        }

        [Fact]
        public async Task CreateFood_ReturnsCreatedId()
        {
            //Arrange
            _pactBuilder
                .UponReceiving("a request to create a food")
                .WithRequest(HttpMethod.Post, "/api/v1/foods")
                .WithJsonBody(new
                {
                    name = "Manzana",
                    category = "Frutas",
                    unitOfMeasure = 1,
                    quantity = 100m,
                    calories = 52m,
                    protein = 0.3m,
                    carbs = 14m,
                    fat = 0.2m
                })
                .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    success = Match.Type(true),
                    message = Match.Type("Food created successfully"),
                    data = Match.Type(FoodId)
                });

            //Act / Assert
            await _pactBuilder.VerifyAsync(async ctx =>
            {
                var client = new CatalogApiClient(ctx.MockServerUri);

                var result = await client.CreateFoodAsync(new FoodRequest(
                    "Manzana", "Frutas", 1, 100m, 52m, 0.3m, 14m, 0.2m));

                Assert.True(result.Success);
                Assert.NotEqual(Guid.Empty, result.Data);
            });
        }

        [Fact]
        public async Task GetFoods_ReturnsPagedList()
        {
            //Arrange
            _pactBuilder
                .UponReceiving("a request to list foods")
                .Given("existen alimentos registrados")
                .WithRequest(HttpMethod.Get, "/api/v1/foods")
                .WithQuery("page", "1")
                .WithQuery("pageSize", "10")
                .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    success = Match.Type(true),
                    message = Match.Type("Food list retrieved successfully"),
                    data = new
                    {
                        items = Match.MinType(new
                        {
                            id = Match.Type(FoodId),
                            name = Match.Type("Manzana"),
                            category = Match.Type("Frutas"),
                            unitOfMeasure = Match.Type("Gram"),
                            quantity = Match.Number(100m),
                            calories = Match.Number(52m),
                            protein = Match.Number(0.3m),
                            carbs = Match.Number(14m),
                            fat = Match.Number(0.2m)
                        }, 1),
                        page = Match.Integer(1),
                        pageSize = Match.Integer(10),
                        totalCount = Match.Integer(0),
                        hasNextPage = Match.Type(false),
                        hasPreviousPage = Match.Type(false)
                    }
                });

            //Act / Assert
            await _pactBuilder.VerifyAsync(async ctx =>
            {
                var client = new CatalogApiClient(ctx.MockServerUri);

                var result = await client.GetFoodsAsync(1, 10);

                Assert.True(result.Success);
                Assert.NotNull(result.Data);
                Assert.Equal(1, result.Data!.Page);
                Assert.Equal(10, result.Data.PageSize);
            });
        }

        [Fact]
        public async Task CreateRecipe_ReturnsCreatedId()
        {
            //Arrange
            _pactBuilder
                .UponReceiving("a request to create a recipe")
                .WithRequest(HttpMethod.Post, "/api/v1/recipes")
                .WithJsonBody(new
                {
                    name = "Salmon al horno",
                    instructions = "Hornear 25 minutos"
                })
                .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    success = Match.Type(true),
                    message = Match.Type("Recipe created successfully"),
                    data = Match.Type(RecipeId)
                });

            //Act / Assert
            await _pactBuilder.VerifyAsync(async ctx =>
            {
                var client = new CatalogApiClient(ctx.MockServerUri);

                var result = await client.CreateRecipeAsync(new RecipeRequest("Salmon al horno", "Hornear 25 minutos"));

                Assert.True(result.Success);
                Assert.NotEqual(Guid.Empty, result.Data);
            });
        }

        [Fact]
        public async Task GetRecipes_ReturnsPagedList()
        {
            //Arrange
            _pactBuilder
                .UponReceiving("a request to list recipes")
                .Given("existen recetas registradas")
                .WithRequest(HttpMethod.Get, "/api/v1/recipes")
                .WithQuery("page", "1")
                .WithQuery("pageSize", "10")
                .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    success = Match.Type(true),
                    message = Match.Type("Recipe list retrieved successfully"),
                    data = new
                    {
                        items = Match.MinType(new
                        {
                            id = Match.Type(RecipeId),
                            name = Match.Type("Salmon al horno"),
                            instructions = Match.Type("Hornear 25 minutos"),
                            ingredients = Match.MinType(new
                            {
                                foodId = Match.Type(FoodId),
                                quantity = Match.Number(150m)
                            }, 1)
                        }, 1),
                        page = Match.Integer(1),
                        pageSize = Match.Integer(10),
                        totalCount = Match.Integer(0),
                        hasNextPage = Match.Type(false),
                        hasPreviousPage = Match.Type(false)
                    }
                });

            //Act / Assert
            await _pactBuilder.VerifyAsync(async ctx =>
            {
                var client = new CatalogApiClient(ctx.MockServerUri);

                var result = await client.GetRecipesAsync(1, 10);

                Assert.True(result.Success);
                Assert.NotNull(result.Data);
                Assert.Equal(1, result.Data!.Page);
                Assert.Equal(10, result.Data.PageSize);
            });
        }
    }
}