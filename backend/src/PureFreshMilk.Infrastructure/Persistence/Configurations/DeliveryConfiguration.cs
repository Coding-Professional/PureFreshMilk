using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PureFreshMilk.Domain.Deliveries;
using PureFreshMilk.Domain.Recipients;

namespace PureFreshMilk.Infrastructure.Persistence.Configurations;

internal sealed class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
{
    public const string RecipientDateIndexName = "ux_deliveries_recipient_id_delivery_date";

    public void Configure(EntityTypeBuilder<Delivery> builder)
    {
        builder.ToTable("deliveries", table =>
        {
            table.HasCheckConstraint("ck_deliveries_status", "status IN ('delivered', 'missed')");
            table.HasCheckConstraint("ck_deliveries_quantity",
                "(status = 'delivered' AND delivered_ml BETWEEN 1 AND 20000) OR (status = 'missed' AND delivered_ml = 0)");
        });
        builder.HasKey(delivery => delivery.Id).HasName("pk_deliveries");
        builder.Property(delivery => delivery.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(delivery => delivery.HouseholdId).HasColumnName("household_id");
        builder.Property(delivery => delivery.RecipientId).HasColumnName("recipient_id");
        builder.Property(delivery => delivery.DeliveryDate).HasColumnName("delivery_date");
        builder.Property(delivery => delivery.Status).HasColumnName("status").HasMaxLength(16)
            .HasConversion(status => status == DeliveryStatus.Delivered ? "delivered" : "missed",
                value => value == "delivered" ? DeliveryStatus.Delivered : DeliveryStatus.Missed);
        builder.Property(delivery => delivery.DeliveredQuantity).HasColumnName("delivered_ml");
        builder.Property(delivery => delivery.CreatedAt).HasColumnName("created_at");
        builder.Property(delivery => delivery.UpdatedAt).HasColumnName("updated_at");
        builder.HasOne<Recipient>().WithMany()
            .HasForeignKey(delivery => new { delivery.HouseholdId, delivery.RecipientId })
            .HasPrincipalKey(recipient => new { recipient.HouseholdId, recipient.Id })
            .HasConstraintName("fk_deliveries_recipients_household_id_recipient_id")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(delivery => new { delivery.RecipientId, delivery.DeliveryDate })
            .IsUnique().HasDatabaseName(RecipientDateIndexName);
        builder.HasIndex(delivery => new { delivery.HouseholdId, delivery.DeliveryDate })
            .HasDatabaseName("ix_deliveries_household_id_delivery_date");
    }
}
