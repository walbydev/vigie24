using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vigie24.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnalysisJson",
                table: "articles",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnalysisJson",
                table: "articles");
        }
    }
}
