// src/app/core/models/player.model.ts

export enum PlayerPosition {
  // Goalkeeper
  GK = 'GK',

  // Defenders
  CB = 'CB',
  SW = 'SW',
  RB = 'RB',
  LB = 'LB',
  RWB = 'RWB',
  LWB = 'LWB',

  // Midfielders
  CDM = 'CDM',
  CM = 'CM',
  CAM = 'CAM',
  RM = 'RM',
  LM = 'LM',

  // Forwards
  ST = 'ST',
  CF = 'CF',
  RW = 'RW',
  LW = 'LW'
}

export enum PreferredFoot {
  Right = 'Right',
  Left = 'Left',
  Both = 'Both'
}

export interface PlayerDto {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  profileImageUrl: string | null;
  /** Detailed player biography for "Player of the Month" features (max 2000 chars) */
  bio: string | null;
  dateOfBirth: string;
  ageYears: number;
  nationality: string | null;
  jerseyNumber: number;
  position: PlayerPosition;
  preferredFoot: PreferredFoot;
  isActive: boolean;
  goals: number;
  assists: number;
  yellowCards: number;
  redCards: number;
  teamId: string;
  teamName: string;
  matchesPlayed: number;
  isCurrentlySuspended: boolean;
  suspensionEndDate: string | null;
}

export interface CreatePlayerRequest {
  firstName: string;
  lastName: string;
  profileImageUrl?: string;
  bio?: string;
  dateOfBirth: string;
  nationality?: string;
  jerseyNumber: number;
  position: PlayerPosition;
  preferredFoot: PreferredFoot;
  teamId: string;
}

export interface UpdatePlayerRequest extends CreatePlayerRequest {
  id: string;
  isActive: boolean;
}
