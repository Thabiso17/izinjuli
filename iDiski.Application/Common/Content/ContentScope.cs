using FluentValidation;
using iDiski.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Common.Content;

/// <summary>
/// Implemented by commands that let an author say what a piece of content is about:
/// a division, a team inside it, or a player inside that team. All null is league-wide.
/// </summary>
public interface IContentScope
{
    Guid? DivisionId { get; }
    Guid? TeamId { get; }
    Guid? PlayerId { get; }
}

public static class ContentScopeRules
{
    /// <summary>
    /// Enforces that the scope narrows consistently — a team belongs to the named division,
    /// a player to the named team — so content can never claim a subject it doesn't have.
    /// </summary>
    public static void AddContentScopeRules<T>(this AbstractValidator<T> validator, ILeagueDbContext db)
        where T : IContentScope
    {
        validator.RuleFor(x => x.DivisionId)
            .MustAsync(async (divisionId, ct) =>
                await db.Divisions.AnyAsync(d => d.Id == divisionId!.Value, ct))
            .When(x => x.DivisionId.HasValue)
            .WithMessage("Division not found.");

        validator.RuleFor(x => x.TeamId)
            .Must((cmd, _) => cmd.DivisionId.HasValue)
            .When(x => x.TeamId.HasValue)
            .WithMessage("Select a division before selecting a team.");

        validator.RuleFor(x => x.TeamId)
            .MustAsync(async (cmd, teamId, ct) =>
                await db.Teams.AnyAsync(t => t.Id == teamId!.Value && t.DivisionId == cmd.DivisionId, ct))
            .When(x => x.TeamId.HasValue && x.DivisionId.HasValue)
            .WithMessage("That team is not in the selected division.");

        validator.RuleFor(x => x.PlayerId)
            .Must((cmd, _) => cmd.TeamId.HasValue)
            .When(x => x.PlayerId.HasValue)
            .WithMessage("Select a team before selecting a player.");

        // Deliberately not checking that the player is currently in the named team. A scope
        // records who the piece was about when it was written, and players transfer: an
        // article about a player's time at their old club stays with that club, and must
        // still be editable afterwards. Nothing records past squads, so a stricter rule
        // could not tell a historical tag from a wrong one anyway.
        validator.RuleFor(x => x.PlayerId)
            .MustAsync(async (playerId, ct) =>
                await db.Players.AnyAsync(p => p.Id == playerId!.Value, ct))
            .When(x => x.PlayerId.HasValue)
            .WithMessage("Player not found.");
    }
}
