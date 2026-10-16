using Catalog.Infrastructure.Persistence.StoredModel.Entities.Food;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.StoredModel.Config.Food
{
    public class FoodReadConfig : IEntityTypeConfiguration<FoodModel>
    {
        public void Configure(EntityTypeBuilder<FoodModel> builder)
        {
            builder.ToTable("Food");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(500);
            builder.Property(x => x.Category).HasColumnName("category").HasMaxLength(100);
            builder.Property(x => x.UnitOfMeasure).HasColumnName("unitOfMeasure");
            builder.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(12, 4);
            builder.Property(x => x.Calories).HasColumnName("calories").HasPrecision(12, 4);
            builder.Property(x => x.Protein).HasColumnName("protein").HasPrecision(12, 4);
            builder.Property(x => x.Carbs).HasColumnName("carbs").HasPrecision(12, 4);
            builder.Property(x => x.Fat).HasColumnName("fat").HasPrecision(12, 4);
            builder.Property(x => x.CreatedAt).HasColumnName("createdAt");
            builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt");
            builder.Property(x => x.DeletedAt).HasColumnName("deletedAt");
            builder.Property(x => x.IsDeleted).HasColumnName("isDeleted").HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}