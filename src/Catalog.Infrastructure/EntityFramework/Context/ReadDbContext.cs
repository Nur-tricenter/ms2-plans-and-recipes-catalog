using Catalog.Infrastructure.EntityFramework.ReadModel.Food;
using Catalog.Infrastructure.EntityFramework.ReadModel.MealPlan;
using Catalog.Infrastructure.EntityFramework.ReadModel.Recipe;
using Catalog.Infrastructure.EntityFramework.Config.ReadConfig.Food;
using Catalog.Infrastructure.EntityFramework.Config.ReadConfig.MealPlan;
using Catalog.Infrastructure.EntityFramework.Config.ReadConfig.Recipe;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.EntityFramework.Context
{
    public class ReadDbContext : DbContext
    {
        public virtual DbSet<FoodReadModel> Foods { get; set; } = null!;
        public virtual DbSet<RecipeReadModel> Recipes { get; set; } = null!;
        public virtual DbSet<MealPlanReadModel> MealPlans { get; set; } = null!;

        public ReadDbContext(DbContextOptions<ReadDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var mealPlanConfig = new MealPlanReadConfig();
            modelBuilder.ApplyConfiguration<MealPlanReadModel>(mealPlanConfig);
            modelBuilder.ApplyConfiguration<PlanDayReadModel>(mealPlanConfig);
            modelBuilder.ApplyConfiguration<MealTimeReadModel>(mealPlanConfig);
            modelBuilder.ApplyConfiguration<AssignedRecipeReadModel>(mealPlanConfig);

            var recipeConfig = new RecipeReadConfig();
            modelBuilder.ApplyConfiguration<RecipeReadModel>(recipeConfig);
            modelBuilder.ApplyConfiguration<IngredientReadModel>(recipeConfig);

            var foodConfig = new FoodReadConfig();
            modelBuilder.ApplyConfiguration<FoodReadModel>(foodConfig);
        }
    }
}