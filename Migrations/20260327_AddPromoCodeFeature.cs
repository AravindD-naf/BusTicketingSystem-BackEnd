using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusTicketingSystem.Migrations
{
    public partial class AddPromoCodeFeature : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add PromoCode table
            migrationBuilder.CreateTable(
                name: "PromoCodes",
                columns: table => new
                {
                    PromoCodeId   = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Code          = table.Column<string>(maxLength: 50, nullable: false),
                    DiscountType  = table.Column<int>(nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    MaxDiscountAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false, defaultValue: 0m),
                    MinBookingAmount  = table.Column<decimal>(type: "decimal(10,2)", nullable: false, defaultValue: 0m),
                    ValidFrom     = table.Column<DateTime>(nullable: false),
                    ValidUntil    = table.Column<DateTime>(nullable: false),
                    MaxUsageCount = table.Column<int>(nullable: false, defaultValue: 0),
                    UsedCount     = table.Column<int>(nullable: false, defaultValue: 0),
                    IsActive      = table.Column<bool>(nullable: false, defaultValue: true),
                    CreatedAt     = table.Column<DateTime>(nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_PromoCodes", x => x.PromoCodeId));

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodes_Code",
                table: "PromoCodes",
                column: "Code",
                unique: true);

            // Add promo fields to Bookings
            migrationBuilder.AddColumn<string>(
                name: "PromoCodeUsed",
                table: "Bookings",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Bookings",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            // Seed promo codes
            migrationBuilder.InsertData(
                table: "PromoCodes",
                columns: new[] { "PromoCodeId","Code","DiscountType","DiscountValue","MaxDiscountAmount","MinBookingAmount","ValidFrom","ValidUntil","MaxUsageCount","UsedCount","IsActive","CreatedAt" },
                values: new object[,]
                {
                    { 1, "FIRSTBUS20",  0, 20m,  0m,   300m, new DateTime(2026,1,1), new DateTime(2026,5,31), 0, 0, true, new DateTime(2026,1,1) },
                    { 2, "WEEKEND150",  1, 150m, 150m, 500m, new DateTime(2026,1,1), new DateTime(2026,4,30), 0, 0, true, new DateTime(2026,1,1) },
                    { 3, "MEMBER100",   1, 100m, 100m, 400m, new DateTime(2026,1,1), new DateTime(2026,4,15), 0, 0, true, new DateTime(2026,1,1) },
                    { 4, "SLEEPER15",   0, 15m,  300m, 600m, new DateTime(2026,1,1), new DateTime(2026,4,30), 0, 0, true, new DateTime(2026,1,1) },
                    { 5, "GROUP10",     0, 10m,  500m, 0m,   new DateTime(2026,1,1), new DateTime(2026,5,31), 0, 0, true, new DateTime(2026,1,1) }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PromoCodes");
            migrationBuilder.DropColumn(name: "PromoCodeUsed", table: "Bookings");
            migrationBuilder.DropColumn(name: "DiscountAmount", table: "Bookings");
        }
    }
}
