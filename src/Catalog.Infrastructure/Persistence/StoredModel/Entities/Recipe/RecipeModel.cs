namespace Catalog.Infrastructure.Persistence.StoredModel.Entities.Recipe
{
    public class RecipeModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Instructions { get; set; } = null!;
        public List<IngredientModel> Ingredients { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class IngredientModel
    {
        public Guid Id { get; set; }
        public Guid FoodId { get; set; }
        public decimal PortionQuantity { get; set; }
        public Guid RecipeId { get; set; }
        public RecipeModel Recipe { get; set; } = null!;
    }
}