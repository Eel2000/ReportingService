using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReportingService.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdDSchedulSendHoursAndMinute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SendHour",
                table: "Schedules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SendMinute",
                table: "Schedules",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SendHour",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "SendMinute",
                table: "Schedules");
        }
    }
}
