namespace Catalog.Tests.IntegrationTest.Setup
{
    public class Envelope
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }
    }

    public class FoodCreateResponse : Envelope
    {
        public Guid Data { get; set; }
    }

    public class FoodData
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Category { get; set; } = null!;
    }

    public class FoodGetResponse : Envelope
    {
        public FoodData? Data { get; set; }
    }

    public class FoodPage
    {
        public List<FoodData> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    public class FoodListResponse : Envelope
    {
        public FoodPage? Data { get; set; }
    }

    public class RecipeCreateResponse : Envelope
    {
        public Guid Data { get; set; }
    }

    public class RecipeData
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class RecipeGetResponse : Envelope
    {
        public RecipeData? Data { get; set; }
    }

    public class RecipePage
    {
        public List<RecipeData> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    public class RecipeListResponse : Envelope
    {
        public RecipePage? Data { get; set; }
    }

    public class MealPlanCreateResponse : Envelope
    {
        public Guid Data { get; set; }
    }

    public class MealPlanData
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class MealPlanListResponse : Envelope
    {
        public List<MealPlanData>? Data { get; set; }
    }

    public class NutritionalInfoData
    {
        public decimal Quantity { get; set; }
        public decimal Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbs { get; set; }
        public decimal Fat { get; set; }
    }

    public class NutritionalInfoResponse : Envelope
    {
        public NutritionalInfoData? Data { get; set; }
    }
}