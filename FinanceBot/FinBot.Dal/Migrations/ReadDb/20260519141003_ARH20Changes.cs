using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FinBot.Dal.Migrations.ReadDb
{
    /// <inheritdoc />
    public partial class ARH20Changes : Migration
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

            migrationBuilder.CreateTable(
                name: "join_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_join_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_join_requests_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_join_requests_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_join_requests_group_id",
                table: "join_requests",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_join_requests_user_id_group_id",
                table: "join_requests",
                columns: new[] { "user_id", "group_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "join_requests");

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
