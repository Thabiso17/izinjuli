/**
 * How a competition is played. The API serialises enums as strings, so these are the names
 * rather than the numbers.
 *
 * This lives on the competition rather than the division, because a division runs several at
 * once and they are routinely different shapes.
 */
export type CompetitionFormat = 'League' | 'Knockout' | 'GroupAndKnockout';

/**
 * Who a competition is for. A string rather than the numeric Gender enum because the API
 * serialises enums by name, which is how divisions already send theirs.
 */
export type CompetitionGender = 'Male' | 'Female' | 'Mixed';

export const COMPETITION_GENDER_LABEL: Record<CompetitionGender, string> = {
  Male: 'Boys / Men',
  Female: 'Girls / Women',
  Mixed: 'Mixed',
};

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

/** On now or still to come — the distinction the listing pages filter on. */
export function isCurrentCompetition(status: CompetitionStatus): boolean {
  return status !== 'Completed';
}

/**
 * Something the clubs of a division actually play.
 *
 * A division is a pool of teams; this is the league, the cup, the sponsor's tournament. Several
 * run at once in one division and each has its own shape, its own entrants and its own status.
 */
export interface CompetitionDto {
  id: string;
  name: string;
  shortCode: string;
  season: number;
  format: CompetitionFormat;
  /** Who it is for. Checked against every club entered. */
  gender: CompetitionGender;
  /** "U17", "Open" — informational; age is not enforced at entry. */
  ageGroup: string | null;
  /**
   * How many clubs the organiser said would play, or null when they did not say. A cap on the
   * entry list, not a description of it: a ninth club is refused from an eight-club cup.
   */
  maxTeams: number | null;
  startDate: string | null;
  endDate: string | null;
  description: string | null;
  isActive: boolean;
  /** How many clubs are in it. Nothing else says: a competition is its entry list. */
  entrantCount: number;
  /** How many different divisions those clubs come from. More than one means a cup. */
  divisionsRepresented: number;
  matchCount: number;
  playedCount: number;
  pendingCount: number;
  finalPlayed: boolean;
  status: CompetitionStatus;
}

/** One club's place in a competition, and where they came from. */
export interface CompetitionEntrantDto {
  teamId: string;
  teamName: string;
  shortCode: string;
  logoUrl: string | null;
  divisionId: string | null;
  divisionName: string | null;
}

export interface CreateCompetitionCommand {
  name: string;
  shortCode: string;
  season: number;
  format: CompetitionFormat;
  gender: CompetitionGender;
  ageGroup?: string | null;
  /** Blank for "however many are entered", which is the ordinary case for a league. */
  maxTeams?: number | null;
  startDate?: string;
  endDate?: string;
  description?: string;
  /**
   * Optionally start the entry list off with every club in this division — what a league
   * wants. A convenience only: nothing afterwards records that they arrived together.
   */
  enterTeamsFromDivisionId?: string | null;
}

export interface UpdateCompetitionCommand {
  competitionId: string;
  name: string;
  shortCode: string;
  format: CompetitionFormat;
  gender: CompetitionGender;
  ageGroup?: string | null;
  maxTeams?: number | null;
  startDate?: string;
  endDate?: string;
  description?: string;
  isActive: boolean;
}

/**
 * How full a competition is, for a screen that wants to say so. Null when the organiser set no
 * number, in which case "entered" is the whole truth and there is nothing to be short of.
 */
export function entrantProgress(c: CompetitionDto): { text: string; full: boolean; short: number } {
  if (c.maxTeams == null) {
    return { text: `${c.entrantCount} entered`, full: false, short: 0 };
  }

  return {
    text: `${c.entrantCount} of ${c.maxTeams} entered`,
    full: c.entrantCount >= c.maxTeams,
    short: Math.max(0, c.maxTeams - c.entrantCount),
  };
}
