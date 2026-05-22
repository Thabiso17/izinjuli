using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iDiski.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaFieldsToArticleAndPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "Players",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeaturedImageUrl",
                table: "Articles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "Articles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bio",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "FeaturedImageUrl",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "Articles");
        }
    }
}
