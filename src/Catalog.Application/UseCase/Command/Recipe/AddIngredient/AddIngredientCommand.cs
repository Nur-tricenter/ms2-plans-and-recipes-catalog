using Catalog.Application.Utils;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Catalog.Application.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace Catalog.Application.UseCase.Command.Recipe.AddIngredient
{
    public class AddIngredientCommand : ICommand
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