using System;
using System.Threading.Tasks;
using iDiski.Domain.Entities;
using iDiski.Domain.Enums;
using iDiski.Infrastructure.Persistence;

namespace iDiski.Tests.Integration.Common;

/// <summary>
/// Two divisions, a team in each, a player, and one admin of every role wired to the right
/// scope: the team admin owns TeamA, the division admin owns DivisionOne. That wiring is the
/// whole point — the ownership handlers resolve access by reading UserTeams and
/// UserCompetitions,
/// so a scenario whose assignments point at the wrong user proves nothing.
///
/// Everything is created fresh per call with new ids, so tests sharing a fixture cannot
/// collide with each other.
/// </summary>
public sealed class LeagueScenario
{
    public Guid SuperAdminId { get; } = Guid.NewGuid();
    public Guid CompetitionAdminId { get; } = Guid.NewGuid();
    public Guid TeamAdminId { get; } = Guid.NewGuid();

    /// <summary>The division whose clubs play in the scenario's competition.</summary>
    public Guid DivisionOneId { get; } = Guid.NewGuid();

    /// <summary>A second division, whose clubs are in nothing.</summary>
    public Guid DivisionTwoId { get; } = Guid.NewGuid();

    /// <summary>The competition the competition admin was assigned.</summary>
    public Guid CompetitionOneId { get; } = Guid.NewGuid();

    /// <summary>In DivisionOne, and the team the team admin is assigned to.</summary>
    public Guid TeamAId { get; } = Guid.NewGuid();

    /// <summary>In DivisionOne, but not assigned to the team admin.</summary>
    public Guid TeamBId { get; } = Guid.NewGuid();

    /// <summary>In DivisionTwo, outside the division admin's reach.</summary>
    public Guid TeamCId { get; } = Guid.NewGuid();

    /// <summary>Plays for TeamA.</summary>
    public Guid PlayerAId { get; } = Guid.NewGuid();

    public static async Task<LeagueScenario> CreateAsync(LeagueDbContext db)
    {
        var s = new LeagueScenario();
        var now = DateTime.UtcNow;

        // Tests in a class share this context. If one of them failed partway through a save its
        // entities are still tracked as Added, and every save after it would resubmit them —
        // one genuine failure would then cascade into every test that ran afterwards, all
        // reporting a duplicate key that has nothing to do with them.
        db.ChangeTracker.Clear();

        db.Divisions.AddRange(
            new Division
            {
                Id = s.DivisionOneId, Name = "Division One", ShortCode = $"D1{Suffix()}",
                Season = 2026, Gender = Gender.Male, IsActive = true, CreatedAt = now
            },
            new Division
            {
                Id = s.DivisionTwoId, Name = "Division Two", ShortCode = $"D2{Suffix()}",
                Season = 2026, Gender = Gender.Male, IsActive = true, CreatedAt = now
            });

        db.Teams.AddRange(
            new Team { Id = s.TeamAId, Name = "Team A", ShortCode = $"TA{Suffix()}", DivisionId = s.DivisionOneId, Founded = 2020, CreatedAt = now },
            new Team { Id = s.TeamBId, Name = "Team B", ShortCode = $"TB{Suffix()}", DivisionId = s.DivisionOneId, Founded = 2020, CreatedAt = now },
            new Team { Id = s.TeamCId, Name = "Team C", ShortCode = $"TC{Suffix()}", DivisionId = s.DivisionTwoId, Founded = 2020, CreatedAt = now });

        db.Players.Add(new Player
        {
            Id = s.PlayerAId, FirstName = "Player", LastName = "One", TeamId = s.TeamAId,
            JerseyNumber = 9, Position = PlayerPosition.ST, PreferredFoot = PreferredFoot.Right,
            DateOfBirth = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true, CreatedAt = now
        });

        foreach (var (id, first) in new[]
                 {
                     (s.SuperAdminId, "Super"), (s.CompetitionAdminId, "Competition"), (s.TeamAdminId, "Team")
                 })
        {
            db.Users.Add(new User
            {
                Id = id, Email = $"{first.ToLowerInvariant()}-{id:N}@test.com",
                PasswordHash = "not-used-here", FirstName = first, LastName = "Admin",
                IsActive = true, CreatedAt = now
            });
        }

        db.UserRoles.AddRange(
            new UserRole { Id = Guid.NewGuid(), UserId = s.SuperAdminId, Role = Role.SuperAdmin, AssignedAt = now, CreatedAt = now },
            new UserRole { Id = Guid.NewGuid(), UserId = s.CompetitionAdminId, Role = Role.CompetitionAdmin, AssignedAt = now, CreatedAt = now },
            new UserRole { Id = Guid.NewGuid(), UserId = s.TeamAdminId, Role = Role.TeamAdmin, AssignedAt = now, CreatedAt = now });

        // Something for them to run. A competition admin assigned to nothing administers
        // nothing, so the scenario gives them one and enters DivisionOne's clubs in it.
        db.Competitions.Add(new Competition
        {
            Id = s.CompetitionOneId,
            Name = $"League {Suffix()}",
            ShortCode = TestIds.Code("LS"),
            Season = 2026,
            Format = CompetitionFormat.League,
            Gender = Gender.Male,
            IsActive = true,
            CreatedAt = now,
        });

        foreach (var teamId in new[] { s.TeamAId, s.TeamBId })
        {
            db.CompetitionEntries.Add(new CompetitionEntry
            {
                Id = Guid.NewGuid(), CompetitionId = s.CompetitionOneId, TeamId = teamId,
                CreatedAt = now
            });
        }

        db.UserCompetitions.Add(new UserCompetition
        {
            Id = Guid.NewGuid(), UserId = s.CompetitionAdminId,
            CompetitionId = s.CompetitionOneId,
            AssignedAt = now, CreatedAt = now
        });

        db.UserTeams.Add(new UserTeam
        {
            Id = Guid.NewGuid(), UserId = s.TeamAdminId, TeamId = s.TeamAId,
            AssignedAt = now, CreatedAt = now
        });

        await db.SaveChangesAsync();
        return s;
    }

    /// <summary>Short codes carry a unique index, so they cannot repeat across scenarios.</summary>
    private static string Suffix() => TestIds.Code(string.Empty);

    public Guid UserFor(Role role) => role switch
    {
        Role.SuperAdmin => SuperAdminId,
        Role.CompetitionAdmin => CompetitionAdminId,
        Role.TeamAdmin => TeamAdminId,
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    public FakeCurrentUserService SignedInAs(Role role) => new(UserFor(role), role);
}
