// src/app/core/models/match.model.ts

export type MatchStatus =
  | 'Scheduled'
  | 'InProgress'
  | 'Completed'
  | 'Postponed'
  | 'Cancelled';

export type MatchStage = 'League' | 'Group' | 'Knockout';

/**
 * What a round of that size is called. Derived from the number of teams left rather than sent
 * down as a label, so the bracket names itself whatever size it is.
 */
export function knockoutRoundName(teamsLeft: number | null | undefined): string | null {
  if (!teamsLeft) return null;

  switch (teamsLeft) {
    case 2:
      return 'Final';
    case 4:
      return 'Semi-final';
    case 8:
      return 'Quarter-final';
    default:
      return `Round of ${teamsLeft}`;
  }
}

export interface MatchResultDto {
  id: string;
  matchDate: string;
  matchweekNumber: number;
  season: number;
  venue: string | null;
  referee: string | null;
  status: MatchStatus;
  /** "vs" when Scheduled, "2 – 1" when Completed */
  scoreDisplay: string;
  homeScore: number;
  awayScore: number;
  // Null until a knockout slot is filled: a semi-final is scheduled while the quarter-finals
  // are still being played, so it has a date and a venue before it has teams.
  homeTeamId: string | null;
  homeTeamName: string | null;
  homeTeamLogo: string | null;
  homeTeamShortCode: string | null;
  awayTeamId: string | null;
  awayTeamName: string | null;
  awayTeamLogo: string | null;
  awayTeamShortCode: string | null;
  notes: string | null;
  divisionId: string | null;
  divisionName: string | null;
  /** Which part of the competition this belongs to. */
  stage: MatchStage;
  /** For a group-stage fixture: "A", "B", and so on. */
  groupName: string | null;
  /** Teams left at this point in a bracket: 2 is the final, 4 the semi-finals. */
  knockoutRoundSize: number | null;
  events: any[]; // Will be populated with MatchEventDto[]
}

export interface CreateMatchCommand {
  matchDate: string;
  matchweekNumber: number;
  season: number;
  homeTeamId: string;
  awayTeamId: string;
  venue?: string;
  referee?: string;
  divisionId?: string;
}

export interface UpdateMatchScoreCommand {
  id: string;
  homeScore: number;
  awayScore: number;
  status: MatchStatus;
  notes?: string;
}

// src/app/core/models/pagination.model.ts

/** Generic paginated wrapper matching PaginatedList<T> from the C# API. */
export interface PaginatedList<T> {
  items: T[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
