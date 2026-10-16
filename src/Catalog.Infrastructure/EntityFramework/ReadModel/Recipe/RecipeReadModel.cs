namespace Catalog.Infrastructure.EntityFramework.ReadModel.Recipe
{
    public class RecipeReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Instructions { get; set; } = null!;
        public List<IngredientReadModel> Ingredients { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class IngredientReadModel
    {
        public Guid Id { get; set; }
        public Guid FoodId { get; set; }
        public decimal PortionQuantity { get; set; }
        public Guid RecipeId { get; set; }
        public RecipeReadModel Recipe { get; set; } = null!;
    }
}