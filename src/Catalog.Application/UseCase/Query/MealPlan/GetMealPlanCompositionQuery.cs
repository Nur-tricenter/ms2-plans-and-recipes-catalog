using Catalog.Application.Utils;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Catalog.Application.Abstractions;

namespace Catalog.Application.UseCase.Query.MealPlan
{
    public class GetMealPlanCompositionQuery : IQuery<Result<MealPlanCompositionDto>>
    {
        public Guid MealPlanId { get; set; }
        public GetMealPlanCompositionQuery() { }
        public GetMealPlanCompositionQuery(Guid mealPlanId) => MealPlanId = mealPlanId;
    }

    public class MealPlanCompositionDto
    {
        public Guid MealPlanId { get; set; }
        public string Name { get; set; } = null!;
        public List<PlanDayCompositionDto> Days { get; set; } = new();
    }

    public class PlanDayCompositionDto
    {
        public int DayNumber { get; set; }
        public List<MealTimeCompositionDto> MealTimes { get; set; } = new();
    }

    public class MealTimeCompositionDto
    {
        public Guid MealTimeId { get; set; }
        public string Name { get; set; } = null!;
        public int Order { get; set; }
        public List<AssignedRecipeCompositionDto> Recipes { get; set; } = new();
    }

    public class AssignedRecipeCompositionDto
    {
        public Guid RecipeId { get; set; }
        public string RecipeName { get; set; } = null!;
        public RationDto Ration { get; set; } = null!;
        public List<IngredientCompositionDto> Ingredients { get; set; } = new();
    }

    public class RationDto
    {
        public int Quantity { get; set; }
    }

    public class IngredientCompositionDto
    {
        public Guid FoodId { get; set; }
        public string FoodName { get; set; } = null!;
        public PortionDto Portion { get; set; } = null!;
    }

    public class PortionDto
    {
        public decimal Quantity { get; set; }
    }
}