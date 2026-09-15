import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LeagueTableDto, TopScorerDto, HeadToHeadDto } from '../models';

@Injectable({ providedIn: 'root' })
export class StandingsService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/standings`;

  /**
   * Fetches the full league table for a season.
   * @param season Season year, e.g. 2025
   * @param divisionId Optional division filter
   * @param upToMatchweek Optional historical snapshot ceiling
   */
  getLeagueTable(
    season: number,
    divisionId?: string,
    upToMatchweek?: number,
    /** One group of a group stage. Omitted, the table covers the whole competition. */
    group?: string,
    /**
     * The competition whose table this is, and the way to ask for a real one: entrants then
     * come from its entry list rather than from who happens to share a division, which is what
     * makes a table of twelve of a division's twenty possible.
     */
    competitionId?: string
  ): Observable<LeagueTableDto> {
    let params = new HttpParams().set('season', season.toString());
    if (divisionId) params = params.set('divisionId', divisionId);
    if (upToMatchweek !== undefined) params = params.set('upToMatchweek', upToMatchweek.toString());
    if (group) params = params.set('group', group);
    if (competitionId) params = params.set('competitionId', competitionId);
    return this.http.get<LeagueTableDto>(`${this.base}/table`, { params });
  }

  /**
   * Fetches the top-scorers leaderboard.
   * @param season Season year
   * @param topN Number of players to return (default 10)
   * @param divisionId Optional division filter
   */
  getTopScorers(season: number, topN: number = 10, divisionId?: string): Observable<TopScorerDto[]> {
    let params = new HttpParams()
      .set('season', season.toString())
      .set('topN', topN.toString());
    if (divisionId) params = params.set('divisionId', divisionId);
    return this.http.get<TopScorerDto[]>(`${this.base}/top-scorers`, { params });
  }

  /**
   * Fetches all-time head-to-head stats between two clubs.
   */
  getHeadToHead(teamAId: string, teamBId: string): Observable<HeadToHeadDto> {
    const params = new HttpParams()
      .set('teamAId', teamAId)
      .set('teamBId', teamBId);
    return this.http.get<HeadToHeadDto>(`${this.base}/head-to-head`, { params });
  }
}
