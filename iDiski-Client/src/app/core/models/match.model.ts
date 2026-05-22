// src/app/core/models/match.model.ts

export type MatchStatus =
  | 'Scheduled'
  | 'InProgress'
  | 'Completed'
  | 'Postponed'
  | 'Cancelled';

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
  homeTeamId: string;
  homeTeamName: string;
  homeTeamLogo: string | null;
  homeTeamShortCode: string;
  awayTeamId: string;
  awayTeamName: string;
  awayTeamLogo: string | null;
  awayTeamShortCode: string;
  notes: string | null;
  divisionId: string | null;
  divisionName: string | null;
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
