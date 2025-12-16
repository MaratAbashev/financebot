using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinBot.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddedExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_expense_accounts_account_id",
                table: "expense");

            migrationBuilder.DropPrimaryKey(
                name: "pk_expense",
                table: "expense");

            migrationBuilder.RenameTable(
                name: "expense",
                newName: "expenses");

            migrationBuilder.RenameIndex(
                name: "ix_expense_account_id",
                table: "expenses",
                newName: "ix_expenses_account_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_expenses",
                table: "expenses",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_expenses_accounts_account_id",
                table: "expenses",
                column: "account_id",
                principalTable: "accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_expenses_accounts_account_id",
                table: "expenses");

            migrationBuilder.DropPrimaryKey(
                name: "pk_expenses",
                table: "expenses");

            migrationBuilder.RenameTable(
                name: "expenses",
                newName: "expense");

            migrationBuilder.RenameIndex(
                name: "ix_expenses_account_id",
                table: "expense",
                newName: "ix_expense_account_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_expense",
                table: "expense",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_expense_accounts_account_id",
                table: "expense",
                column: "account_id",
                principalTable: "accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
