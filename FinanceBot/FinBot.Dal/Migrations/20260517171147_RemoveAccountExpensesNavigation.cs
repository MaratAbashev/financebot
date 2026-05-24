using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinBot.Dal.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAccountExpensesNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_expenses_accounts_account_id",
                table: "expenses");

            migrationBuilder.DropIndex(
                name: "ix_expenses_account_id",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "account_id",
                table: "expenses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "account_id",
                table: "expenses",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_expenses_account_id",
                table: "expenses",
                column: "account_id");

            migrationBuilder.AddForeignKey(
                name: "fk_expenses_accounts_account_id",
                table: "expenses",
                column: "account_id",
                principalTable: "accounts",
                principalColumn: "id");
        }
    }
}
