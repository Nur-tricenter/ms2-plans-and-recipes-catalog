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
        private static readonly Guid AlimentoId = Guid.Parse("3f2504e0-4f89-41d3-9a0c-0305e82c3301");
        private static readonly Guid RecetaId = Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff");

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
        public async Task CreateAlimento_ReturnsCreatedId()
        {
            //Arrange
            _pactBuilder
                .UponReceiving("a request to create an alimento")
                .WithRequest(HttpMethod.Post, "/api/v1/alimentos")
                .WithJsonBody(new
                {
                    nombre = "Manzana",
                    categoria = "Frutas",
                    unidadMedida = 1,
                    cantidad = 100m,
                    calorias = 52m,
                    proteinas = 0.3m,
                    carbohidratos = 14m,
                    grasas = 0.2m
                })
                .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    success = Match.Type(true),
                    message = Match.Type("Alimento creado exitosamente"),
                    data = Match.Type(AlimentoId)
                });

            //Act / Assert
            await _pactBuilder.VerifyAsync(async ctx =>
            {
                var client = new CatalogApiClient(ctx.MockServerUri);

                var result = await client.CreateAlimentoAsync(new AlimentoRequest(
                    "Manzana", "Frutas", 1, 100m, 52m, 0.3m, 14m, 0.2m));

                Assert.True(result.Success);
                Assert.NotEqual(Guid.Empty, result.Data);
            });
        }

        [Fact]
        public async Task GetAlimentos_ReturnsPagedList()
        {
            //Arrange
            _pactBuilder
                .UponReceiving("a request to list alimentos")
                .Given("existen alimentos registrados")
                .WithRequest(HttpMethod.Get, "/api/v1/alimentos")
                .WithQuery("page", "1")
                .WithQuery("pageSize", "10")
                .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    success = Match.Type(true),
                    message = Match.Type("Listado de alimentos obtenido exitosamente"),
                    data = new
                    {
                        items = Match.MinType(new
                        {
                            id = Match.Type(AlimentoId),
                            nombre = Match.Type("Manzana"),
                            categoria = Match.Type("Frutas"),
                            unidadMedida = Match.Type("Gramos"),
                            cantidad = Match.Number(100m),
                            calorias = Match.Number(52m),
                            proteinas = Match.Number(0.3m),
                            carbohidratos = Match.Number(14m),
                            grasas = Match.Number(0.2m)
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

                var result = await client.GetAlimentosAsync(1, 10);

                Assert.True(result.Success);
                Assert.NotNull(result.Data);
                Assert.Equal(1, result.Data!.Page);
                Assert.Equal(10, result.Data.PageSize);
            });
        }

        [Fact]
        public async Task CreateReceta_ReturnsCreatedId()
        {
            //Arrange
            _pactBuilder
                .UponReceiving("a request to create a receta")
                .WithRequest(HttpMethod.Post, "/api/v1/recetas")
                .WithJsonBody(new
                {
                    nombre = "Salmon al horno",
                    instrucciones = "Hornear 25 minutos"
                })
                .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    success = Match.Type(true),
                    message = Match.Type("Receta creada exitosamente"),
                    data = Match.Type(RecetaId)
                });

            //Act / Assert
            await _pactBuilder.VerifyAsync(async ctx =>
            {
                var client = new CatalogApiClient(ctx.MockServerUri);

                var result = await client.CreateRecetaAsync(new RecetaRequest("Salmon al horno", "Hornear 25 minutos"));

                Assert.True(result.Success);
                Assert.NotEqual(Guid.Empty, result.Data);
            });
        }

        [Fact]
        public async Task GetRecetas_ReturnsPagedList()
        {
            //Arrange
            _pactBuilder
                .UponReceiving("a request to list recetas")
                .Given("existen recetas registradas")
                .WithRequest(HttpMethod.Get, "/api/v1/recetas")
                .WithQuery("page", "1")
                .WithQuery("pageSize", "10")
                .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    success = Match.Type(true),
                    message = Match.Type("Listado de recetas obtenido exitosamente"),
                    data = new
                    {
                        items = Match.MinType(new
                        {
                            id = Match.Type(RecetaId),
                            nombre = Match.Type("Salmon al horno"),
                            instrucciones = Match.Type("Hornear 25 minutos"),
                            ingredientes = Match.MinType(new
                            {
                                alimentoId = Match.Type(AlimentoId),
                                cantidad = Match.Number(150m)
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

                var result = await client.GetRecetasAsync(1, 10);

                Assert.True(result.Success);
                Assert.NotNull(result.Data);
                Assert.Equal(1, result.Data!.Page);
                Assert.Equal(10, result.Data.PageSize);
            });
        }
    }
}