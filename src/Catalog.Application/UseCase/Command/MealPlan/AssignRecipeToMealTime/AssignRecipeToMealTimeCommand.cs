using Catalog.Application.Utils;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Catalog.Application.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace Catalog.Application.UseCase.Command.MealPlan.AssignRecipeToMealTime
{
    public class AssignRecipeToMealTimeCommand : ICommand
    {
        public Guid MealPlanId { get; set; }
        public Guid MealTimeId { get; set; }

        [MinLength(1, ErrorMessage = "At least one recipe must be assigned")]
        public List<RecipeItem> Recipes { get; set; } = new();
    }

    public class RecipeItem
    {
        public Guid RecipeId { get; set; }
        public int RationQuantity { get; set; }
    }
}