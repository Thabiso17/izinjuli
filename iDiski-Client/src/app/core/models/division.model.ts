export enum Gender {
  Male = 0,
  Female = 1,
  Mixed = 2
}

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
