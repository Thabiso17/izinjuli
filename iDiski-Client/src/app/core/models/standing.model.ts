// src/app/core/models/standing.model.ts

/** A single row in the league table. */
export interface StandingDto {
  position: number;
  teamId: string;
  teamName: string;
  shortCode: string;
  logoUrl: string | null;
  played: number;
  won: number;
  drawn: number;
  lost: number;
  goalsFor: number;
  goalsAgainst: number;
  goalDifference: number;
  points: number;
  /**
   * Last 5 results, newest first, comma-separated: "W,W,D,L,W"
   * Parse with formArray = standing.form.split(',') and render as coloured dots.
   */
  form: string;
}

/** Wraps the full league table with season metadata. */
export interface LeagueTableDto {
  season: number;
  matchweeksPlayed: number;
  table: StandingDto[];
}

/** A single entry on the top-scorers leaderboard. */
export interface TopScorerDto {
  rank: number;
  playerId: string;
  fullName: string;
  profileImageUrl: string | null;
  teamName: string;
  teamShortCode: string;
  position: string;
  goals: number;
  assists: number;
  /** goals + assists */
  goalContributions: number;
}

/** Stats from a single meeting in a head-to-head comparison. */
export interface HeadToHeadMatchDto {
  matchDate: string;
  season: number;
  matchweek: number;
  homeTeamName: string;
  awayTeamName: string;
  homeScore: number;
  awayScore: number;
  result: 'HOME_WIN' | 'AWAY_WIN' | 'DRAW';
}

/** Full head-to-head summary between two teams. */
export interface HeadToHeadDto {
  teamAId: string;
  teamAName: string;
  teamAShortCode: string;
  teamBId: string;
  teamBName: string;
  teamBShortCode: string;
  totalMeetings: number;
  teamAWins: number;
  teamBWins: number;
  draws: number;
  teamAGoalsScored: number;
  teamBGoalsScored: number;
  /** Capped at 10, newest first */
  recentMeetings: HeadToHeadMatchDto[];
}
