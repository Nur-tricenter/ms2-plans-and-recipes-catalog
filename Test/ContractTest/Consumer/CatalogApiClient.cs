using System.Net.Http.Json;
using System.Text.Json;

namespace Catalog.Tests.ContractTest.Consumer
{
    /// <summary>
    /// Payloads exchanged with the Catalog API (camelCase wire format, like the real WebApi).
    /// These records simulate what a real consumer - the web/mobile client or another
    /// microservice - sends and receives.
    /// </summary>
    public record FoodRequest(
        string Name,
        string Category,
        int UnitOfMeasure,
        decimal Quantity,
        decimal Calories,
        decimal Protein,
        decimal Carbs,
        decimal Fat);

    public record RecipeRequest(string Name, string Instructions);

    public record FoodItem(
        Guid Id,
        string Name,
        string Category,
        string UnitOfMeasure,
        decimal Quantity,
        decimal Calories,
        decimal Protein,
        decimal Carbs,
        decimal Fat);

    public record IngredientItem(Guid FoodId, decimal Quantity);

    public record RecipeItem(Guid Id, string Name, string Instructions, List<IngredientItem> Ingredients);

    public record PagedItems<T>(List<T> Items, int Page, int PageSize, int TotalCount, bool HasNextPage, bool HasPreviousPage);

    public record FoodListResponse(bool Success, string? Message, PagedItems<FoodItem>? Data);

    public record RecipeListResponse(bool Success, string? Message, PagedItems<RecipeItem>? Data);

    public record GuidResponse(bool Success, string? Message, Guid Data);

    /// <summary>
    /// The consumer-side HTTP client used to exercise the Catalog API. In a Pact consumer
    /// test this client is pointed at the Pact mock server, and the same methods are what a
    /// real integration layer would call against the provider.
    /// </summary>
    public class CatalogApiClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;

        public CatalogApiClient(Uri baseAddress)
        {
            _httpClient = new HttpClient { BaseAddress = baseAddress };
        }

        public async Task<GuidResponse> CreateFoodAsync(FoodRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/foods", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GuidResponse>(JsonOptions) ?? throw new InvalidOperationException("Empty response");
        }

        public async Task<FoodListResponse> GetFoodsAsync(int page = 1, int pageSize = 10)
        {
            var response = await _httpClient.GetAsync($"/api/v1/foods?page={page}&pageSize={pageSize}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<FoodListResponse>(JsonOptions) ?? throw new InvalidOperationException("Empty response");
        }

        public async Task<GuidResponse> CreateRecipeAsync(RecipeRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/recipes", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GuidResponse>(JsonOptions) ?? throw new InvalidOperationException("Empty response");
        }

        public async Task<RecipeListResponse> GetRecipesAsync(int page = 1, int pageSize = 10)
        {
            var response = await _httpClient.GetAsync($"/api/v1/recipes?page={page}&pageSize={pageSize}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<RecipeListResponse>(JsonOptions) ?? throw new InvalidOperationException("Empty response");
        }
    }
}