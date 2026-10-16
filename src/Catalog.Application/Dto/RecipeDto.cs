namespace Catalog.Application.Dto
{
    public class RecipeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Instructions { get; set; } = null!;
        public List<IngredientDto> Ingredients { get; set; } = new();
    }

    public class IngredientDto
    {
        public Guid FoodId { get; set; }
        public string FoodName { get; set; } = null!;
        public decimal Quantity { get; set; }
    }
}