using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusTicketingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddPassengerGender : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Passengers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Passengers");
        }
    }
}
