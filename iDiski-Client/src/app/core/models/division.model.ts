export enum Gender {
  Male = 0,
  Female = 1,
  Mixed = 2
}

/**
 * How a competition is played. The API serialises enums as strings, so these are the names
 * rather than the numbers.
 */
export type CompetitionFormat = 'League' | 'Knockout' | 'GroupAndKnockout';

export const COMPETITION_FORMAT_LABEL: Record<CompetitionFormat, string> = {
  League: 'League',
  Knockout: 'Knockout',
  GroupAndKnockout: 'Groups then knockout',
};

/** What each one means for the organiser choosing between them. */
export const COMPETITION_FORMAT_HINT: Record<CompetitionFormat, string> = {
  League: 'Everyone plays everyone. The table decides it.',
  Knockout: 'Lose and you are out. A bracket, drawn in full before a ball is kicked.',
  GroupAndKnockout: 'Groups first, then the teams that come through play a bracket.',
};

/**
 * How far through its own life a competition is. Worked out by the API from the results
 * themselves rather than stored, so it cannot go stale the way a flag somebody ticks does.
 */
export type CompetitionStatus = 'NotStarted' | 'InProgress' | 'Completed';

export const COMPETITION_STATUS_LABEL: Record<CompetitionStatus, string> = {
  NotStarted: 'Not started',
  InProgress: 'In progress',
  Completed: 'Completed',
};

/** Bootstrap contextual class per status, so the badge reads the same on every screen. */
export const COMPETITION_STATUS_CLASS: Record<CompetitionStatus, string> = {
  NotStarted: 'bg-secondary',
  InProgress: 'bg-success',
  Completed: 'bg-dark',
};

/** On now or still to come — the distinction the divisions page filters on. */
export function isCurrentCompetition(status: CompetitionStatus): boolean {
  return status !== 'Completed';
}

export interface DivisionDto {
  id: string;
  name: string;
  shortCode: string;
  season: number;
  format: CompetitionFormat;
  ageGroup: string | null;
  gender: string | null;
  isActive: boolean;
  startDate: string | null;
  endDate: string | null;
  description: string | null;
  teamCount: number;
  matchCount: number;
  /** Fixtures with a result, and fixtures still expected. `matchCount` is every fixture drawn. */
  playedCount: number;
  pendingCount: number;
  /** Whether the tie nothing follows has been played. Always false for a league. */
  finalPlayed: boolean;
  status: CompetitionStatus;
}

export interface CreateDivisionCommand {
  name: string;
  shortCode: string;
  season: number;
  format: CompetitionFormat;
  ageGroup?: string;
  gender?: string;
  startDate?: string;
  endDate?: string;
  description?: string;
}

export interface UpdateDivisionCommand extends CreateDivisionCommand {
  id: string;
  isActive: boolean;
}
