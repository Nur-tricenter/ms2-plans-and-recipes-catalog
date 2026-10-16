using Catalog.Domain.Model.Foods;
using Catalog.Domain.Model.MealPlans;
using Catalog.Domain.Model.Recipes;
using Catalog.Infrastructure.Persistence.DomainModel.Config.Food;
using Catalog.Infrastructure.Persistence.DomainModel.Config.MealPlan;
using Catalog.Infrastructure.Persistence.DomainModel.Config.Recipe;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Primitives;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.EFCore.Persistence;

namespace Catalog.Infrastructure.Persistence.DomainModel
{
    public class DomainDbContext : DbContext
    {
        public virtual DbSet<MealPlan> MealPlans { get; set; } = null!;
        public virtual DbSet<Recipe> Recipes { get; set; } = null!;
        public virtual DbSet<Food> Foods { get; set; } = null!;
        public DbSet<OutboxMessage<DomainEvent>> OutboxMessages { get; set; } = null!;

        public DomainDbContext(DbContextOptions<DomainDbContext> options) : base(options)
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

            modelBuilder.AddOutboxModel<DomainEvent>();
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
