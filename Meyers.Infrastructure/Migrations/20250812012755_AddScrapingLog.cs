using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meyers.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScrapingLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScrapingLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RequestSuccessful = table.Column<bool>(type: "INTEGER", nullable: false),
                    ParsingSuccessful = table.Column<bool>(type: "INTEGER", nullable: false),
                    NewMenuItemsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapingLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScrapingLogs");
        }
    }
}
