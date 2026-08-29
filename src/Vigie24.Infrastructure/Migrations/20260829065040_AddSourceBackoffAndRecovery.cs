using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vigie24.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceBackoffAndRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DisabledAt",
                table: "sources",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastFetchAttemptAt",
                table: "sources",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisabledAt",
                table: "sources");

            migrationBuilder.DropColumn(
                name: "LastFetchAttemptAt",
                table: "sources");
        }
    }
}
