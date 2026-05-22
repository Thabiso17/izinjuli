// src/app/core/services/league.service.ts

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  LeagueTableDto,
  TopScorerDto,
  HeadToHeadDto,
  TeamDto,
  CreateTeamRequest,
  MatchResultDto,
  PaginatedList,
} from '../models';

@Injectable({ providedIn: 'root' })
export class LeagueService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  // ── STANDINGS ──────────────────────────────────────────────────────────────

  /**
   * Fetches the full league table for a season.
   * @param season  Season year, e.g. 2025.
   * @param divisionId  Optional division filter.
   * @param upToMatchweek  Optional historical snapshot ceiling.
   */
  getLeagueTable(season: number, divisionId?: string, upToMatchweek?: number): Observable<LeagueTableDto> {
    let params = new HttpParams().set('season', season);
    if (divisionId) params = params.set('divisionId', divisionId);
    if (upToMatchweek != null) params = params.set('upToMatchweek', upToMatchweek);
    return this.http.get<LeagueTableDto>(`${this.base}/standings/table`, { params });
  }

  /**
   * Fetches the top-scorers leaderboard.
   * @param season  Season year.
   * @param topN    Number of players to return (default 10).
   */
  getTopScorers(season: number, topN = 10): Observable<TopScorerDto[]> {
    const params = new HttpParams().set('season', season).set('topN', topN);
    return this.http.get<TopScorerDto[]>(`${this.base}/standings/top-scorers`, { params });
  }

  /**
   * Fetches all-time head-to-head stats between two clubs.
   */
  getHeadToHead(teamAId: string, teamBId: string): Observable<HeadToHeadDto> {
    const params = new HttpParams().set('teamAId', teamAId).set('teamBId', teamBId);
    return this.http.get<HeadToHeadDto>(`${this.base}/standings/head-to-head`, { params });
  }

  // ── TEAMS ──────────────────────────────────────────────────────────────────

  /** Returns all teams ordered alphabetically. */
  getAllTeams(): Observable<TeamDto[]> {
    return this.http.get<TeamDto[]>(`${this.base}/teams`);
  }

  /** Returns a single team by ID. */
  getTeamById(id: string): Observable<TeamDto> {
    return this.http.get<TeamDto>(`${this.base}/teams/${id}`);
  }

  /** Creates a new team. Returns the new team's Guid. */
  createTeam(request: CreateTeamRequest): Observable<string> {
    return this.http.post<string>(`${this.base}/teams`, request);
  }

  // ── FIXTURES ───────────────────────────────────────────────────────────────

  /**
   * Returns a paginated fixture/results list for a season.
   * All parameters except season are optional.
   */
  getFixtures(options: {
    season: number;
    matchweek?: number;
    teamId?: string;
    status?: string;
    pageNumber?: number;
    pageSize?: number;
  }): Observable<PaginatedList<MatchResultDto>> {
    let params = new HttpParams().set('season', options.season);
    if (options.matchweek != null) params = params.set('matchweek', options.matchweek);
    if (options.teamId)            params = params.set('teamId', options.teamId);
    if (options.status)            params = params.set('status', options.status);
    if (options.pageNumber != null) params = params.set('pageNumber', options.pageNumber);
    if (options.pageSize != null)   params = params.set('pageSize', options.pageSize);

    return this.http.get<PaginatedList<MatchResultDto>>(
      `${this.base}/matchresults`, { params });
  }

  /** Returns a single match by ID. */
  getMatchById(id: string): Observable<MatchResultDto> {
    return this.http.get<MatchResultDto>(`${this.base}/matchresults/${id}`);
  }
}
