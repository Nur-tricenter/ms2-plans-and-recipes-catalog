using Catalog.Domain;
using Catalog.Domain.Model.MealPlans;
using Catalog.Domain.ValueObjects;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using DomainMealPlan = Catalog.Domain.Model.MealPlans.MealPlan;

namespace Catalog.Infrastructure.Persistence.DomainModel.Config.MealPlan
{
    public class MealPlanWriteConfig : IEntityTypeConfiguration<DomainMealPlan>,
        IEntityTypeConfiguration<PlanDay>,
        IEntityTypeConfiguration<MealTime>
    {
        public void Configure(EntityTypeBuilder<DomainMealPlan> builder)
        {
            builder.ToTable("MealPlan", DatabaseSchemas.Write);
            builder.HasKey(x => x.Id);

            var mealPlanNameConverter = new ValueConverter<MealPlanName, string>(
                name => name.Name,
                s => (MealPlanName)s
            );

            builder.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(x => x.Name)
                .HasConversion(mealPlanNameConverter)
                .HasColumnName("name")
                .HasMaxLength(500);

            var durationConverter = new ValueConverter<PlanDuration, string>(
                d => d.Type.ToString(),
                s => new PlanDuration(Enum.Parse<DurationType>(s))
            );

            builder.Property(x => x.Duration)
                .HasConversion(durationConverter)
                .HasColumnName("duration");

            builder.Property(x => x.MealsPerDay).HasColumnName("mealsPerDay");

            builder.Property(x => x.CreatedAt).HasColumnName("createdAt");
            builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt");
            builder.Property(x => x.DeletedAt).HasColumnName("deletedAt");
            builder.Property(x => x.IsDeleted).HasColumnName("isDeleted").HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            builder.HasMany(typeof(PlanDay), "_planDays");
            builder.Ignore(x => x.DomainEvents);
            builder.Ignore(x => x.PlanDays);
        }

        public void Configure(EntityTypeBuilder<PlanDay> builder)
        {
            builder.ToTable("PlanDay", DatabaseSchemas.Write);
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.DayNumber).HasColumnName("dayNumber");
            builder.Property(x => x.CreatedAt).HasColumnName("createdAt");
            builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt");
            builder.Property(x => x.DeletedAt).HasColumnName("deletedAt");
            builder.Property(x => x.IsDeleted).HasColumnName("isDeleted").HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);
            builder.HasMany(typeof(MealTime), "_mealTimes");
            builder.Ignore(x => x.DomainEvents);
            builder.Ignore(x => x.MealTimes);
        }

        public void Configure(EntityTypeBuilder<MealTime> builder)
        {
            builder.ToTable("MealTime", DatabaseSchemas.Write);
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            builder.Property(x => x.Order).HasColumnName("order");
            builder.Property(x => x.CreatedAt).HasColumnName("createdAt");
            builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt");
            builder.Property(x => x.DeletedAt).HasColumnName("deletedAt");
            builder.Property(x => x.IsDeleted).HasColumnName("isDeleted").HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);
            builder.Ignore(x => x.DomainEvents);
            builder.Ignore(x => x.AssignedRecipes);

            builder.OwnsMany<RecipeAssignment>("_assignedRecipes", asig =>
            {
                asig.ToTable("RecipeAssignment", DatabaseSchemas.Write);

                asig.Property<Guid>("Id").ValueGeneratedOnAdd();

                asig.Property(x => x.RecipeId)
                    .HasColumnName("recipeId");

                asig.OwnsOne(x => x.Ration, ration =>
                {
                    ration.Property(r => r.Quantity).HasColumnName("rationQuantity");
                });
            });
        }
    }
}