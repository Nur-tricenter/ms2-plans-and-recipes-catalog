using Catalog.Domain.Model.Recipes;
using Catalog.Domain.ValueObjects;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using DomainRecipe = Catalog.Domain.Model.Recipes.Recipe;

namespace Catalog.Infrastructure.Persistence.DomainModel.Config.Recipe
{
    public class RecipeWriteConfig : IEntityTypeConfiguration<DomainRecipe>
    {
        public void Configure(EntityTypeBuilder<DomainRecipe> builder)
        {
            builder.ToTable("Recipe", DatabaseSchemas.Write);
            builder.HasKey(x => x.Id);

            var recipeNameConverter = new ValueConverter<RecipeName, string>(
                name => name.Name,
                s => (RecipeName)s
            );

            builder.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(x => x.Name)
                .HasConversion(recipeNameConverter)
                .HasColumnName("name")
                .HasMaxLength(500);

            builder.Property(x => x.Instructions)
                .HasColumnName("instructions")
                .HasMaxLength(4000);

            builder.Property(x => x.CreatedAt).HasColumnName("createdAt");
            builder.Property(x => x.UpdatedAt).HasColumnName("updatedAt");
            builder.Property(x => x.DeletedAt).HasColumnName("deletedAt");
            builder.Property(x => x.IsDeleted).HasColumnName("isDeleted").HasDefaultValue(false);
            builder.HasQueryFilter(x => !x.IsDeleted);

            builder.Ignore(x => x.DomainEvents);
            builder.Ignore(x => x.Ingredients);

            builder.OwnsMany<RecipeIngredient>("_ingredients", ing =>
            {
                ing.ToTable("RecipeIngredient", DatabaseSchemas.Write);

                ing.Property<Guid>("Id")
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                // RecipeAssignment configures its own RecipeId as "recipeId"; this one was
                // left on the PascalCase default, so the two owned child tables of the
                // aggregate disagreed with each other as well as with everything else.
                ing.Property<Guid>("RecipeId")
                    .HasColumnName("recipeId");

                ing.Property(x => x.FoodId)
                    .HasColumnName("foodId");

                ing.OwnsOne(x => x.Portion, portion =>
                {
                    portion.Property(p => p.Quantity).HasColumnName("portionQuantity").HasPrecision(12, 4);
                });
            });
        }
    }
}