using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Persistence.StoredModel.Entities;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.Food;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.MealPlan;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.Recipe;
using Catalog.Infrastructure.Persistence.StoredModel.Config.Food;
using Catalog.Infrastructure.Persistence.StoredModel.Config.MealPlan;
using Catalog.Infrastructure.Persistence.StoredModel.Config.Recipe;
using Catalog.Infrastructure.Persistence.StoredModel.Config;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.StoredModel
{
    public class StoredDbContext : DbContext, IDatabase
    {
        public virtual DbSet<FoodModel> Foods { get; set; } = null!;
        public virtual DbSet<RecipeModel> Recipes { get; set; } = null!;
        public virtual DbSet<MealPlanModel> MealPlans { get; set; } = null!;

        /// <summary>
        /// Users projected from the identity service. Not catalog-owned data: it is a projection of
        /// somebody else's events, which is what lets a meal plan name its author without the
        /// catalog ever calling the identity service.
        /// </summary>
        public virtual DbSet<PlanAuthorModel> PlanAuthors { get; set; } = null!;

        /// <summary>
        /// Messages this service has already handled, keyed by the publisher's message id. Makes
        /// cross-service consumption idempotent under at-least-once delivery.
        /// </summary>
        public virtual DbSet<InboxMessage> InboxMessages { get; set; } = null!;

        public StoredDbContext(DbContextOptions<StoredDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Anything a configuration forgets to place explicitly still lands in the read
            // schema, never in `public` where the write side would collide with it.
            modelBuilder.HasDefaultSchema(DatabaseSchemas.Read);

            var mealPlanConfig = new MealPlanReadConfig();
            modelBuilder.ApplyConfiguration<MealPlanModel>(mealPlanConfig);
            modelBuilder.ApplyConfiguration<PlanDayModel>(mealPlanConfig);
            modelBuilder.ApplyConfiguration<MealTimeModel>(mealPlanConfig);
            modelBuilder.ApplyConfiguration<RecipeAssignmentModel>(mealPlanConfig);

            var recipeConfig = new RecipeReadConfig();
            modelBuilder.ApplyConfiguration<RecipeModel>(recipeConfig);
            modelBuilder.ApplyConfiguration<IngredientModel>(recipeConfig);

            var foodConfig = new FoodReadConfig();
            modelBuilder.ApplyConfiguration<FoodModel>(foodConfig);

            modelBuilder.ApplyConfiguration<PlanAuthorModel>(new PlanAuthorReadConfig());
            modelBuilder.ApplyConfiguration<InboxMessage>(new InboxMessageConfig());
        }

        public void Migrate() => Database.Migrate();
    }
}