using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iDiski.Infrastructure.Migrations
{
    /// <summary>
    /// Unpublishing used to be the only way to take published content off the site, which left
    /// it looking like a draft: hidden from the public, but never actually retired. Archiving
    /// replaces that, so anything sitting in the old state — it has a PublishedAt, meaning it
    /// went live at some point, but IsPublished is false — becomes archived published content,
    /// which is what it was always meant to be. Restoring one from the archive then puts it
    /// back on the site rather than stranding it as a draft again.
    /// </summary>
    public partial class ArchivePreviouslyUnpublishedContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in new[] { "Articles", "Videos" })
            {
                migrationBuilder.Sql($@"
                    UPDATE ""{table}""
                    SET ""IsPublished"" = true,
                        ""IsArchived"" = true
                    WHERE ""PublishedAt"" IS NOT NULL
                      AND ""IsPublished"" = false;
                ");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The old state is indistinguishable from content archived on purpose, so this
            // cannot be reversed without guessing. Left as a no-op deliberately.
        }
    }
}
