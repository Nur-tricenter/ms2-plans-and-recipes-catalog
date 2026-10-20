namespace Catalog.Contracts;

/// <summary>
/// Every scope the catalog microservice recognises. A service owns its own permission list; the
/// gateway aggregates the lists at runtime through service discovery rather than hardcoding them.
/// </summary>
public static class CatalogPermissions
{
    public const string FoodCreate = "catalog.food:create";
    public const string FoodEdit = "catalog.food:edit";
    public const string FoodManage = "catalog.food:manage";

    public const string RecipeCreate = "catalog.recipe:create";
    public const string RecipeEdit = "catalog.recipe:edit";
    public const string RecipeManage = "catalog.recipe:manage";

    public const string MealPlanCreate = "catalog.mealPlan:create";
    public const string MealPlanEdit = "catalog.mealPlan:edit";
    public const string MealPlanManage = "catalog.mealPlan:manage";

    /// <summary>
    /// Every scope this service defines, in one place so the gateway never has to guess and a
    /// typo cannot hide a permission.
    /// </summary>
    public static IReadOnlyList<string> PermissionsList { get; } = new[]
    {
        FoodCreate,
        FoodEdit,
        FoodManage,
        RecipeCreate,
        RecipeEdit,
        RecipeManage,
        MealPlanCreate,
        MealPlanEdit,
        MealPlanManage
    };
}