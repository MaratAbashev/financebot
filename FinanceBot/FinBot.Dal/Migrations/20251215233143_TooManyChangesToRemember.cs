using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinBot.Dal.Migrations
{
    /// <inheritdoc />
    public partial class TooManyChangesToRemember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_savings_users_owner_id",
                table: "savings");

            migrationBuilder.DropIndex(
                name: "ix_savings_owner_id",
                table: "savings");

            migrationBuilder.DropColumn(
                name: "owner_id",
                table: "savings");

            migrationBuilder.DropColumn(
                name: "saving_id",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "allocation_flat",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "allocation_weight",
                table: "accounts");

            migrationBuilder.RenameColumn(
                name: "allocation_strategy",
                table: "groups",
                newName: "saving_strategy");

            migrationBuilder.AddColumn<int>(
                name: "debt_strategy",
                table: "groups",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "group_balance",
                table: "groups",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "monthly_replenishment",
                table: "groups",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "daily_allocation",
                table: "accounts",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "debt_strategy",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "group_balance",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "monthly_replenishment",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "daily_allocation",
                table: "accounts");

            migrationBuilder.RenameColumn(
                name: "saving_strategy",
                table: "groups",
                newName: "allocation_strategy");

            migrationBuilder.AddColumn<Guid>(
                name: "owner_id",
                table: "savings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "saving_id",
                table: "groups",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "allocation_flat",
                table: "accounts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "allocation_weight",
                table: "accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_savings_owner_id",
                table: "savings",
                column: "owner_id");

            migrationBuilder.AddForeignKey(
                name: "fk_savings_users_owner_id",
                table: "savings",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
