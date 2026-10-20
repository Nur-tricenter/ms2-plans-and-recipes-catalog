using Nur.Tricenter.Access;

namespace Catalog.Contracts;

/// <summary>
/// The catalog's route table. Declaring it here rather than inside the WebApi keeps the public
/// contract in one place and lets the gateway aggregate every service's routes through discovery.
/// </summary>
public static class CatalogRoutes
{
    private const string Base = "/api/catalog";

    public static IReadOnlyList<ScopeAccessRule> Routes { get; } = new[]
    {
        // Foods
        new ScopeAccessRule { RouteTemplate = $"{Base}/foods", HttpMethod = "GET" },
        new ScopeAccessRule { RouteTemplate = $"{Base}/foods", HttpMethod = "POST", RequiredScope = CatalogPermissions.FoodCreate },
        new ScopeAccessRule { RouteTemplate = $"{Base}/foods/{{id}}", HttpMethod = "GET" },
        new ScopeAccessRule { RouteTemplate = $"{Base}/foods/{{id}}", HttpMethod = "PUT", RequiredScope = CatalogPermissions.FoodEdit },

        // Recipes
        new ScopeAccessRule { RouteTemplate = $"{Base}/recipes", HttpMethod = "GET" },
        new ScopeAccessRule { RouteTemplate = $"{Base}/recipes", HttpMethod = "POST", RequiredScope = CatalogPermissions.RecipeCreate },
        new ScopeAccessRule { RouteTemplate = $"{Base}/recipes/{{id}}", HttpMethod = "GET" },
        new ScopeAccessRule { RouteTemplate = $"{Base}/recipes/ingredients", HttpMethod = "POST", RequiredScope = CatalogPermissions.RecipeEdit },
        new ScopeAccessRule { RouteTemplate = $"{Base}/recipes/{{recipeId}}/ingredients/{{foodId}}", HttpMethod = "DELETE", RequiredScope = CatalogPermissions.RecipeEdit },

        // Meal plans
        new ScopeAccessRule { RouteTemplate = $"{Base}/meal-plans", HttpMethod = "GET" },
        new ScopeAccessRule { RouteTemplate = $"{Base}/meal-plans", HttpMethod = "POST", RequiredScope = CatalogPermissions.MealPlanCreate },
        new ScopeAccessRule { RouteTemplate = $"{Base}/meal-plans/{{id}}", HttpMethod = "GET" },
        new ScopeAccessRule { RouteTemplate = $"{Base}/meal-plans/meal-times", HttpMethod = "POST", RequiredScope = CatalogPermissions.MealPlanEdit },
        new ScopeAccessRule { RouteTemplate = $"{Base}/meal-plans/assign-recipes", HttpMethod = "POST", RequiredScope = CatalogPermissions.MealPlanEdit },
        new ScopeAccessRule { RouteTemplate = $"{Base}/meal-plans/{{planId}}/meal-times/{{mealTimeId}}/recipes/{{recipeId}}", HttpMethod = "DELETE", RequiredScope = CatalogPermissions.MealPlanEdit }
    };
}