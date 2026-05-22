export interface SuspensionDto {
  id: string;
  playerId: string;
  playerName: string;
  teamName: string;
  reason: string;
  startDate: string;
  endDate: string;
  matchesSuspended: number;
  isActive: boolean;
}

export interface CreateSuspensionCommand {
  playerId: string;
  reason: string;
  matchesSuspended: number;
  startDate?: string;
}
