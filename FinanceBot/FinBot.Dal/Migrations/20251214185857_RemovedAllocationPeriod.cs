using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinBot.Dal.Migrations
{
    /// <inheritdoc />
    public partial class RemovedAllocationPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "allocation_period_days",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "allocation_period_months",
                table: "groups");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "allocation_period_days",
                table: "groups",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "allocation_period_months",
                table: "groups",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
