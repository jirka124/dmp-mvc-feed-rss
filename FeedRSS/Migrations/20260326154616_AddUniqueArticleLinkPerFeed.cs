using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeedRSS.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueArticleLinkPerFeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Article_FeedId",
                table: "Article");

            migrationBuilder.CreateIndex(
                name: "IX_Article_FeedId_Link",
                table: "Article",
                columns: new[] { "FeedId", "Link" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Article_FeedId_Link",
                table: "Article");

            migrationBuilder.CreateIndex(
                name: "IX_Article_FeedId",
                table: "Article",
                column: "FeedId");
        }
    }
}
