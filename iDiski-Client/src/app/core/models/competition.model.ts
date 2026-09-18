/**
 * How a competition is played. The API serialises enums as strings, so these are the names
 * rather than the numbers.
 *
 * This lives on the competition rather than the division, because a division runs several at
 * once and they are routinely different shapes.
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
  divisionId: string;
  divisionName: string;
  name: string;
  shortCode: string;
  season: number;
  format: CompetitionFormat;
  /**
   * How many clubs the organiser said would play, or null when they did not say. A cap on the
   * entry list, not a description of it: a ninth club is refused from an eight-club cup.
   */
  maxTeams: number | null;
  startDate: string | null;
  endDate: string | null;
  description: string | null;
  isActive: boolean;
  /** How many clubs are in it — which is not how many are in the division. */
  entrantCount: number;
  /** How many of those were invited from outside the division running it. */
  externalEntrantCount: number;
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
  /** True when this club plays in a different division from the one running it. */
  isExternal: boolean;
}

export interface CreateCompetitionCommand {
  divisionId: string;
  name: string;
  shortCode: string;
  season: number;
  format: CompetitionFormat;
  /** Blank for "however many are entered", which is the ordinary case for a league. */
  maxTeams?: number | null;
  startDate?: string;
  endDate?: string;
  description?: string;
  /** A league wants everybody; a cup is easier to trim down than to build up. */
  enterAllDivisionTeams: boolean;
}

export interface UpdateCompetitionCommand {
  competitionId: string;
  name: string;
  shortCode: string;
  format: CompetitionFormat;
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
