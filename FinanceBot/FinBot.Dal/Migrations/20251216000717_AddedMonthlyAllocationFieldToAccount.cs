using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinBot.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddedMonthlyAllocationFieldToAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "monthly_allocation",
                table: "accounts",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "monthly_allocation",
                table: "accounts");
        }
    }
}
