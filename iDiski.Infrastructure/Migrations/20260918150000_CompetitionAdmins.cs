using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using iDiski.Infrastructure.Persistence;

#nullable disable

namespace iDiski.Infrastructure.Migrations;

/// <summary>
/// Administrators are assigned to competitions rather than to divisions.
///
/// The middle role was DivisionAdmin, scoped to a division. A division is a collection of
/// clubs and runs nothing, so that scope granted no coherent authority over what its clubs
/// played. What gets administered is a competition, and an organiser is given the ones they
/// run — one, or several.
///
/// The stored role value does not move: DivisionAdmin and CompetitionAdmin are both 2, so
/// nobody's role changes here. Only the name changes, in code and in the JWT, which means
/// sessions holding a token issued before this deploy sign in again.
///
/// Existing assignments are carried across rather than dropped: a division admin becomes an
/// admin of every competition their division's clubs are currently entered in, so they keep
/// working on the same things the morning after. A division whose clubs are entered in
/// nothing leaves its admin assigned to nothing, which is the honest answer — there was
/// nothing there for them to run.
/// </summary>
/// <remarks>
/// The id is declared here rather than in a scaffolded Designer file, as with the migrations
/// before it.
/// </remarks>
[DbContext(typeof(LeagueDbContext))]
[Migration("20260918150000_CompetitionAdmins")]
public partial class CompetitionAdmins : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS ""UserCompetitions"" (
                ""Id""               uuid NOT NULL,
                ""UserId""           uuid NOT NULL,
                ""CompetitionId""    uuid NOT NULL,
                ""AssignedAt""       timestamp with time zone NOT NULL,
                ""AssignedByUserId"" uuid NULL,
                ""CreatedAt""        timestamp with time zone NOT NULL,
                ""UpdatedAt""        timestamp with time zone NULL,
                ""CreatedByUserId""  uuid NULL,
                ""UpdatedByUserId""  uuid NULL,
                CONSTRAINT ""PK_UserCompetitions"" PRIMARY KEY (""Id""),
                CONSTRAINT ""FK_UserCompetitions_Users_UserId""
                    FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"")
                    ON DELETE CASCADE,
                CONSTRAINT ""FK_UserCompetitions_Competitions_CompetitionId""
                    FOREIGN KEY (""CompetitionId"") REFERENCES ""Competitions"" (""Id"")
                    ON DELETE CASCADE
            );
        ");

        migrationBuilder.Sql(@"
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_UserCompetitions_UserId_CompetitionId""
            ON ""UserCompetitions"" (""UserId"", ""CompetitionId"");

            CREATE INDEX IF NOT EXISTS ""IX_UserCompetitions_CompetitionId""
            ON ""UserCompetitions"" (""CompetitionId"");
        ");

        // Carry existing admins across: every competition their division's clubs are in.
        // DISTINCT because a division of twenty clubs entered in one cup would otherwise
        // produce twenty identical assignments.
        migrationBuilder.Sql(@"
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.tables
                    WHERE table_name = 'UserDivisions'
                ) THEN
                    EXECUTE '
                        INSERT INTO ""UserCompetitions""
                            (""Id"", ""UserId"", ""CompetitionId"", ""AssignedAt"", ""CreatedAt"")
                        SELECT DISTINCT
                            gen_random_uuid(), ud.""UserId"", ce.""CompetitionId"",
                            now(), now()
                        FROM ""UserDivisions"" ud
                        JOIN ""Teams"" t              ON t.""DivisionId"" = ud.""DivisionId""
                        JOIN ""CompetitionEntries"" ce ON ce.""TeamId"" = t.""Id""
                        ON CONFLICT DO NOTHING';
                END IF;
            END $$;
        ");

        // Nothing reads it any more, and a table nobody reads is a question every reader has
        // to answer again.
        migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""UserDivisions"";");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // UserDivisions comes back empty. Which divisions somebody administered is not
        // recoverable from the competitions they were given: a competition is contested across
        // divisions, so there is no division to work back to.
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS ""UserDivisions"" (
                ""Id""               uuid NOT NULL,
                ""UserId""           uuid NOT NULL,
                ""DivisionId""       uuid NOT NULL,
                ""AssignedAt""       timestamp with time zone NOT NULL,
                ""AssignedByUserId"" uuid NULL,
                ""CreatedAt""        timestamp with time zone NOT NULL,
                ""UpdatedAt""        timestamp with time zone NULL,
                ""CreatedByUserId""  uuid NULL,
                ""UpdatedByUserId""  uuid NULL,
                CONSTRAINT ""PK_UserDivisions"" PRIMARY KEY (""Id"")
            );
        ");

        migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""UserCompetitions"";");
    }
}
