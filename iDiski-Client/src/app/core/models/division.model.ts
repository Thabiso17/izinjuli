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
