using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Recipients;

namespace PureFreshMilk.Infrastructure.Persistence.Configurations;

internal sealed class RecipientConfiguration : IEntityTypeConfiguration<Recipient>
{
    /// <summary>The generated column holding the lower-cased name, used to keep active names unique.</summary>
    public const string NameKey = "NameKey";

    public const string ActiveNameIndexName = "ux_recipients_household_id_name_key_active";

    public void Configure(EntityTypeBuilder<Recipient> builder)
    {
        builder.ToTable("recipients", table =>
        {
            table.HasCheckConstraint("ck_recipients_name_length", "length(btrim(name)) BETWEEN 1 AND 80");
            table.HasCheckConstraint("ck_recipients_expected_ml", "expected_ml BETWEEN 1 AND 20000");
            table.HasCheckConstraint(
                "ck_recipients_archived_after_start", "archived_from IS NULL OR archived_from > starts_on");
        });

        builder.HasKey(recipient => recipient.Id).HasName("pk_recipients");

        // The target of the composite foreign keys that child tables (deliveries) will use, so that a
        // child row can never point at a recipient of another household.
        builder.HasAlternateKey(recipient => new { recipient.HouseholdId, recipient.Id })
            .HasName("uq_recipients_household_id_id");

        builder.Property(recipient => recipient.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(recipient => recipient.HouseholdId).HasColumnName("household_id").IsRequired();
        builder.Property(recipient => recipient.Name).HasColumnName("name").HasMaxLength(80).IsRequired();
        builder.Property(recipient => recipient.ExpectedQuantity).HasColumnName("expected_ml");
        builder.Property(recipient => recipient.StartsOn).HasColumnName("starts_on");
        builder.Property(recipient => recipient.ArchivedFrom).HasColumnName("archived_from");
        builder.Property(recipient => recipient.CreatedAt).HasColumnName("created_at");
        builder.Property(recipient => recipient.UpdatedAt).HasColumnName("updated_at");

        builder.Property<string>(NameKey)
            .HasColumnName("name_key")
            .HasMaxLength(80)
            .HasComputedColumnSql("lower(name)", stored: true);

        builder.HasOne<Household>()
            .WithMany()
            .HasForeignKey(recipient => recipient.HouseholdId)
            .HasConstraintName("fk_recipients_households_household_id")
            .OnDelete(DeleteBehavior.Restrict);

        // Active names are unique within a household, ignoring case. An archived name can be reused.
        builder.HasIndex(nameof(Recipient.HouseholdId), NameKey)
            .IsUnique()
            .HasFilter("archived_from IS NULL")
            .HasDatabaseName(ActiveNameIndexName);
    }
}
