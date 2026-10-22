using Catalog.Infrastructure.Persistence.StoredModel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.StoredModel.Config;

public class PlanAuthorReadConfig : IEntityTypeConfiguration<PlanAuthorModel>
{
    public void Configure(EntityTypeBuilder<PlanAuthorModel> builder)
    {
        builder.ToTable("PlanAuthors");

        // The identity service's user id IS the primary key here: it is the natural key, it is
        // stable across re-projections, and inventing a surrogate would mean a mapping table to
        // keep in sync for no benefit.
        builder.HasKey(a => a.UserId);

        builder.Property(a => a.UserName).HasMaxLength(128).IsRequired();
        builder.Property(a => a.Email).HasMaxLength(256).IsRequired();
        builder.Property(a => a.IsAdministrator).IsRequired();
        builder.Property(a => a.SeenOn).IsRequired();

        // The list view sorts by name, and without this the database sorts on the clustered
        // primary key instead, which is the random guid and therefore useless as an order.
        builder.HasIndex(a => a.UserName).HasDatabaseName("IX_PlanAuthors_UserName");
    }
}