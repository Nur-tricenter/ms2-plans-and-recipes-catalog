using Catalog.Application.Utils;
using MediatR;

namespace Catalog.Application.UseCase.Command.MealPlan.RemoveRecipeFromMealTime
{
    public class RemoveRecipeFromMealTimeCommand : IRequest<Result>
    {
        public Guid MealPlanId { get; set; }
        public Guid MealTimeId { get; set; }
        public Guid RecipeId { get; set; }
    }
}