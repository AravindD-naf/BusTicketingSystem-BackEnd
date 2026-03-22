using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusTicketingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddFareToSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Fare",
                table: "Schedules",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fare",
                table: "Schedules");
        }
    }
}
