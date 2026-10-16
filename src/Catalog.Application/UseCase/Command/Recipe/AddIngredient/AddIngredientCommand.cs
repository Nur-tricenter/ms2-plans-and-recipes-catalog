using Catalog.Application.Utils;
using Shared.Core;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace Catalog.Application.UseCase.Command.Recipe.AddIngredient
{
    public class AddIngredientCommand : IRequest<Result>
    {
        public Guid RecipeId { get; set; }

        [MinLength(1, ErrorMessage = "At least one ingredient must be added")]
        public List<IngredientItem> Ingredients { get; set; } = new();
    }

    public class IngredientItem
    {
        public Guid FoodId { get; set; }
        public decimal Quantity { get; set; }
    }
}