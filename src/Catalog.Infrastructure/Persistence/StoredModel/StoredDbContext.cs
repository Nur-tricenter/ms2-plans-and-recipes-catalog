using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.Food;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.MealPlan;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.Recipe;
using Catalog.Infrastructure.Persistence.StoredModel.Config.Food;
using Catalog.Infrastructure.Persistence.StoredModel.Config.MealPlan;
using Catalog.Infrastructure.Persistence.StoredModel.Config.Recipe;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.StoredModel
{
    public class StoredDbContext : DbContext, IDatabase
    {
        public virtual DbSet<FoodModel> Foods { get; set; } = null!;
        public virtual DbSet<RecipeModel> Recipes { get; set; } = null!;
        public virtual DbSet<MealPlanModel> MealPlans { get; set; } = null!;

        public StoredDbContext(DbContextOptions<StoredDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var mealPlanConfig = new MealPlanReadConfig();
            modelBuilder.ApplyConfiguration<MealPlanModel>(mealPlanConfig);
            modelBuilder.ApplyConfiguration<PlanDayModel>(mealPlanConfig);
            modelBuilder.ApplyConfiguration<MealTimeModel>(mealPlanConfig);
            modelBuilder.ApplyConfiguration<AssignedRecipeModel>(mealPlanConfig);

            var recipeConfig = new RecipeReadConfig();
            modelBuilder.ApplyConfiguration<RecipeModel>(recipeConfig);
            modelBuilder.ApplyConfiguration<IngredientModel>(recipeConfig);

            var foodConfig = new FoodReadConfig();
            modelBuilder.ApplyConfiguration<FoodModel>(foodConfig);
        }

        public void Migrate() => Database.Migrate();
    }
}