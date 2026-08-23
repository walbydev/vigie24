using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vigie24.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleTokenUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompletionTokens",
                table: "articles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PromptTokens",
                table: "articles",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletionTokens",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "PromptTokens",
                table: "articles");
        }
    }
}
