namespace Catalog.Infrastructure.Persistence.StoredModel.Entities.MealPlan
{
    public class MealPlanModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string DurationType { get; set; } = null!;
        public int MealsPerDay { get; set; }
        public List<PlanDayModel> PlanDays { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class PlanDayModel
    {
        public Guid Id { get; set; }
        public int DayNumber { get; set; }
        public Guid MealPlanId { get; set; }
        public MealPlanModel MealPlan { get; set; } = null!;
        public List<MealTimeModel> MealTimes { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class MealTimeModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public int Order { get; set; }
        public Guid PlanDayId { get; set; }
        public PlanDayModel PlanDay { get; set; } = null!;
        public List<RecipeAssignmentModel> AssignedRecipes { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class RecipeAssignmentModel
    {
        public Guid Id { get; set; }
        public Guid RecipeId { get; set; }
        public int RationQuantity { get; set; }
        public Guid MealTimeId { get; set; }
        public MealTimeModel MealTime { get; set; } = null!;
    }
}