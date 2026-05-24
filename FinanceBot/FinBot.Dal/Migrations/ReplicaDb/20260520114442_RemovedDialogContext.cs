using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FinBot.Dal.Migrations.ReplicaDb
{
    /// <inheritdoc />
    public partial class RemovedDialogContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dialogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dialogs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    current_step = table.Column<int>(type: "integer", nullable: false),
                    dialog_name = table.Column<string>(type: "text", nullable: false),
                    dialog_storage = table.Column<string>(type: "jsonb", nullable: true),
                    prev_step = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dialogs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dialogs_user_id",
                table: "dialogs",
                column: "user_id",
                unique: true);
        }
    }
}
