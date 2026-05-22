// src/app/core/models/team.model.ts

export interface TeamDto {
  id: string;
  name: string;
  shortCode: string;
  logoUrl: string | null;
  founded: number;
  homeGround: string | null;
  city: string | null;
  primaryColour: string | null;
  secondaryColour: string | null;
  playerCount: number;
  divisionId: string | null;
  divisionName: string | null;
}

export interface CreateTeamRequest {
  name: string;
  shortCode: string;
  logoUrl?: string;
  founded: number;
  homeGround?: string;
  city?: string;
  primaryColour?: string;
  secondaryColour?: string;
  divisionId?: string;
}

export interface UpdateTeamRequest extends CreateTeamRequest {
  id: string;
}
