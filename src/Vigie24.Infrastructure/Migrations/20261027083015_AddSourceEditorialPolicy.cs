using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vigie24.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceEditorialPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EditorialPolicy",
                table: "sources",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Standard");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EditorialPolicy",
                table: "sources");
        }
    }
}
