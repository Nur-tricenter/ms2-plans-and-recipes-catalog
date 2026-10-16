namespace Catalog.Application.Dto
{
    public class FoodDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string UnitOfMeasure { get; set; } = null!;
        public decimal Quantity { get; set; }
        public decimal Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbs { get; set; }
        public decimal Fat { get; set; }
    }
}