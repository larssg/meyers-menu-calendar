using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meyers.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueDateConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MenuEntries_Date",
                table: "MenuEntries");

            migrationBuilder.CreateIndex(
                name: "IX_MenuEntries_Date",
                table: "MenuEntries",
                column: "Date",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MenuEntries_Date",
                table: "MenuEntries");

            migrationBuilder.CreateIndex(
                name: "IX_MenuEntries_Date",
                table: "MenuEntries",
                column: "Date");
        }
    }
}
