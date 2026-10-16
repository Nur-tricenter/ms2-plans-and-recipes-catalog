namespace Catalog.Infrastructure.EntityFramework.ReadModel.MealPlan
{
    public class MealPlanReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string DurationType { get; set; } = null!;
        public int MealsPerDay { get; set; }
        public List<PlanDayReadModel> PlanDays { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class PlanDayReadModel
    {
        public Guid Id { get; set; }
        public int DayNumber { get; set; }
        public Guid MealPlanId { get; set; }
        public MealPlanReadModel MealPlan { get; set; } = null!;
        public List<MealTimeReadModel> MealTimes { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class MealTimeReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public int Order { get; set; }
        public Guid PlanDayId { get; set; }
        public PlanDayReadModel PlanDay { get; set; } = null!;
        public List<AssignedRecipeReadModel> AssignedRecipes { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class AssignedRecipeReadModel
    {
        public Guid Id { get; set; }
        public Guid RecipeId { get; set; }
        public int RationQuantity { get; set; }
        public Guid MealTimeId { get; set; }
        public MealTimeReadModel MealTime { get; set; } = null!;
    }
}