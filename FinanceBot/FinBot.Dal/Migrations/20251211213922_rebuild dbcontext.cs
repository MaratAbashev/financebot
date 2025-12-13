using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinBot.Dal.Migrations
{
    /// <inheritdoc />
    public partial class rebuilddbcontext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DialogName",
                table: "Dialogs",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DialogName",
                table: "Dialogs");
        }
    }
}
