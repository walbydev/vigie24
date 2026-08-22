using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vigie24.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEngagementMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_article_votes_ArticleId",
                table: "article_votes");

            migrationBuilder.DropIndex(
                name: "IX_article_votes_UserId_ArticleId",
                table: "article_votes");

            migrationBuilder.AddColumn<int>(
                name: "ClickCount",
                table: "articles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "article_votes",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "VoterKey",
                table: "article_votes",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_article_votes_ArticleId_VoterKey",
                table: "article_votes",
                columns: new[] { "ArticleId", "VoterKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_article_votes_UserId",
                table: "article_votes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_article_votes_ArticleId_VoterKey",
                table: "article_votes");

            migrationBuilder.DropIndex(
                name: "IX_article_votes_UserId",
                table: "article_votes");

            migrationBuilder.DropColumn(
                name: "ClickCount",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "VoterKey",
                table: "article_votes");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "article_votes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_article_votes_ArticleId",
                table: "article_votes",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_article_votes_UserId_ArticleId",
                table: "article_votes",
                columns: new[] { "UserId", "ArticleId" },
                unique: true);
        }
    }
}
