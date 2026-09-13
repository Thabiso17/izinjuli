using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iDiski.Infrastructure.Migrations
{
    /// <summary>
    /// Adds the archived state. Anything that has been published is archived rather than
    /// deleted from here on, so existing rows all start out un-archived.
    /// </summary>
    public partial class AddArchiveToArticleAndVideo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in new[] { "Articles", "Videos" })
            {
                migrationBuilder.AddColumn<bool>(
                    name: "IsArchived",
                    table: table,
                    type: "boolean",
                    nullable: false,
                    defaultValue: false);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in new[] { "Articles", "Videos" })
            {
                migrationBuilder.DropColumn(name: "IsArchived", table: table);
            }
        }
    }
}
