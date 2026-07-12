using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrlShortener.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClickTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClickCount",
                table: "ShortUrls",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAccessedAtUtc",
                table: "ShortUrls",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClickCount",
                table: "ShortUrls");

            migrationBuilder.DropColumn(
                name: "LastAccessedAtUtc",
                table: "ShortUrls");
        }
    }
}
