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

            // PlanAuthorId has a private constructor taking a bare value, which EF cannot bind: it
            // sees only `PlanAuthorId(PlanAuthorId original)` from the record and reports "no
            // suitable constructor". An explicit converter is the fix.
            //
            // Nullable because the seeded catalogue has no author, so there is no value to convert
            // and no Create to route through. The guard against Guid.Empty lives in the aggregate
            // and the command, not here.
            var planAuthorIdConverter = new ValueConverter<PlanAuthorId?, Guid?>(
                author => author == null ? null : author.Value,
                value => value == null ? null : PlanAuthorId.Create(value.Value)
            );

            builder.Property(x => x.AuthorId)
                .HasConversion(planAuthorIdConverter)
                .HasColumnName("authorId");

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

            // PlanDay has no MealPlanId property, so EF discovers a shadow foreign key and
            // names both the CLR property and the column "MealPlanId". Every other column in
            // this schema is camelCase by explicit HasColumnName, and EF defaults a shadow
            // foreign key to nullable. Both defaults are wrong here: a PlanDay always belongs
            // to a MealPlan (_planDays is non-nullable), and the read side already declares
            // read.PlanDay.mealPlanId as required. Declared explicitly so the write schema
            // and the read schema describe the same relationship the same way.
            builder.HasMany(typeof(PlanDay), "_planDays")
                .WithOne()
                .HasForeignKey("MealPlanId")
                .IsRequired();

            builder.Ignore(x => x.DomainEvents);
            builder.Ignore(x => x.PlanDays);
        }

        public void Configure(EntityTypeBuilder<PlanDay> builder)
        {
            builder.ToTable("PlanDay", DatabaseSchemas.Write);
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever().HasColumnName("id");
            builder.Property(x => x.DayNumber).HasColumnName("dayNumber");

            // Same reason as the PlanDay -> MealPlan key on the MealPlan side: this is the
            // shadow foreign key EF created for the _mealTimes collection.
            builder.Property<Guid>("MealPlanId")
                .HasColumnName("mealPlanId")
                .IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnName("createdAt");
            builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt");
            builder.Property(x => x.DeletedAt).HasColumnName("deletedAt");
            builder.Property(x => x.IsDeleted).HasColumnName("isDeleted").HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);
            builder.HasMany(typeof(MealTime), "_mealTimes")
                .WithOne()
                .HasForeignKey("PlanDayId")
                .IsRequired();
            builder.Ignore(x => x.DomainEvents);
            builder.Ignore(x => x.MealTimes);
        }

        public void Configure(EntityTypeBuilder<MealTime> builder)
        {
            builder.ToTable("MealTime", DatabaseSchemas.Write);
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever().HasColumnName("id");
            builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            builder.Property(x => x.Order).HasColumnName("order");

            // A MealTime always belongs to a PlanDay (_mealTimes is non-nullable), so this
            // shadow foreign key is required. The read side agrees: read.MealTime.planDayId.
            // It lives on MealTime, not PlanDay: EF puts the foreign key on the dependent.
            builder.Property<Guid>("PlanDayId")
                .HasColumnName("planDayId")
                .IsRequired();
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

                asig.Property<Guid>("Id")
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                // The OwnsMany owner key is a shadow property too. It was already required
                // (Guid, not Guid?), but its column kept the PascalCase default while
                // recipeId and rationQuantity right below it are explicit camelCase.
                asig.Property<Guid>("MealTimeId")
                    .HasColumnName("mealTimeId");

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