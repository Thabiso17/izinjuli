export enum EventType {
  Goal = 'Goal',
  Assist = 'Assist',
  YellowCard = 'YellowCard',
  RedCard = 'RedCard',
  OwnGoal = 'OwnGoal',
  Substitution = 'Substitution'
}

export interface MatchEventDto {
  id: string;
  matchId: string;
  playerId: string;
  playerName: string;
  teamName: string;
  eventType: EventType;
  minute: number;
  additionalInfo: string | null;
}

export interface MatchEventInput {
  playerId: string;
  eventType: EventType;
  minute: number;
  additionalInfo?: string;
}

export interface RecordMatchEventsCommand {
  matchId: string;
  events: MatchEventInput[];
}
