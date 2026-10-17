using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Persistence.StoredModel.Entities.MealPlan;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.StoredModel.Config.MealPlan
{
    public class MealPlanReadConfig : IEntityTypeConfiguration<MealPlanModel>,
        IEntityTypeConfiguration<PlanDayModel>,
        IEntityTypeConfiguration<MealTimeModel>,
        IEntityTypeConfiguration<RecipeAssignmentModel>
    {
        public void Configure(EntityTypeBuilder<MealPlanModel> builder)
        {
            builder.ToTable("MealPlan", DatabaseSchemas.Read);
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

        public void Configure(EntityTypeBuilder<PlanDayModel> builder)
        {
            builder.ToTable("PlanDay", DatabaseSchemas.Read);
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

        public void Configure(EntityTypeBuilder<MealTimeModel> builder)
        {
            builder.ToTable("MealTime", DatabaseSchemas.Read);
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
                .WithOne(assignment => assignment.MealTime)
                .HasForeignKey(ar => ar.MealTimeId);
        }

        public void Configure(EntityTypeBuilder<RecipeAssignmentModel> builder)
        {
            builder.ToTable("RecipeAssignment", DatabaseSchemas.Read);
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            builder.Property(x => x.RecipeId).HasColumnName("recipeId");
            builder.Property(x => x.RationQuantity).HasColumnName("rationQuantity");
            builder.Property(x => x.MealTimeId).HasColumnName("mealTimeId");
        }
    }
}