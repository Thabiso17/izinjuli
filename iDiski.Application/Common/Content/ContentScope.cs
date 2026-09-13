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
    /// A piece tagged to a player at a club becomes that club's record of their time there
    /// the moment the player leaves, and is closed to further editing. Reads the stored
    /// scope rather than the incoming one, so the lock cannot be sidestepped by retagging
    /// on the way past.
    /// </summary>
    public static async Task EnsureOpenForEditingAsync(
        ILeagueDbContext db,
        Guid? storedPlayerId,
        Guid? storedTeamId,
        CancellationToken cancellationToken)
    {
        if (storedPlayerId is null || storedTeamId is null) return;

        var currentTeamId = await db.Players
            .Where(p => p.Id == storedPlayerId.Value)
            .Select(p => (Guid?)p.TeamId)
            .FirstOrDefaultAsync(cancellationToken);

        // No player row left to compare against — nothing to protect.
        if (currentTeamId is null || currentTeamId == storedTeamId) return;

        throw new InvalidOperationException(
            "This is locked. The player it was written about has since left that team, so it "
            + "stands as a record of their time there and can no longer be edited.");
    }

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
