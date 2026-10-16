using Catalog.Application.Utils;
using Shared.Core;
using MediatR;

namespace Catalog.Application.UseCase.Command.Food.UpdateFood
{
    public class UpdateFoodCommand : IRequest<Result>
    {
        public Guid FoodId { get; set; }
        public string Name { get; set; } = null!;
        public string Category { get; set; } = null!;
        public int UnitOfMeasure { get; set; }
        public decimal Quantity { get; set; }
        public decimal Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbs { get; set; }
        public decimal Fat { get; set; }
    }
}