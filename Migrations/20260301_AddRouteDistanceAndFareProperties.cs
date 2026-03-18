using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusTicketingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteDistanceAndFareProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Distance",
                table: "Routes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                comment: "Distance between source and destination in kilometers");

            migrationBuilder.AddColumn<int>(
                name: "EstimatedTravelTimeMinutes",
                table: "Routes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "Estimated travel time in minutes");

            migrationBuilder.AddColumn<decimal>(
                name: "BaseFare",
                table: "Routes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                comment: "Base fare for this route (per seat)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Distance",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "EstimatedTravelTimeMinutes",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "BaseFare",
                table: "Routes");
        }
    }
}
