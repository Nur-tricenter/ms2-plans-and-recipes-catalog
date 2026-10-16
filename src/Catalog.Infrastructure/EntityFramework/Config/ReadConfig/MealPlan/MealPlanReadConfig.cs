using Catalog.Infrastructure.EntityFramework.ReadModel.MealPlan;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.EntityFramework.Config.ReadConfig.MealPlan
{
    public class MealPlanReadConfig : IEntityTypeConfiguration<MealPlanReadModel>,
        IEntityTypeConfiguration<PlanDayReadModel>,
        IEntityTypeConfiguration<MealTimeReadModel>,
        IEntityTypeConfiguration<AssignedRecipeReadModel>
    {
        public void Configure(EntityTypeBuilder<MealPlanReadModel> builder)
        {
            builder.ToTable("MealPlan");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(500);
            builder.Property(x => x.DurationType).HasColumnName("durationType");
            builder.Property(x => x.MealsPerDay).HasColumnName("mealsPerDay");
            builder.Property(x => x.CreatedAt).HasColumnName("createdAt");
            builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt");
            builder.Property(x => x.DeletedAt).HasColumnName("deletedAt");
            builder.Property(x => x.IsDeleted).HasColumnName("isDeleted").HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            builder.HasMany(x => x.PlanDays)
                .WithOne(d => d.MealPlan)
                .HasForeignKey(d => d.MealPlanId);
        }

        public void Configure(EntityTypeBuilder<PlanDayReadModel> builder)
        {
            builder.ToTable("PlanDay");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(x => x.DayNumber).HasColumnName("dayNumber");
            builder.Property(x => x.MealPlanId).HasColumnName("mealPlanId");
            builder.Property(x => x.CreatedAt).HasColumnName("createdAt");
            builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt");
            builder.Property(x => x.DeletedAt).HasColumnName("deletedAt");
            builder.Property(x => x.IsDeleted).HasColumnName("isDeleted").HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            builder.HasMany(x => x.MealTimes)
                .WithOne(mt => mt.PlanDay)
                .HasForeignKey(mt => mt.PlanDayId);
        }

        public void Configure(EntityTypeBuilder<MealTimeReadModel> builder)
        {
            builder.ToTable("MealTime");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            builder.Property(x => x.Order).HasColumnName("order");
            builder.Property(x => x.PlanDayId).HasColumnName("planDayId");
            builder.Property(x => x.CreatedAt).HasColumnName("createdAt");
            builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt");
            builder.Property(x => x.DeletedAt).HasColumnName("deletedAt");
            builder.Property(x => x.IsDeleted).HasColumnName("isDeleted").HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            builder.HasMany(x => x.AssignedRecipes)
                .WithOne(ar => ar.MealTime)
                .HasForeignKey(ar => ar.MealTimeId);
        }

        public void Configure(EntityTypeBuilder<AssignedRecipeReadModel> builder)
        {
            builder.ToTable("AssignedRecipe");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            builder.Property(x => x.RecipeId).HasColumnName("recipeId");
            builder.Property(x => x.RationQuantity).HasColumnName("rationQuantity");
            builder.Property(x => x.MealTimeId).HasColumnName("mealTimeId");
        }
    }
}