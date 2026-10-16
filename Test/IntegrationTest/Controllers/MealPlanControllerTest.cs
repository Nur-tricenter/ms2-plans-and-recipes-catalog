using Catalog.Domain.Repository.MealPlan;
using Catalog.Infrastructure.EntityFramework.Context;
using Catalog.Infrastructure.EntityFramework.ReadModel.MealPlan;
using Catalog.Tests.IntegrationTest.Setup;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Catalog.Tests.IntegrationTest.Controllers
{
    public class MealPlanControllerTest : IClassFixture<MealPlanControllerWebApplicationFactory>
    {
        private readonly MealPlanControllerWebApplicationFactory _factory;

        public MealPlanControllerTest(MealPlanControllerWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static object NewMealPlan() => new
        {
            Name = "Dieta equilibrada",
            DurationType = "FIFTEEN_DAYS",
            MealsPerDay = 3
        };

        [Fact]
        public async Task Create_WithValidRequest_PersistsMealPlan()
        {
            //Arrange
            var client = _factory.CreateClient();

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/meal-plans", NewMealPlan());

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<MealPlanCreateResponse>();
            Assert.NotNull(body);
            Assert.True(body!.Success);
            Assert.NotEqual(Guid.Empty, body.Data);

            using var scope = _factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IMealPlanRepository>();
            var mealPlan = await repository.GetByIdAsync(body.Data);
            Assert.NotNull(mealPlan);
            Assert.Equal("Dieta equilibrada", mealPlan!.Name.Name);
            Assert.Equal(3, mealPlan.MealsPerDay);
            Assert.Equal(15, mealPlan.PlanDays.Count());
        }

        [Fact]
        public async Task Create_WithInvalidDuration_ReturnsBadRequest()
        {
            //Arrange
            var client = _factory.CreateClient();

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/meal-plans", new
            {
                Name = "Dieta semanal",
                DurationType = "WEEKLY",
                MealsPerDay = 3
            });

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddMealTime_WhenMealPlanDoesNotExist_Returns400()
        {
            //Arrange
            var client = _factory.CreateClient();

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/meal-plans/meal-times", new
            {
                MealPlanId = Guid.NewGuid(),
                DayNumber = 1,
                MealTimeType = 1
            });

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddMealTime_WithValidRequest_AddsMealTime()
        {
            //Arrange
            var client = _factory.CreateClient();
            var createResponse = await client.PostAsJsonAsync("/api/v1/meal-plans", NewMealPlan());
            createResponse.EnsureSuccessStatusCode();
            var created = await createResponse.Content.ReadFromJsonAsync<MealPlanCreateResponse>();

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/meal-plans/meal-times", new
            {
                MealPlanId = created!.Data,
                DayNumber = 1,
                MealTimeType = 1
            });

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<Envelope>();
            Assert.NotNull(body);
            Assert.True(body!.Success);

            using var scope = _factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IMealPlanRepository>();
            var mealPlan = await repository.GetByIdAsync(created.Data);
            Assert.NotNull(mealPlan);
            Assert.Single(mealPlan!.PlanDays.Single(d => d.DayNumber == 1).MealTimes);
        }

        [Fact]
        public async Task AssignRecipe_WhenMealPlanDoesNotExist_Returns400()
        {
            //Arrange
            var client = _factory.CreateClient();

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/meal-plans/assign-recipes", new
            {
                MealPlanId = Guid.NewGuid(),
                MealTimeId = Guid.NewGuid(),
                Recipes = new[]
                {
                    new { RecipeId = Guid.NewGuid(), RationQuantity = 10 }
                }
            });

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AssignRecipe_WithValidRequest_PersistsAssignment()
        {
            //Arrange
            var client = _factory.CreateClient();
            var planResponse = await client.PostAsJsonAsync("/api/v1/meal-plans", NewMealPlan());
            planResponse.EnsureSuccessStatusCode();
            var planCreated = await planResponse.Content.ReadFromJsonAsync<MealPlanCreateResponse>();

            var mealTimeResponse = await client.PostAsJsonAsync("/api/v1/meal-plans/meal-times", new
            {
                MealPlanId = planCreated!.Data,
                DayNumber = 1,
                MealTimeType = 1
            });
            mealTimeResponse.EnsureSuccessStatusCode();

            var recipeResponse = await client.PostAsJsonAsync("/api/v1/recipes", new
            {
                Name = "Ensalada",
                Instructions = "Mezclar y servir"
            });
            recipeResponse.EnsureSuccessStatusCode();
            var recipeCreated = await recipeResponse.Content.ReadFromJsonAsync<RecipeCreateResponse>();

            Guid mealTimeId;
            using (var scope = _factory.Services.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IMealPlanRepository>();
                var mealPlan = await repository.GetByIdAsync(planCreated.Data);
                mealTimeId = mealPlan!.PlanDays.Single(d => d.DayNumber == 1).MealTimes.Single().Id;
            }

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/meal-plans/assign-recipes", new
            {
                MealPlanId = planCreated.Data,
                MealTimeId = mealTimeId,
                Recipes = new[]
                {
                    new { RecipeId = recipeCreated!.Data, RationQuantity = 10 }
                }
            });

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<Envelope>();
            Assert.NotNull(body);
            Assert.True(body!.Success);

            using var verifyScope = _factory.Services.CreateScope();
            var verifyRepository = verifyScope.ServiceProvider.GetRequiredService<IMealPlanRepository>();
            var persistedPlan = await verifyRepository.GetByIdAsync(planCreated.Data);
            Assert.NotNull(persistedPlan);
            var mealTime = persistedPlan!.PlanDays.Single(d => d.DayNumber == 1).MealTimes.Single();
            Assert.Contains(mealTime.AssignedRecipes, a => a.RecipeId == recipeCreated.Data);
        }

        [Fact]
        public async Task RemoveRecipeFromMealTime_WhenMealPlanDoesNotExist_Returns400()
        {
            //Arrange
            var client = _factory.CreateClient();

            //Act
            var response = await client.DeleteAsync(
                $"/api/v1/meal-plans/{Guid.NewGuid()}/meal-times/{Guid.NewGuid()}/recipes/{Guid.NewGuid()}");

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RemoveRecipeFromMealTime_WithValidRequest_RemovesRecipe()
        {
            //Arrange
            var client = _factory.CreateClient();
            var planResponse = await client.PostAsJsonAsync("/api/v1/meal-plans", NewMealPlan());
            planResponse.EnsureSuccessStatusCode();
            var planCreated = await planResponse.Content.ReadFromJsonAsync<MealPlanCreateResponse>();

            var mealTimeResponse = await client.PostAsJsonAsync("/api/v1/meal-plans/meal-times", new
            {
                MealPlanId = planCreated!.Data,
                DayNumber = 1,
                MealTimeType = 1
            });
            mealTimeResponse.EnsureSuccessStatusCode();

            var recipeResponse = await client.PostAsJsonAsync("/api/v1/recipes", new
            {
                Name = "Ensalada",
                Instructions = "Mezclar y servir"
            });
            recipeResponse.EnsureSuccessStatusCode();
            var recipeCreated = await recipeResponse.Content.ReadFromJsonAsync<RecipeCreateResponse>();

            Guid mealTimeId;
            using (var scope = _factory.Services.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IMealPlanRepository>();
                var mealPlan = await repository.GetByIdAsync(planCreated.Data);
                mealTimeId = mealPlan!.PlanDays.Single(d => d.DayNumber == 1).MealTimes.Single().Id;
            }

            var assignResponse = await client.PostAsJsonAsync("/api/v1/meal-plans/assign-recipes", new
            {
                MealPlanId = planCreated.Data,
                MealTimeId = mealTimeId,
                Recipes = new[]
                {
                    new { RecipeId = recipeCreated!.Data, RationQuantity = 10 }
                }
            });
            assignResponse.EnsureSuccessStatusCode();

            //Act
            var response = await client.DeleteAsync(
                $"/api/v1/meal-plans/{planCreated.Data}/meal-times/{mealTimeId}/recipes/{recipeCreated.Data}");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<Envelope>();
            Assert.NotNull(body);
            Assert.True(body!.Success);

            using var verifyScope = _factory.Services.CreateScope();
            var verifyRepository = verifyScope.ServiceProvider.GetRequiredService<IMealPlanRepository>();
            var persistedPlan = await verifyRepository.GetByIdAsync(planCreated.Data);
            Assert.NotNull(persistedPlan);
            var mealTime = persistedPlan!.PlanDays.Single(d => d.DayNumber == 1).MealTimes.Single();
            Assert.DoesNotContain(mealTime.AssignedRecipes, a => a.RecipeId == recipeCreated.Data);
        }

        [Fact]
        public async Task GetComposition_AfterCreate_ReturnsComposition()
        {
            //Arrange
            var client = _factory.CreateClient();
            var createResponse = await client.PostAsJsonAsync("/api/v1/meal-plans", NewMealPlan());
            createResponse.EnsureSuccessStatusCode();
            var created = await createResponse.Content.ReadFromJsonAsync<MealPlanCreateResponse>();

            //Act
            var response = await client.GetAsync($"/api/v1/meal-plans/{created!.Data}");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<Envelope>();
            Assert.NotNull(body);
            Assert.True(body!.Success);
        }

        [Fact]
        public async Task GetComposition_WhenMealPlanDoesNotExist_Returns404()
        {
            //Arrange
            var client = _factory.CreateClient();

            //Act
            var response = await client.GetAsync($"/api/v1/meal-plans/{Guid.NewGuid()}");

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task List_WithSeededReadModel_ReturnsMealPlans()
        {
            //Arrange
            var client = _factory.CreateClient();
            using (var scope = _factory.Services.CreateScope())
            {
                var readDb = scope.ServiceProvider.GetRequiredService<ReadDbContext>();
                readDb.MealPlans.Add(new MealPlanReadModel
                {
                    Id = Guid.NewGuid(),
                    Name = "Plan deportista",
                    DurationType = "FIFTEEN_DAYS",
                    MealsPerDay = 5,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });
                await readDb.SaveChangesAsync();
            }

            //Act
            var response = await client.GetAsync("/api/v1/meal-plans");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<MealPlanListResponse>();
            Assert.NotNull(body);
            Assert.True(body!.Success);
            Assert.NotNull(body.Data);
            Assert.Contains(body.Data, p => p.Name == "Plan deportista");
        }
    }
}