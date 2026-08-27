using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Vigie24.Infrastructure.Data;

#nullable disable

namespace Vigie24.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(Vigie24DbContext))]
    [Migration("20260827000000_AddSourceFetchFailures")]
    public partial class AddSourceFetchFailures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FetchFailureCount",
                table: "sources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DisabledReason",
                table: "sources",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisabledReason",
                table: "sources");

            migrationBuilder.DropColumn(
                name: "FetchFailureCount",
                table: "sources");
        }
    }
}
