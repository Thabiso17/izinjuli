using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iDiski.Infrastructure.Migrations
{
    /// <summary>
    /// Reconciles two pieces of schema that only ever existed as hand-run SQL: the Videos table
    /// and Articles.IsPinned. Their original migrations (20260523160000_AddIsPinnedToArticle,
    /// 20260523160100_AddVideoEntity) are absent from the project, but /api/migrate/manual wrote
    /// their ids into __EFMigrationsHistory, so EF will never replay them. Everything here is
    /// guarded so it is a no-op on databases that were patched by hand and creates the objects
    /// on databases (fresh deployments, local dev, CI) that never were.
    /// </summary>
    public partial class AddVideosAndArticleIsPinned : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Articles"" ADD COLUMN IF NOT EXISTS ""IsPinned"" boolean NOT NULL DEFAULT false;
                CREATE INDEX IF NOT EXISTS ""IX_Articles_IsPinned"" ON ""Articles"" (""IsPinned"");
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""Videos"" (
                    ""Id"" uuid NOT NULL,
                    ""Title"" character varying(300) NOT NULL,
                    ""VideoUrl"" character varying(500) NOT NULL,
                    ""Description"" text,
                    ""ThumbnailUrl"" text,
                    ""Author"" character varying(100) NOT NULL,
                    ""IsPublished"" boolean NOT NULL,
                    ""PublishedAt"" timestamp with time zone,
                    ""IsPinned"" boolean NOT NULL,
                    ""ViewCount"" integer NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    ""UpdatedAt"" timestamp with time zone,
                    ""CreatedByUserId"" uuid,
                    ""UpdatedByUserId"" uuid,
                    CONSTRAINT ""PK_Videos"" PRIMARY KEY (""Id"")
                );

                CREATE INDEX IF NOT EXISTS ""IX_Videos_IsPinned"" ON ""Videos"" (""IsPinned"");
                CREATE INDEX IF NOT EXISTS ""IX_Videos_PublishedAt"" ON ""Videos"" (""PublishedAt"");
            ");

            // Databases that got Videos from /api/migrate/manual are missing the BaseEntity audit
            // columns and have UpdatedAt NOT NULL, neither of which matches the EF model.
            migrationBuilder.Sql(@"
                ALTER TABLE ""Videos"" ADD COLUMN IF NOT EXISTS ""CreatedByUserId"" uuid;
                ALTER TABLE ""Videos"" ADD COLUMN IF NOT EXISTS ""UpdatedByUserId"" uuid;
                ALTER TABLE ""Videos"" ALTER COLUMN ""UpdatedAt"" DROP NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""Videos"";");

            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ""IX_Articles_IsPinned"";
                ALTER TABLE ""Articles"" DROP COLUMN IF EXISTS ""IsPinned"";
            ");
        }
    }
}
