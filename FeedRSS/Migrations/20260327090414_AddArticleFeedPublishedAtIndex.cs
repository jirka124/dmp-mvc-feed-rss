using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeedRSS.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleFeedPublishedAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Article_FeedId_PublishedAt",
                table: "Article",
                columns: new[] { "FeedId", "PublishedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Article_FeedId_PublishedAt",
                table: "Article");
        }
    }
}
