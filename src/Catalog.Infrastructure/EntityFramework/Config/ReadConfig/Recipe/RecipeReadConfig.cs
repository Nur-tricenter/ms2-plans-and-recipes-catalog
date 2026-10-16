using Catalog.Infrastructure.EntityFramework.ReadModel.Recipe;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.EntityFramework.Config.ReadConfig.Recipe
{
    public class RecipeReadConfig : IEntityTypeConfiguration<RecipeReadModel>,
        IEntityTypeConfiguration<IngredientReadModel>
    {
        public void Configure(EntityTypeBuilder<RecipeReadModel> builder)
        {
            builder.ToTable("Recipe");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(500);
            builder.Property(x => x.Instructions).HasColumnName("instructions").HasMaxLength(4000);
            builder.Property(x => x.CreatedAt).HasColumnName("createdAt");
            builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt");
            builder.Property(x => x.DeletedAt).HasColumnName("deletedAt");
            builder.Property(x => x.IsDeleted).HasColumnName("isDeleted").HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            builder.HasMany(x => x.Ingredients)
                .WithOne(i => i.Recipe)
                .HasForeignKey(i => i.RecipeId);
        }

        public void Configure(EntityTypeBuilder<IngredientReadModel> builder)
        {
            builder.ToTable("RecipeIngredient");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            builder.Property(x => x.FoodId).HasColumnName("foodId");
            builder.Property(x => x.PortionQuantity).HasColumnName("portionQuantity").HasPrecision(12, 4);
            builder.Property(x => x.RecipeId).HasColumnName("recipeId");
        }
    }
}