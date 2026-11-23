using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PureFreshMilk.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "deliveries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    delivered_ml = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deliveries", x => x.id);
                    table.CheckConstraint("ck_deliveries_quantity", "(status = 'delivered' AND delivered_ml BETWEEN 1 AND 20000) OR (status = 'missed' AND delivered_ml = 0)");
                    table.CheckConstraint("ck_deliveries_status", "status IN ('delivered', 'missed')");
                    table.ForeignKey(
                        name: "fk_deliveries_recipients_household_id_recipient_id",
                        columns: x => new { x.household_id, x.recipient_id },
                        principalTable: "recipients",
                        principalColumns: new[] { "household_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_deliveries_household_id_delivery_date",
                table: "deliveries",
                columns: new[] { "household_id", "delivery_date" });

            migrationBuilder.CreateIndex(
                name: "IX_deliveries_household_id_recipient_id",
                table: "deliveries",
                columns: new[] { "household_id", "recipient_id" });

            migrationBuilder.CreateIndex(
                name: "ux_deliveries_recipient_id_delivery_date",
                table: "deliveries",
                columns: new[] { "recipient_id", "delivery_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deliveries");
        }
    }
}
