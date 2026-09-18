using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using iDiski.Infrastructure.Persistence;

#nullable disable

namespace iDiski.Infrastructure.Migrations;

/// <summary>
/// Competitions stop belonging to divisions.
///
/// A division is a collection of teams — Division 1, Division 2, Division 3 — and those teams
/// go and contest competitions. A competition is not something a division owns: a cup played
/// by clubs from all three sits in none of them, and a league is simply a competition whose
/// entrants happen to be one division's clubs. Keeping a DivisionId on a competition kept the
/// two readable as the same thing, which is what they are not.
///
/// So the link goes, and two things it was carrying need somewhere to live first:
///
///   Gender was read off the division running the competition, and it is what the entry list
///   is checked against — a women's club cannot be entered into a boys competition. It moves
///   onto the competition itself, backfilled from the division before that division is
///   forgotten. AgeGroup comes along for description, though it has never been enforced.
///
///   A fixture's division was copied from the competition's. Nothing can derive one now, and
///   for a cup tie between a Division 1 club and a Division 3 club there was never an honest
///   answer anyway. It goes, and what read it now asks about the two clubs instead.
///
/// Order matters throughout: backfill before dropping, and widen the short-code index only
/// once nothing can clash inside it.
/// </summary>
/// <remarks>
/// The id is declared here rather than in a scaffolded Designer file, as with the migrations
/// before it. The attribute is what EF reads to identify and order a migration, and every
/// operation below is raw SQL that needs no generated model.
/// </remarks>
[DbContext(typeof(LeagueDbContext))]
[Migration("20260918120000_CompetitionsStandAlone")]
public partial class CompetitionsStandAlone : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── Gender and age move onto the competition ──────────────────────────
        migrationBuilder.Sql(@"
            ALTER TABLE ""Competitions""
                ADD COLUMN IF NOT EXISTS ""Gender"" character varying(20) NULL,
                ADD COLUMN IF NOT EXISTS ""AgeGroup"" character varying(20) NULL;
        ");

        // Taken from the division that was running it, while that is still knowable. Only
        // where the column exists: a database created after this migration never had one.
        migrationBuilder.Sql(@"
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_name = 'Competitions' AND column_name = 'DivisionId'
                ) THEN
                    EXECUTE '
                        UPDATE ""Competitions"" c
                        SET ""Gender""   = COALESCE(c.""Gender"", d.""Gender""),
                            ""AgeGroup"" = COALESCE(c.""AgeGroup"", d.""AgeGroup"")
                        FROM ""Divisions"" d
                        WHERE d.""Id"" = c.""DivisionId""';
                END IF;
            END $$;
        ");

        // Anything the backfill could not reach — a competition whose division had gone —
        // gets the default rather than a null the model cannot express.
        migrationBuilder.Sql(@"
            UPDATE ""Competitions"" SET ""Gender"" = 'Male' WHERE ""Gender"" IS NULL;

            ALTER TABLE ""Competitions"" ALTER COLUMN ""Gender"" SET NOT NULL;
        ");

        // ── A short code is unique within its season now ──────────────────────
        //
        // It used to be unique within a division and season, so two divisions could each call
        // theirs "LGE". Without a division there is no smaller scope, and two competitions
        // that shared a code in the same season would collide — so make them not.
        migrationBuilder.Sql(@"
            DO $$
            DECLARE
                clash RECORD;
                suffix integer;
            BEGIN
                FOR clash IN
                    SELECT ""Id"", ""ShortCode"", ""Season"",
                           ROW_NUMBER() OVER (
                               PARTITION BY ""Season"", ""ShortCode"" ORDER BY ""CreatedAt"", ""Id""
                           ) AS n
                    FROM ""Competitions""
                LOOP
                    IF clash.n > 1 THEN
                        suffix := clash.n;
                        UPDATE ""Competitions""
                        SET ""ShortCode"" = LEFT(clash.""ShortCode"", 17) || '-' || suffix
                        WHERE ""Id"" = clash.""Id"";
                    END IF;
                END LOOP;
            END $$;
        ");

        migrationBuilder.Sql(@"
            DROP INDEX IF EXISTS ""IX_Competitions_DivisionId_Season_ShortCode"";

            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Competitions_Season_ShortCode""
            ON ""Competitions"" (""Season"", ""ShortCode"");
        ");

        // ── The competition's division link goes ──────────────────────────────
        migrationBuilder.Sql(@"
            ALTER TABLE ""Competitions""
                DROP CONSTRAINT IF EXISTS ""FK_Competitions_Divisions_DivisionId"";

            ALTER TABLE ""Competitions"" DROP COLUMN IF EXISTS ""DivisionId"";
        ");

        // ── And a fixture's ───────────────────────────────────────────────────
        migrationBuilder.Sql(@"
            ALTER TABLE ""MatchResults""
                DROP CONSTRAINT IF EXISTS ""FK_MatchResults_Divisions_DivisionId"";

            DROP INDEX IF EXISTS ""IX_MatchResults_DivisionId"";
            DROP INDEX IF EXISTS ""IX_MatchResults_DivisionId_Season_Stage"";

            ALTER TABLE ""MatchResults"" DROP COLUMN IF EXISTS ""DivisionId"";
        ");

        // The rounds of a knockout are still read one at a time; they are found by competition
        // now, which is the only thing a fixture belongs to.
        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS ""IX_MatchResults_CompetitionId_Season_Stage""
            ON ""MatchResults"" (""CompetitionId"", ""Season"", ""Stage"");
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The columns come back empty. Which division ran a competition, and which a fixture
        // was played in, are not recoverable once dropped — and for a competition contested
        // across three divisions there was no single answer to restore.
        migrationBuilder.Sql(@"
            DROP INDEX IF EXISTS ""IX_MatchResults_CompetitionId_Season_Stage"";

            ALTER TABLE ""MatchResults"" ADD COLUMN IF NOT EXISTS ""DivisionId"" uuid NULL;

            CREATE INDEX IF NOT EXISTS ""IX_MatchResults_DivisionId""
            ON ""MatchResults"" (""DivisionId"");
        ");

        migrationBuilder.Sql(@"
            ALTER TABLE ""Competitions"" ADD COLUMN IF NOT EXISTS ""DivisionId"" uuid NULL;

            DROP INDEX IF EXISTS ""IX_Competitions_Season_ShortCode"";
        ");

        migrationBuilder.Sql(@"
            ALTER TABLE ""Competitions""
                DROP COLUMN IF EXISTS ""Gender"",
                DROP COLUMN IF EXISTS ""AgeGroup"";
        ");
    }
}
