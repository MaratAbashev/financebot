using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinBot.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddedUserIdKeyToExpense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_expenses_accounts_account_id",
                table: "expenses");

            migrationBuilder.AlterColumn<int>(
                name: "account_id",
                table: "expenses",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<Guid>(
                name: "group_id",
                table: "expenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "expenses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_expenses_group_id",
                table: "expenses",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_expenses_user_id",
                table: "expenses",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_expenses_accounts_account_id",
                table: "expenses",
                column: "account_id",
                principalTable: "accounts",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_expenses_groups_group_id",
                table: "expenses",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_expenses_users_user_id",
                table: "expenses",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_expenses_accounts_account_id",
                table: "expenses");

            migrationBuilder.DropForeignKey(
                name: "fk_expenses_groups_group_id",
                table: "expenses");

            migrationBuilder.DropForeignKey(
                name: "fk_expenses_users_user_id",
                table: "expenses");

            migrationBuilder.DropIndex(
                name: "ix_expenses_group_id",
                table: "expenses");

            migrationBuilder.DropIndex(
                name: "ix_expenses_user_id",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "group_id",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "expenses");

            migrationBuilder.AlterColumn<int>(
                name: "account_id",
                table: "expenses",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_expenses_accounts_account_id",
                table: "expenses",
                column: "account_id",
                principalTable: "accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
