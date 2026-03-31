using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusTicketingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingContactAndBoarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BoardingPointName",
                table: "Bookings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Bookings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "Bookings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropPointName",
                table: "Bookings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BoardingPointName",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DropPointName",
                table: "Bookings");
        }
    }
}
