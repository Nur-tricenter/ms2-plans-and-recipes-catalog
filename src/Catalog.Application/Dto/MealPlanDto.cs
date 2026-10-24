using Catalog.Domain.ValueObjects;

namespace Catalog.Application.Dto
{
    public class MealPlanDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string DurationType { get; set; } = null!;
        public int TotalDays { get; set; }
        public int MealsPerDay { get; set; }
        public Guid? AuthorId { get; set; }
        public string? AuthorName { get; set; }
    }

    public class PlanDayDto
    {
        public int DayNumber { get; set; }
        public List<MealTimeDto> MealTimes { get; set; } = new();
    }

    public class MealTimeDto
    {
        public string Name { get; set; } = null!;
        public int Order { get; set; }
        public List<AssignedRecipeDto> Recipes { get; set; } = new();
    }

    public class AssignedRecipeDto
    {
        public Guid RecipeId { get; set; }
        public string RecipeName { get; set; } = null!;
        public int Ration { get; set; }
    }
}