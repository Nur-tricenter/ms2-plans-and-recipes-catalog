using System.Net.Http.Json;
using System.Text.Json;

namespace Catalog.Tests.ContractTest.Consumer
{
    /// <summary>
    /// Payloads exchanged with the Catalog API (camelCase wire format, like the real WebApi).
    /// These records simulate what a real consumer - the web/mobile client or another
    /// microservice - sends and receives.
    /// </summary>
    public record AlimentoRequest(
        string Nombre,
        string Categoria,
        int UnidadMedida,
        decimal Cantidad,
        decimal Calorias,
        decimal Proteinas,
        decimal Carbohidratos,
        decimal Grasas);

    public record RecetaRequest(string Nombre, string Instrucciones);

    public record AlimentoItem(
        Guid Id,
        string Nombre,
        string Categoria,
        string UnidadMedida,
        decimal Cantidad,
        decimal Calorias,
        decimal Proteinas,
        decimal Carbohidratos,
        decimal Grasas);

    public record IngredienteItem(Guid AlimentoId, decimal Cantidad);

    public record RecetaItem(Guid Id, string Nombre, string Instrucciones, List<IngredienteItem> Ingredientes);

    public record PagedItems<T>(List<T> Items, int Page, int PageSize, int TotalCount, bool HasNextPage, bool HasPreviousPage);

    public record AlimentoListResponse(bool Success, string? Message, PagedItems<AlimentoItem>? Data);

    public record RecetaListResponse(bool Success, string? Message, PagedItems<RecetaItem>? Data);

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

        public async Task<GuidResponse> CreateAlimentoAsync(AlimentoRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/alimentos", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GuidResponse>(JsonOptions) ?? throw new InvalidOperationException("Respuesta vacía");
        }

        public async Task<AlimentoListResponse> GetAlimentosAsync(int page = 1, int pageSize = 10)
        {
            var response = await _httpClient.GetAsync($"/api/v1/alimentos?page={page}&pageSize={pageSize}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AlimentoListResponse>(JsonOptions) ?? throw new InvalidOperationException("Respuesta vacía");
        }

        public async Task<GuidResponse> CreateRecetaAsync(RecetaRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/recetas", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GuidResponse>(JsonOptions) ?? throw new InvalidOperationException("Respuesta vacía");
        }

        public async Task<RecetaListResponse> GetRecetasAsync(int page = 1, int pageSize = 10)
        {
            var response = await _httpClient.GetAsync($"/api/v1/recetas?page={page}&pageSize={pageSize}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<RecetaListResponse>(JsonOptions) ?? throw new InvalidOperationException("Respuesta vacía");
        }
    }
}