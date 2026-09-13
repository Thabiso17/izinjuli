using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iDiski.Infrastructure.Migrations
{
    /// <summary>
    /// Gives articles and videos an optional subject: a division, a team within it, or a
    /// player within that team. Existing rows keep all three null, which reads as league-wide.
    /// </summary>
    public partial class AddContentScopeToArticleAndVideo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in new[] { "Articles", "Videos" })
            {
                migrationBuilder.AddColumn<Guid>(
                    name: "DivisionId",
                    table: table,
                    type: "uuid",
                    nullable: true);

                migrationBuilder.AddColumn<Guid>(
                    name: "TeamId",
                    table: table,
                    type: "uuid",
                    nullable: true);

                migrationBuilder.AddColumn<Guid>(
                    name: "PlayerId",
                    table: table,
                    type: "uuid",
                    nullable: true);

                migrationBuilder.CreateIndex(
                    name: $"IX_{table}_DivisionId",
                    table: table,
                    column: "DivisionId");

                migrationBuilder.CreateIndex(
                    name: $"IX_{table}_TeamId",
                    table: table,
                    column: "TeamId");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in new[] { "Articles", "Videos" })
            {
                migrationBuilder.DropIndex(name: $"IX_{table}_DivisionId", table: table);
                migrationBuilder.DropIndex(name: $"IX_{table}_TeamId", table: table);

                migrationBuilder.DropColumn(name: "DivisionId", table: table);
                migrationBuilder.DropColumn(name: "TeamId", table: table);
                migrationBuilder.DropColumn(name: "PlayerId", table: table);
            }
        }
    }
}
