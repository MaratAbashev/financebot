using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinBot.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddSavingAndUserRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_savings_group_id",
                table: "savings");

            migrationBuilder.AddColumn<Guid>(
                name: "saving_id",
                table: "groups",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_savings_group_id",
                table: "savings",
                column: "group_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_savings_group_id",
                table: "savings");

            migrationBuilder.DropColumn(
                name: "saving_id",
                table: "groups");

            migrationBuilder.CreateIndex(
                name: "ix_savings_group_id",
                table: "savings",
                column: "group_id");
        }
    }
}
