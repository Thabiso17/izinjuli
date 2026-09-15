export enum Gender {
  Male = 0,
  Female = 1,
  Mixed = 2
}

/**
 * A pool of teams — "U17 Boys, 2026" — rather than a competition.
 *
 * It used to carry a format and a status of its own, because a division was the competition.
 * Those moved to CompetitionDto when a division became able to run several at once: its league
 * can be halfway through while its cup has already been won, and no single status can honestly
 * describe both.
 */
export interface DivisionDto {
  id: string;
  name: string;
  shortCode: string;
  season: number;
  ageGroup: string | null;
  gender: string | null;
  isActive: boolean;
  startDate: string | null;
  endDate: string | null;
  description: string | null;
  teamCount: number;
  matchCount: number;
  /** How many competitions are being run from this division. */
  competitionCount: number;
}

export interface CreateDivisionCommand {
  name: string;
  shortCode: string;
  season: number;
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
