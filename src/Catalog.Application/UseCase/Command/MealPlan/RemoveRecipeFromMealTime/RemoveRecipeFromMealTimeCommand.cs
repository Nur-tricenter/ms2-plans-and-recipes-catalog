using Catalog.Application.Utils;
using MediatR;
using Catalog.Application.Abstractions;

namespace Catalog.Application.UseCase.Command.MealPlan.RemoveRecipeFromMealTime
{
    public class RemoveRecipeFromMealTimeCommand : ICommand
    {
        public Guid MealPlanId { get; set; }
        public Guid MealTimeId { get; set; }
        public Guid RecipeId { get; set; }
    }
}