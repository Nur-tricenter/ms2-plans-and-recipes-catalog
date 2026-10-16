using Catalog.Application.Utils;
using Shared.Core;
using MediatR;

namespace Catalog.Application.UseCase.Command.Food.CreateFood
{
    public class CreateFoodCommand : IRequest<Result<Guid>>
    {
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