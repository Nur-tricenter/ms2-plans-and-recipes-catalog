using Catalog.Domain.ValueObjects;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using DomainFood = Catalog.Domain.Model.Foods.Food;

namespace Catalog.Infrastructure.Persistence.DomainModel.Config.Food
{
    public class FoodWriteConfig : IEntityTypeConfiguration<DomainFood>
    {
        public void Configure(EntityTypeBuilder<DomainFood> builder)
        {
            builder.ToTable("Food", DatabaseSchemas.Write);
            builder.HasKey(x => x.Id);

            var foodNameConverter = new ValueConverter<FoodName, string>(
                name => name.Name,
                s => (FoodName)s
            );

            var categoryNameConverter = new ValueConverter<CategoryName, string>(
                name => name.Name,
                s => (CategoryName)s
            );

            builder.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(x => x.Name)
                .HasConversion(foodNameConverter)
                .HasColumnName("name")
                .HasMaxLength(500);

            builder.Property(x => x.Category)
                .HasConversion(categoryNameConverter)
                .HasColumnName("category")
                .HasMaxLength(100);

            builder.Property(x => x.UnitOfMeasure)
                .HasColumnName("unitOfMeasure")
                .HasConversion<int>();

            builder.OwnsOne(x => x.BaseNutritionalInfo, info =>
            {
                info.Property(i => i.Quantity).HasColumnName("quantity").HasPrecision(12, 4);
                info.Property(i => i.Calories).HasColumnName("calories").HasPrecision(12, 4);
                info.Property(i => i.Protein).HasColumnName("protein").HasPrecision(12, 4);
                info.Property(i => i.Carbs).HasColumnName("carbs").HasPrecision(12, 4);
                info.Property(i => i.Fat).HasColumnName("fat").HasPrecision(12, 4);
            });

            builder.Property(x => x.CreatedAt).HasColumnName("createdAt");
            builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt");
            builder.Property(x => x.DeletedAt).HasColumnName("deletedAt");
            builder.Property(x => x.IsDeleted).HasColumnName("isDeleted").HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            builder.Ignore(x => x.DomainEvents);
        }
    }
}