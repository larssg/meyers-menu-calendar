using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meyers.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarDownloadLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalendarDownloadLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FeedPath = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ClientName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IpHash = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    NotModified = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarDownloadLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarDownloadLogs_Timestamp",
                table: "CalendarDownloadLogs",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarDownloadLogs");
        }
    }
}
