using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PureFreshMilk.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHouseholdsRecipientsAndMilkRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "households",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    time_zone_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_households", x => x.id);
                    table.CheckConstraint("ck_households_name_length", "length(btrim(name)) BETWEEN 1 AND 80");
                });

            migrationBuilder.CreateTable(
                name: "milk_rates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    price_per_litre_paise = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_milk_rates", x => x.id);
                    table.CheckConstraint("ck_milk_rates_price", "price_per_litre_paise BETWEEN 1 AND 1000000");
                    table.ForeignKey(
                        name: "fk_milk_rates_households_household_id",
                        column: x => x.household_id,
                        principalTable: "households",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "recipients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    expected_ml = table.Column<int>(type: "integer", nullable: false),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: false),
                    archived_from = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true, computedColumnSql: "lower(name)", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipients", x => x.id);
                    table.UniqueConstraint("uq_recipients_household_id_id", x => new { x.household_id, x.id });
                    table.CheckConstraint("ck_recipients_archived_after_start", "archived_from IS NULL OR archived_from > starts_on");
                    table.CheckConstraint("ck_recipients_expected_ml", "expected_ml BETWEEN 1 AND 20000");
                    table.CheckConstraint("ck_recipients_name_length", "length(btrim(name)) BETWEEN 1 AND 80");
                    table.ForeignKey(
                        name: "fk_recipients_households_household_id",
                        column: x => x.household_id,
                        principalTable: "households",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_milk_rates_household_id_effective_from",
                table: "milk_rates",
                columns: new[] { "household_id", "effective_from" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_recipients_household_id_name_key_active",
                table: "recipients",
                columns: new[] { "household_id", "name_key" },
                unique: true,
                filter: "archived_from IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "milk_rates");

            migrationBuilder.DropTable(
                name: "recipients");

            migrationBuilder.DropTable(
                name: "households");
        }
    }
}
