using Catalog.Application.Utils;
using MediatR;
using Catalog.Application.Abstractions;

namespace Catalog.Application.UseCase.Command.Recipe.RemoveIngredient
{
    public class RemoveIngredientCommand : ICommand
    {
        public Guid RecipeId { get; set; }
        public Guid FoodId { get; set; }
    }
}