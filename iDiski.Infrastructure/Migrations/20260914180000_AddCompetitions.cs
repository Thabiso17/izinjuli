using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using iDiski.Infrastructure.Persistence;

#nullable disable

namespace iDiski.Infrastructure.Migrations;

/// <summary>
/// Competitions, so a division can run more than one thing at a time.
///
/// A division used to be the competition: it carried the format, and its entrants were every
/// team in it. That allows exactly one competition per division per season, which is not how a
/// season works — an under-seventeen division runs its league, a top-eight cup, and a sponsor's
/// tournament that invites clubs from other divisions, all at once.
///
/// The division becomes a pool of teams. What gets played is a Competition, and who plays in
/// one is an explicit entry list rather than the division's membership — which is what makes
/// "twelve of the twenty" and "and these four from elsewhere" expressible at all.
///
/// Nothing in production is lost. Every division that has fixtures or is not a plain league
/// becomes one competition carrying its format, its name and its season, with every one of its
/// teams entered and every one of its fixtures repointed. A league that has been running all
/// year carries on as a competition, and reads the same on every screen.
///
/// Written by hand and guarded throughout, as this project's recent migrations have been.
/// </summary>
/// <remarks>
/// The id is declared here rather than in a scaffolded Designer file. The attribute is what
/// EF reads to identify and order a migration; the Designer's generated model is only used by
/// the command-line tooling, and every operation below is raw SQL that needs none of it.
/// </remarks>
[DbContext(typeof(LeagueDbContext))]
[Migration("20260914180000_AddCompetitions")]
public partial class AddCompetitions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── The competition ───────────────────────────────────────────────────
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS ""Competitions"" (
                ""Id""          uuid NOT NULL,
                ""DivisionId""  uuid NOT NULL,
                ""Name""        character varying(100) NOT NULL,
                ""ShortCode""   character varying(20) NOT NULL,
                ""Season""      integer NOT NULL,
                ""Format""      character varying(20) NOT NULL DEFAULT 'League',
                ""StartDate""   timestamp with time zone NULL,
                ""EndDate""     timestamp with time zone NULL,
                ""Description"" character varying(1000) NULL,
                ""IsActive""    boolean NOT NULL DEFAULT TRUE,
                ""CreatedAt""   timestamp with time zone NOT NULL,
                ""UpdatedAt""   timestamp with time zone NULL,
                ""CreatedByUserId"" uuid NULL,
                ""UpdatedByUserId"" uuid NULL,
                CONSTRAINT ""PK_Competitions"" PRIMARY KEY (""Id""),
                CONSTRAINT ""FK_Competitions_Divisions_DivisionId""
                    FOREIGN KEY (""DivisionId"") REFERENCES ""Divisions"" (""Id"")
                    ON DELETE RESTRICT
            );
        ");

        migrationBuilder.Sql(@"
            CREATE UNIQUE INDEX IF NOT EXISTS
            ""IX_Competitions_DivisionId_Season_ShortCode""
            ON ""Competitions"" (""DivisionId"", ""Season"", ""ShortCode"");
        ");

        // ── Who is in it ──────────────────────────────────────────────────────
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS ""CompetitionEntries"" (
                ""Id""            uuid NOT NULL,
                ""CompetitionId"" uuid NOT NULL,
                ""TeamId""        uuid NOT NULL,
                ""CreatedAt""     timestamp with time zone NOT NULL,
                ""UpdatedAt""     timestamp with time zone NULL,
                ""CreatedByUserId"" uuid NULL,
                ""UpdatedByUserId"" uuid NULL,
                CONSTRAINT ""PK_CompetitionEntries"" PRIMARY KEY (""Id""),
                CONSTRAINT ""FK_CompetitionEntries_Competitions_CompetitionId""
                    FOREIGN KEY (""CompetitionId"") REFERENCES ""Competitions"" (""Id"")
                    ON DELETE CASCADE,
                CONSTRAINT ""FK_CompetitionEntries_Teams_TeamId""
                    FOREIGN KEY (""TeamId"") REFERENCES ""Teams"" (""Id"")
                    ON DELETE RESTRICT
            );
        ");

        // A club enters a competition once: twice would give them two places in the draw and
        // two rows in the table.
        migrationBuilder.Sql(@"
            CREATE UNIQUE INDEX IF NOT EXISTS
            ""IX_CompetitionEntries_CompetitionId_TeamId""
            ON ""CompetitionEntries"" (""CompetitionId"", ""TeamId"");
        ");

        // ── A fixture belongs to a competition ────────────────────────────────
        migrationBuilder.Sql(@"
            ALTER TABLE ""MatchResults""
            ADD COLUMN IF NOT EXISTS ""CompetitionId"" uuid NULL;
        ");

        migrationBuilder.Sql(@"
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conname = 'FK_MatchResults_Competitions_CompetitionId'
                ) THEN
                    ALTER TABLE ""MatchResults""
                    ADD CONSTRAINT ""FK_MatchResults_Competitions_CompetitionId""
                    FOREIGN KEY (""CompetitionId"") REFERENCES ""Competitions"" (""Id"")
                    ON DELETE RESTRICT;
                END IF;
            END $$;
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS ""IX_MatchResults_CompetitionId_Stage""
            ON ""MatchResults"" (""CompetitionId"", ""Stage"");
        ");

        // ── Carry the existing seasons across ─────────────────────────────────
        //
        // One competition per division that has something to carry, taking that division's
        // format and name. Deterministic id derived from the division's, so re-running this
        // cannot produce a second copy and the guard below stays honest.
        migrationBuilder.Sql(@"
            INSERT INTO ""Competitions"" (
                ""Id"", ""DivisionId"", ""Name"", ""ShortCode"", ""Season"", ""Format"",
                ""StartDate"", ""EndDate"", ""Description"", ""IsActive"", ""CreatedAt""
            )
            SELECT
                md5('competition:' || d.""Id""::text)::uuid,
                d.""Id"",
                d.""Name"",
                LEFT(d.""ShortCode"", 20),
                d.""Season"",
                COALESCE(d.""Format"", 'League'),
                d.""StartDate"",
                d.""EndDate"",
                d.""Description"",
                d.""IsActive"",
                NOW()
            FROM ""Divisions"" d
            WHERE
                -- Worth carrying across: it has been played, or it was never a plain league.
                (
                    EXISTS (SELECT 1 FROM ""MatchResults"" m WHERE m.""DivisionId"" = d.""Id"")
                    OR COALESCE(d.""Format"", 'League') <> 'League'
                    OR EXISTS (SELECT 1 FROM ""Teams"" t WHERE t.""DivisionId"" = d.""Id"")
                )
                AND NOT EXISTS (
                    SELECT 1 FROM ""Competitions"" c WHERE c.""DivisionId"" = d.""Id""
                );
        ");

        // Everyone in the division was an entrant, because that is exactly what entrants used
        // to mean.
        migrationBuilder.Sql(@"
            INSERT INTO ""CompetitionEntries"" (""Id"", ""CompetitionId"", ""TeamId"", ""CreatedAt"")
            SELECT
                md5('entry:' || c.""Id""::text || ':' || t.""Id""::text)::uuid,
                c.""Id"",
                t.""Id"",
                NOW()
            FROM ""Competitions"" c
            JOIN ""Teams"" t ON t.""DivisionId"" = c.""DivisionId""
            WHERE NOT EXISTS (
                SELECT 1 FROM ""CompetitionEntries"" e
                WHERE e.""CompetitionId"" = c.""Id"" AND e.""TeamId"" = t.""Id""
            );
        ");

        // And every fixture already played belongs to it.
        migrationBuilder.Sql(@"
            UPDATE ""MatchResults"" m
            SET ""CompetitionId"" = c.""Id""
            FROM ""Competitions"" c
            WHERE m.""DivisionId"" = c.""DivisionId""
              AND m.""CompetitionId"" IS NULL;
        ");

        // ── The division stops being a competition ────────────────────────────
        migrationBuilder.Sql(@"ALTER TABLE ""Divisions"" DROP COLUMN IF EXISTS ""Format"";");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Give the division back its format, taking it from whichever competition was carried
        // across from it. A division running several by then keeps the earliest, which is the
        // one that was its format before this migration ran.
        migrationBuilder.Sql(@"
            ALTER TABLE ""Divisions""
            ADD COLUMN IF NOT EXISTS ""Format"" character varying(20) NOT NULL DEFAULT 'League';
        ");

        migrationBuilder.Sql(@"
            UPDATE ""Divisions"" d
            SET ""Format"" = sub.""Format""
            FROM (
                SELECT DISTINCT ON (""DivisionId"") ""DivisionId"", ""Format""
                FROM ""Competitions""
                ORDER BY ""DivisionId"", ""CreatedAt"" ASC
            ) sub
            WHERE sub.""DivisionId"" = d.""Id"";
        ");

        // Fixtures belonging to a competition that was never its division's original one have
        // nowhere to go back to: a division can only hold one competition again. They are left
        // pointing at their division, which is where the old model would have put them.
        migrationBuilder.Sql(@"
            ALTER TABLE ""MatchResults""
            DROP CONSTRAINT IF EXISTS ""FK_MatchResults_Competitions_CompetitionId"";
        ");

        migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_MatchResults_CompetitionId_Stage"";");
        migrationBuilder.Sql(@"ALTER TABLE ""MatchResults"" DROP COLUMN IF EXISTS ""CompetitionId"";");

        migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""CompetitionEntries"";");
        migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""Competitions"";");
    }
}
