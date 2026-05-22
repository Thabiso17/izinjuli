using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iDiski.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDivisionMatchEventSuspension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DivisionId",
                table: "Teams",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DivisionId",
                table: "MatchResults",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Divisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ShortCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Season = table.Column<int>(type: "integer", nullable: false),
                    AgeGroup = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Gender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Divisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MatchEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Minute = table.Column<int>(type: "integer", nullable: false),
                    AdditionalInfo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchEvents", x => x.Id);
                    table.CheckConstraint("CK_MatchEvent_Minute", "\"Minute\" >= 1 AND \"Minute\" <= 120");
                    table.ForeignKey(
                        name: "FK_MatchEvents_MatchResults_MatchId",
                        column: x => x.MatchId,
                        principalTable: "MatchResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchEvents_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Suspensions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MatchesSuspended = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AppliedByUser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suspensions", x => x.Id);
                    table.CheckConstraint("CK_Suspension_Dates", "\"EndDate\" > \"StartDate\"");
                    table.ForeignKey(
                        name: "FK_Suspensions_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_DivisionId",
                table: "Teams",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchResults_DivisionId",
                table: "MatchResults",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Divisions_IsActive",
                table: "Divisions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Divisions_Season_ShortCode",
                table: "Divisions",
                columns: new[] { "Season", "ShortCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchEvents_MatchId",
                table: "MatchEvents",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchEvents_PlayerId",
                table: "MatchEvents",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Suspensions_PlayerId",
                table: "Suspensions",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Suspensions_PlayerId_IsActive",
                table: "Suspensions",
                columns: new[] { "PlayerId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_MatchResults_Divisions_DivisionId",
                table: "MatchResults",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Divisions_DivisionId",
                table: "Teams",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchResults_Divisions_DivisionId",
                table: "MatchResults");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Divisions_DivisionId",
                table: "Teams");

            migrationBuilder.DropTable(
                name: "Divisions");

            migrationBuilder.DropTable(
                name: "MatchEvents");

            migrationBuilder.DropTable(
                name: "Suspensions");

            migrationBuilder.DropIndex(
                name: "IX_Teams_DivisionId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_MatchResults_DivisionId",
                table: "MatchResults");

            migrationBuilder.DropColumn(
                name: "DivisionId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "DivisionId",
                table: "MatchResults");
        }
    }
}
