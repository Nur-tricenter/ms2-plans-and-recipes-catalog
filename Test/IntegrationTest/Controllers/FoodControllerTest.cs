using Catalog.Domain.Repository.Food;
using Catalog.Infrastructure.Persistence.StoredModel;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.Food;
using Catalog.Tests.IntegrationTest.Setup;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Catalog.Tests.IntegrationTest.Controllers
{
    public class FoodControllerTest : IClassFixture<FoodControllerWebApplicationFactory>
    {
        private readonly FoodControllerWebApplicationFactory _factory;

        public FoodControllerTest(FoodControllerWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static object NewFood(string name, int unitOfMeasure = 1) => new
        {
            Name = name,
            Category = "Frutas",
            UnitOfMeasure = unitOfMeasure,
            Quantity = 100m,
            Calories = 52m,
            Protein = 0.3m,
            Carbs = 14m,
            Fat = 0.2m
        };

        [Fact]
        public async Task Create_WithValidRequest_PersistsFood()
        {
            //Arrange
            var client = _factory.CreateClient();

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/foods", NewFood("Manzana"));

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<FoodCreateResponse>();
            Assert.NotNull(body);
            Assert.True(body!.Success);
            Assert.NotEqual(Guid.Empty, body.Data);

            using var scope = _factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IFoodRepository>();
            var food = await repository.GetByIdAsync(body.Data);
            Assert.NotNull(food);
            Assert.Equal("MANZANA", food!.Name.Name);
            Assert.Equal(52m, food.BaseNutritionalInfo.Calories);
        }

        [Fact]
        public async Task Create_WithEmptyName_ReturnsBadRequest()
        {
            //Arrange
            var client = _factory.CreateClient();

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/foods", NewFood(""));

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Create_WithInvalidUnitOfMeasure_ReturnsBadRequest()
        {
            //Arrange
            var client = _factory.CreateClient();

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/foods", NewFood("Manzana", unitOfMeasure: 999));

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetById_AfterCreate_ReturnsFood()
        {
            //Arrange
            var client = _factory.CreateClient();
            var createResponse = await client.PostAsJsonAsync("/api/v1/foods", NewFood("Papa"));
            createResponse.EnsureSuccessStatusCode();
            var created = await createResponse.Content.ReadFromJsonAsync<FoodCreateResponse>();

            // Seed the read model so the query handler (which now reads from StoredDbContext) can find it
            using (var scope = _factory.Services.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IFoodRepository>();
                var food = await repository.GetByIdAsync(created!.Data);
                var readDb = scope.ServiceProvider.GetRequiredService<StoredDbContext>();
                readDb.Foods.Add(new FoodModel
                {
                    Id = food!.Id,
                    Name = food.Name.Name,
                    Category = food.Category.Name,
                    UnitOfMeasure = (int)food.UnitOfMeasure,
                    Quantity = food.BaseNutritionalInfo.Quantity,
                    Calories = food.BaseNutritionalInfo.Calories,
                    Protein = food.BaseNutritionalInfo.Protein,
                    Carbs = food.BaseNutritionalInfo.Carbs,
                    Fat = food.BaseNutritionalInfo.Fat,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });
                await readDb.SaveChangesAsync();
            }

            //Act
            var response = await client.GetAsync($"/api/v1/foods/{created!.Data}");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<FoodGetResponse>();
            Assert.NotNull(body);
            Assert.True(body!.Success);
            Assert.NotNull(body.Data);
            Assert.Equal("PAPA", body.Data.Name);
        }

        [Fact]
        public async Task GetById_WhenFoodDoesNotExist_Returns404()
        {
            //Arrange
            var client = _factory.CreateClient();

            //Act
            var response = await client.GetAsync($"/api/v1/foods/{Guid.NewGuid()}");

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetByCategory_WithSeededReadModel_ReturnsFoods()
        {
            //Arrange
            var client = _factory.CreateClient();
            using (var scope = _factory.Services.CreateScope())
            {
                var readDb = scope.ServiceProvider.GetRequiredService<StoredDbContext>();
                readDb.Foods.Add(new FoodModel
                {
                    Id = Guid.NewGuid(),
                    Name = "MANZANA",
                    Category = "FRUTAS",
                    UnitOfMeasure = 1,
                    Quantity = 100m,
                    Calories = 52m,
                    Protein = 0.3m,
                    Carbs = 14m,
                    Fat = 0.2m,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });
                await readDb.SaveChangesAsync();
            }

            //Act
            var response = await client.GetAsync("/api/v1/foods/category/FRUTAS?page=1&pageSize=10");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<FoodListResponse>();
            Assert.NotNull(body);
            Assert.True(body!.Success);
            Assert.NotNull(body.Data);
            Assert.Contains(body.Data.Items, a => a.Name == "MANZANA");
        }

        [Fact]
        public async Task List_WithSeededReadModel_ReturnsFoods()
        {
            //Arrange
            var client = _factory.CreateClient();
            using (var scope = _factory.Services.CreateScope())
            {
                var readDb = scope.ServiceProvider.GetRequiredService<StoredDbContext>();
                readDb.Foods.Add(new FoodModel
                {
                    Id = Guid.NewGuid(),
                    Name = "BANANA",
                    Category = "FRUTAS",
                    UnitOfMeasure = 1,
                    Quantity = 100m,
                    Calories = 89m,
                    Protein = 1.1m,
                    Carbs = 23m,
                    Fat = 0.3m,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });
                await readDb.SaveChangesAsync();
            }

            //Act
            var response = await client.GetAsync("/api/v1/foods?page=1&pageSize=10");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<FoodListResponse>();
            Assert.NotNull(body);
            Assert.True(body!.Success);
            Assert.NotNull(body.Data);
            Assert.Contains(body.Data.Items, a => a.Name == "BANANA");
        }

        [Fact]
        public async Task Update_WithValidRequest_UpdatesFood()
        {
            //Arrange
            var client = _factory.CreateClient();
            var createResponse = await client.PostAsJsonAsync("/api/v1/foods", NewFood("Leche"));
            createResponse.EnsureSuccessStatusCode();
            var created = await createResponse.Content.ReadFromJsonAsync<FoodCreateResponse>();

            //Act
            var updateResponse = await client.PutAsJsonAsync("/api/v1/foods", new
            {
                FoodId = created!.Data,
                Name = "Pera",
                Category = "Frutas",
                UnitOfMeasure = 1,
                Quantity = 120m,
                Calories = 60m,
                Protein = 0.4m,
                Carbs = 15m,
                Fat = 0.1m
            });

            //Assert
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
            var body = await updateResponse.Content.ReadFromJsonAsync<Envelope>();
            Assert.NotNull(body);
            Assert.True(body!.Success);

            using var scope = _factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IFoodRepository>();
            var food = await repository.GetByIdAsync(created.Data);
            Assert.NotNull(food);
            Assert.Equal("PERA", food!.Name.Name);
            Assert.Equal(60m, food.BaseNutritionalInfo.Calories);
        }

        [Fact]
        public async Task Update_WithUnknownId_Returns404()
        {
            //Arrange
            var client = _factory.CreateClient();

            //Act
            var response = await client.PutAsJsonAsync("/api/v1/foods", new
            {
                FoodId = Guid.NewGuid(),
                Name = "Pera",
                Category = "Frutas",
                UnitOfMeasure = 1,
                Quantity = 120m,
                Calories = 60m,
                Protein = 0.4m,
                Carbs = 15m,
                Fat = 0.1m
            });

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}