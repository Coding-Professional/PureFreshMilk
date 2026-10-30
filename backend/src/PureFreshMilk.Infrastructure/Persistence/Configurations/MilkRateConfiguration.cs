using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Rates;

namespace PureFreshMilk.Infrastructure.Persistence.Configurations;

internal sealed class MilkRateConfiguration : IEntityTypeConfiguration<MilkRate>
{
    public const string EffectiveFromIndexName = "ux_milk_rates_household_id_effective_from";

    public void Configure(EntityTypeBuilder<MilkRate> builder)
    {
        builder.ToTable("milk_rates", table =>
            table.HasCheckConstraint("ck_milk_rates_price", "price_per_litre_paise BETWEEN 1 AND 1000000"));

        builder.HasKey(rate => rate.Id).HasName("pk_milk_rates");

        builder.Property(rate => rate.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(rate => rate.HouseholdId).HasColumnName("household_id").IsRequired();
        builder.Property(rate => rate.EffectiveFrom).HasColumnName("effective_from");
        builder.Property(rate => rate.Price).HasColumnName("price_per_litre_paise");
        builder.Property(rate => rate.CreatedAt).HasColumnName("created_at");
        builder.Property(rate => rate.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<Household>()
            .WithMany()
            .HasForeignKey(rate => rate.HouseholdId)
            .HasConstraintName("fk_milk_rates_households_household_id")
            .OnDelete(DeleteBehavior.Restrict);

        // One price per household per start date.
        builder.HasIndex(rate => new { rate.HouseholdId, rate.EffectiveFrom })
            .IsUnique()
            .HasDatabaseName(EffectiveFromIndexName);
    }
}
