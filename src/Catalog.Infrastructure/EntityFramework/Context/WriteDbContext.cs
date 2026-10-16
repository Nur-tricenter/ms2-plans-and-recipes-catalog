using Catalog.Domain.Model.Foods;
using Catalog.Domain.Model.MealPlans;
using Catalog.Domain.Model.Recipes;
using Catalog.Infrastructure.EntityFramework.Config.WriteConfig.Foods;
using Catalog.Infrastructure.EntityFramework.Config.WriteConfig.MealPlans;
using Catalog.Infrastructure.EntityFramework.Config.WriteConfig.Recipes;
using Microsoft.EntityFrameworkCore;
using Shared.Core;

namespace Catalog.Infrastructure.EntityFramework.Context
{
    public class WriteDbContext : DbContext
    {
        public virtual DbSet<MealPlan> MealPlans { get; set; } = null!;
        public virtual DbSet<Recipe> Recipes { get; set; } = null!;
        public virtual DbSet<Food> Foods { get; set; } = null!;

        public WriteDbContext(DbContextOptions<WriteDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var mealPlanConfig = new MealPlanWriteConfig();
            modelBuilder.ApplyConfiguration<MealPlan>(mealPlanConfig);
            modelBuilder.ApplyConfiguration<PlanDay>(mealPlanConfig);
            modelBuilder.ApplyConfiguration<MealTime>(mealPlanConfig);

            var recipeConfig = new RecipeWriteConfig();
            modelBuilder.ApplyConfiguration<Recipe>(recipeConfig);

            var foodConfig = new FoodWriteConfig();
            modelBuilder.ApplyConfiguration<Food>(foodConfig);

            modelBuilder.Ignore<DomainEvent>();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is not IAuditableEntity entity)
                    continue;

                switch (entry.State)
                {
                    case EntityState.Added:
                        entity.CreatedAt = now;
                        entity.IsDeleted = false;
                        break;

                    case EntityState.Modified:
                        entity.UpdatedAt = now;
                        break;

                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entity.IsDeleted = true;
                        entity.DeletedAt = now;
                        break;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}