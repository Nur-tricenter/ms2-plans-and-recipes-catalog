using Nur.Tricenter.Access.Catalog;

namespace Catalog.Contracts;

/// <summary>
/// The scopes this service recognises, sourced from the platform kernel.
/// </summary>
/// <remarks>
/// The NAMES live in <c>Nur.Tricenter.Access.Catalog</c>, not here. The catalog owns what each
/// permission MEANS, but not the only copy of its name: identity has to be able to grant these to
/// a platform administrator, and it must do that without depending on the catalog. Both read the
/// vocabulary from the kernel, and neither references the other.
/// </remarks>
public static class CatalogPermissions
{
    public const string FoodRead = CatalogPermission.FoodRead;
    public const string FoodCreate = CatalogPermission.FoodCreate;
    public const string FoodEdit = CatalogPermission.FoodEdit;
    public const string FoodManage = CatalogPermission.FoodManage;

    public const string RecipeRead = CatalogPermission.RecipeRead;
    public const string RecipeCreate = CatalogPermission.RecipeCreate;
    public const string RecipeEdit = CatalogPermission.RecipeEdit;
    public const string RecipeManage = CatalogPermission.RecipeManage;

    public const string MealPlanRead = CatalogPermission.MealPlanRead;
    public const string MealPlanCreate = CatalogPermission.MealPlanCreate;
    public const string MealPlanEdit = CatalogPermission.MealPlanEdit;
    public const string MealPlanManage = CatalogPermission.MealPlanManage;

    /// <summary>
    /// Every scope this service defines, registered by <c>AddSecurity</c> to build one authorization
    /// policy per entry.
    /// </summary>
    public static IReadOnlyList<string> PermissionsList { get; } = CatalogPermission.PermissionsList;
}