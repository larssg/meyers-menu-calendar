using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meyers.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuTypeSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MenuEntries_Date",
                table: "MenuEntries");

            // Create MenuTypes table first
            migrationBuilder.CreateTable(
                name: "MenuTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuTypes_Slug",
                table: "MenuTypes",
                column: "Slug",
                unique: true);

            // Insert default "Det velkendte" menu type
            migrationBuilder.InsertData(
                table: "MenuTypes",
                columns: new[] { "Name", "Slug", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { "Det velkendte", "det-velkendte", true, DateTime.UtcNow, DateTime.UtcNow });

            // Add MenuTypeId column with default value of 1 (the "Det velkendte" type we just created)
            migrationBuilder.AddColumn<int>(
                name: "MenuTypeId",
                table: "MenuEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            // Update existing records to use the default menu type
            migrationBuilder.Sql("UPDATE MenuEntries SET MenuTypeId = 1 WHERE MenuTypeId = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MenuEntries_Date_MenuTypeId",
                table: "MenuEntries",
                columns: new[] { "Date", "MenuTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuEntries_MenuTypeId",
                table: "MenuEntries",
                column: "MenuTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuEntries_MenuTypes_MenuTypeId",
                table: "MenuEntries",
                column: "MenuTypeId",
                principalTable: "MenuTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuEntries_MenuTypes_MenuTypeId",
                table: "MenuEntries");

            migrationBuilder.DropTable(
                name: "MenuTypes");

            migrationBuilder.DropIndex(
                name: "IX_MenuEntries_Date_MenuTypeId",
                table: "MenuEntries");

            migrationBuilder.DropIndex(
                name: "IX_MenuEntries_MenuTypeId",
                table: "MenuEntries");

            migrationBuilder.DropColumn(
                name: "MenuTypeId",
                table: "MenuEntries");

            migrationBuilder.CreateIndex(
                name: "IX_MenuEntries_Date",
                table: "MenuEntries",
                column: "Date",
                unique: true);
        }
    }
}
