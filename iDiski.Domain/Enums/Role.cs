namespace iDiski.Domain.Enums;

/// <summary>
/// What somebody may do, in three tiers.
///
/// The middle one used to be DivisionAdmin, scoped to a division. A division is a collection of
/// clubs and runs nothing, so there was nothing for that scope to grant: what gets administered
/// is a competition, and an organiser is assigned to the ones they run — one or several.
///
/// The stored value is unchanged, so nobody's role moves when this deploys. The name does
/// travel in the JWT, so tokens issued before it carry the old one and those sessions sign in
/// again.
/// </summary>
public enum Role
{
    /// <summary>Their club, and its players. Assigned through UserTeams.</summary>
    TeamAdmin = 1,

    /// <summary>
    /// The competitions they were assigned, through UserCompetitions: entrants, fixtures,
    /// results. Not the clubs playing in them — a club belongs to its own administrators,
    /// whatever it happens to be entered in.
    /// </summary>
    CompetitionAdmin = 2,

    /// <summary>Everything, including creating competitions and assigning their organisers.</summary>
    SuperAdmin = 3
}
