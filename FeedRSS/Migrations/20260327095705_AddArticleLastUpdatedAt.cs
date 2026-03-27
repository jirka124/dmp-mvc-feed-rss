using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeedRSS.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleLastUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "Article",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUpdatedAt",
                table: "Article");
        }
    }
}
