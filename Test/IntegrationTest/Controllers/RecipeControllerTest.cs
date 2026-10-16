using Catalog.Domain.Repository.Recipe;
using Catalog.Infrastructure.Persistence.StoredModel;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.Recipe;
using Catalog.Tests.IntegrationTest.Setup;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Catalog.Tests.IntegrationTest.Controllers
{
    public class RecipeControllerTest : IClassFixture<RecipeControllerWebApplicationFactory>
    {
        private readonly RecipeControllerWebApplicationFactory _factory;

        public RecipeControllerTest(RecipeControllerWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static object NewRecipe() => new
        {
            Name = "Salmon al horno",
            Instructions = "Hornear 25 minutos"
        };

        [Fact]
        public async Task Create_WithValidRequest_PersistsRecipe()
        {
            //Arrange
            var client = _factory.CreateClient();

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/recipes", NewRecipe());

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<RecipeCreateResponse>();
            Assert.NotNull(body);
            Assert.True(body!.Success);
            Assert.NotEqual(Guid.Empty, body.Data);

            using var scope = _factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRecipeRepository>();
            var recipe = await repository.GetByIdAsync(body.Data);
            Assert.NotNull(recipe);
            Assert.Equal("Salmon al horno", recipe!.Name.Name);
            Assert.Empty(recipe.Ingredients);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Create_WithEmptyInstructions_ReturnsBadRequest(string instructions)
        {
            //Arrange
            var client = _factory.CreateClient();

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/recipes", new
            {
                Name = "Ensalada",
                Instructions = instructions
            });

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetById_AfterCreate_ReturnsRecipe()
        {
            //Arrange
            var client = _factory.CreateClient();
            var createResponse = await client.PostAsJsonAsync("/api/v1/recipes", NewRecipe());
            createResponse.EnsureSuccessStatusCode();
            var created = await createResponse.Content.ReadFromJsonAsync<RecipeCreateResponse>();

            //Act
            var response = await client.GetAsync($"/api/v1/recipes/{created!.Data}");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<RecipeGetResponse>();
            Assert.NotNull(body);
            Assert.True(body!.Success);
            Assert.NotNull(body.Data);
            Assert.Equal("Salmon al horno", body.Data.Name);
        }

        [Fact]
        public async Task GetById_WhenRecipeDoesNotExist_Returns404()
        {
            //Arrange
            var client = _factory.CreateClient();

            //Act
            var response = await client.GetAsync($"/api/v1/recipes/{Guid.NewGuid()}");

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AddIngredient_WithValidRequest_AddsIngredient()
        {
            //Arrange
            var client = _factory.CreateClient();
            var recipeResponse = await client.PostAsJsonAsync("/api/v1/recipes", NewRecipe());
            recipeResponse.EnsureSuccessStatusCode();
            var recipeCreated = await recipeResponse.Content.ReadFromJsonAsync<RecipeCreateResponse>();

            var foodResponse = await client.PostAsJsonAsync("/api/v1/foods", new
            {
                Name = "Manzana",
                Category = "Frutas",
                UnitOfMeasure = 1,
                Quantity = 100m,
                Calories = 52m,
                Protein = 0.3m,
                Carbs = 14m,
                Fat = 0.2m
            });
            foodResponse.EnsureSuccessStatusCode();
            var foodCreated = await foodResponse.Content.ReadFromJsonAsync<FoodCreateResponse>();

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/recipes/ingredients", new
            {
                RecipeId = recipeCreated!.Data,
                Ingredients = new[]
                {
                    new { FoodId = foodCreated!.Data, Quantity = 150 }
                }
            });

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<Envelope>();
            Assert.NotNull(body);
            Assert.True(body!.Success);

            using var scope = _factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRecipeRepository>();
            var recipe = await repository.GetByIdAsync(recipeCreated.Data);
            Assert.NotNull(recipe);
            Assert.Contains(recipe!.Ingredients, i => i.FoodId == foodCreated.Data);
        }

        [Fact]
        public async Task AddIngredient_WithUnknownFood_Returns400()
        {
            //Arrange
            var client = _factory.CreateClient();
            var recipeResponse = await client.PostAsJsonAsync("/api/v1/recipes", NewRecipe());
            recipeResponse.EnsureSuccessStatusCode();
            var recipeCreated = await recipeResponse.Content.ReadFromJsonAsync<RecipeCreateResponse>();

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/recipes/ingredients", new
            {
                RecipeId = recipeCreated!.Data,
                Ingredients = new[]
                {
                    new { FoodId = Guid.NewGuid(), Quantity = 150 }
                }
            });

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddIngredient_WhenRecipeDoesNotExist_Returns400()
        {
            //Arrange
            var client = _factory.CreateClient();

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/recipes/ingredients", new
            {
                RecipeId = Guid.NewGuid(),
                Ingredients = new[]
                {
                    new { FoodId = Guid.NewGuid(), Quantity = 150 }
                }
            });

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RemoveIngredient_WhenRecipeDoesNotExist_Returns400()
        {
            //Arrange
            var client = _factory.CreateClient();

            //Act
            var response = await client.DeleteAsync($"/api/v1/recipes/{Guid.NewGuid()}/ingredients/{Guid.NewGuid()}");

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RemoveIngredient_WithValidRequest_RemovesIngredient()
        {
            //Arrange
            var client = _factory.CreateClient();
            var recipeResponse = await client.PostAsJsonAsync("/api/v1/recipes", NewRecipe());
            recipeResponse.EnsureSuccessStatusCode();
            var recipeCreated = await recipeResponse.Content.ReadFromJsonAsync<RecipeCreateResponse>();

            var foodResponse = await client.PostAsJsonAsync("/api/v1/foods", new
            {
                Name = "Manzana",
                Category = "Frutas",
                UnitOfMeasure = 1,
                Quantity = 100m,
                Calories = 52m,
                Protein = 0.3m,
                Carbs = 14m,
                Fat = 0.2m
            });
            foodResponse.EnsureSuccessStatusCode();
            var foodCreated = await foodResponse.Content.ReadFromJsonAsync<FoodCreateResponse>();

            var addResponse = await client.PostAsJsonAsync("/api/v1/recipes/ingredients", new
            {
                RecipeId = recipeCreated!.Data,
                Ingredients = new[]
                {
                    new { FoodId = foodCreated!.Data, Quantity = 150 }
                }
            });
            addResponse.EnsureSuccessStatusCode();

            //Act
            var response = await client.DeleteAsync($"/api/v1/recipes/{recipeCreated.Data}/ingredients/{foodCreated.Data}");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<Envelope>();
            Assert.NotNull(body);
            Assert.True(body!.Success);

            using var scope = _factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRecipeRepository>();
            var recipe = await repository.GetByIdAsync(recipeCreated.Data);
            Assert.NotNull(recipe);
            Assert.DoesNotContain(recipe!.Ingredients, i => i.FoodId == foodCreated.Data);
        }

        [Fact]
        public async Task GetNutritionalInfo_WhenRecipeDoesNotExist_Returns404()
        {
            //Arrange
            var client = _factory.CreateClient();

            //Act
            var response = await client.GetAsync($"/api/v1/recipes/{Guid.NewGuid()}/nutritional-info");

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetNutritionalInfo_AfterAddIngredient_ReturnsInfo()
        {
            //Arrange
            var client = _factory.CreateClient();
            var recipeResponse = await client.PostAsJsonAsync("/api/v1/recipes", NewRecipe());
            recipeResponse.EnsureSuccessStatusCode();
            var recipeCreated = await recipeResponse.Content.ReadFromJsonAsync<RecipeCreateResponse>();

            var foodResponse = await client.PostAsJsonAsync("/api/v1/foods", new
            {
                Name = "Manzana",
                Category = "Frutas",
                UnitOfMeasure = 1,
                Quantity = 100m,
                Calories = 52m,
                Protein = 0.3m,
                Carbs = 14m,
                Fat = 0.2m
            });
            foodResponse.EnsureSuccessStatusCode();
            var foodCreated = await foodResponse.Content.ReadFromJsonAsync<FoodCreateResponse>();

            var addResponse = await client.PostAsJsonAsync("/api/v1/recipes/ingredients", new
            {
                RecipeId = recipeCreated!.Data,
                Ingredients = new[]
                {
                    new { FoodId = foodCreated!.Data, Quantity = 200 }
                }
            });
            addResponse.EnsureSuccessStatusCode();

            //Act
            var response = await client.GetAsync($"/api/v1/recipes/{recipeCreated.Data}/nutritional-info");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<NutritionalInfoResponse>();
            Assert.NotNull(body);
            Assert.True(body!.Success);
            Assert.NotNull(body.Data);
            Assert.Equal(104m, body.Data.Calories);
            Assert.Equal(0.6m, body.Data.Protein);
        }

        [Fact]
        public async Task List_WithSeededReadModel_ReturnsRecipes()
        {
            //Arrange
            var client = _factory.CreateClient();
            using (var scope = _factory.Services.CreateScope())
            {
                var readDb = scope.ServiceProvider.GetRequiredService<StoredDbContext>();
                readDb.Recipes.Add(new RecipeModel
                {
                    Id = Guid.NewGuid(),
                    Name = "Sopa de verduras",
                    Instructions = "Cocinar 20 minutos",
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });
                await readDb.SaveChangesAsync();
            }

            //Act
            var response = await client.GetAsync("/api/v1/recipes?page=1&pageSize=10");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<RecipeListResponse>();
            Assert.NotNull(body);
            Assert.True(body!.Success);
            Assert.NotNull(body.Data);
            Assert.Contains(body.Data.Items, r => r.Name == "Sopa de verduras");
        }
    }
}