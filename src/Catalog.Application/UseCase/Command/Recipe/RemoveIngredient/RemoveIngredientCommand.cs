using Catalog.Application.Utils;
using MediatR;

namespace Catalog.Application.UseCase.Command.Recipe.RemoveIngredient
{
    public class RemoveIngredientCommand : IRequest<Result>
    {
        public Guid RecipeId { get; set; }
        public Guid FoodId { get; set; }
    }
}