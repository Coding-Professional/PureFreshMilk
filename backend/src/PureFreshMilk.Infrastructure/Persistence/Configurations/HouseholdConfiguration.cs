using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PureFreshMilk.Domain.Households;

namespace PureFreshMilk.Infrastructure.Persistence.Configurations;

internal sealed class HouseholdConfiguration : IEntityTypeConfiguration<Household>
{
    public void Configure(EntityTypeBuilder<Household> builder)
    {
        builder.ToTable("households", table =>
            table.HasCheckConstraint("ck_households_name_length", "length(btrim(name)) BETWEEN 1 AND 80"));

        builder.HasKey(household => household.Id).HasName("pk_households");

        builder.Property(household => household.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(household => household.Name).HasColumnName("name").HasMaxLength(80).IsRequired();
        builder.Property(household => household.TimeZoneId).HasColumnName("time_zone_id").HasMaxLength(64).IsRequired();
        builder.Property(household => household.CreatedAt).HasColumnName("created_at");
        builder.Property(household => household.UpdatedAt).HasColumnName("updated_at");
    }
}
