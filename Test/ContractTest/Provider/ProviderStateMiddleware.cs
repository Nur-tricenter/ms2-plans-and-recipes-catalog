using Catalog.Infrastructure.Persistence.StoredModel;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.Food;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.Recipe;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Catalog.Tests.ContractTest.Provider
{
    /// <summary>
    /// Pact provider-state endpoint for the contract verification host.
    ///
    /// Before replaying an interaction that the consumer declared with <c>.Given("...")</c>,
    /// Pact POSTs <c>/provider-states</c> with a JSON body
    /// <c>{ "consumer": "...", "state": "...", "params": { ... } }</c>. This middleware receives
    /// that call and seeds the in-memory read model so the interaction runs against the data the
    /// contract assumes (e.g. an empty "list items" call would be meaningless).
    /// </summary>
    public class ProviderStateMiddleware
    {
        /// <summary>Fixed ids shared with the consumer examples (see Consumer/CatalogApiConsumerTests.cs).</summary>
        private static readonly Guid FoodId = Guid.Parse("3f2504e0-4f89-41d3-9a0c-0305e82c3301");
        private static readonly Guid RecipeId = Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff");

        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly RequestDelegate _next;

        public ProviderStateMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/provider-states") && HttpMethods.IsPost(context.Request.Method))
            {
                var request = await JsonSerializer.DeserializeAsync<ProviderStateRequest>(context.Request.Body, JsonOptions);

                var dbContext = context.RequestServices.GetService<StoredDbContext>();
                if (dbContext is not null)
                {
                    await ApplyStateAsync(dbContext, request?.State);
                }

                context.Response.StatusCode = StatusCodes.Status200OK;
                return;
            }

            await _next(context);
        }

        private static async Task ApplyStateAsync(StoredDbContext db, string? state)
        {
            switch (state)
            {
                case "existen alimentos registrados":
                    if (!await db.Foods.AnyAsync(a => a.Id == FoodId))
                    {
                        db.Foods.Add(new FoodModel
                        {
                            Id = FoodId,
                            Name = "Manzana",
                            Category = "Frutas",
                            UnitOfMeasure = 1,
                            Quantity = 100m,
                            Calories = 52m,
                            Protein = 0.3m,
                            Carbs = 14m,
                            Fat = 0.2m,
                            CreatedAt = DateTime.UtcNow,
                            IsDeleted = false
                        });
                    }
                    break;

                case "existen recetas registradas":
                    if (!await db.Recipes.AnyAsync(r => r.Id == RecipeId))
                    {
                        var recipe = new RecipeModel
                        {
                            Id = RecipeId,
                            Name = "Salmon al horno",
                            Instructions = "Hornear 25 minutos",
                            CreatedAt = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        recipe.Ingredients.Add(new IngredientModel
                        {
                            Id = Guid.NewGuid(),
                            RecipeId = RecipeId,
                            FoodId = FoodId,
                            PortionQuantity = 150m
                        });
                        db.Recipes.Add(recipe);
                    }
                    break;
            }

            await db.SaveChangesAsync();
        }
    }

    public class ProviderStateRequest
    {
        public string? State { get; set; }
        public Dictionary<string, object?>? Params { get; set; }
    }
}