import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { MatchResultDto, PaginatedList, CreateMatchCommand, UpdateMatchScoreCommand } from '../models';

@Injectable({ providedIn: 'root' })
export class MatchService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/matchresults`;

  getAll(
    season: number,
    matchweek?: number,
    teamId?: string,
    status?: string,
    divisionId?: string,
    pageNumber: number = 1,
    pageSize: number = 20
  ): Observable<PaginatedList<MatchResultDto>> {
    let params = new HttpParams()
      .set('season', season.toString())
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (matchweek) params = params.set('matchweek', matchweek.toString());
    if (teamId) params = params.set('teamId', teamId);
    if (status) params = params.set('status', status);
    if (divisionId) params = params.set('divisionId', divisionId);

    return this.http.get<PaginatedList<MatchResultDto>>(this.base, { params });
  }

  getById(id: string): Observable<MatchResultDto> {
    return this.http.get<MatchResultDto>(`${this.base}/${id}`);
  }

  create(command: CreateMatchCommand): Observable<string> {
    return this.http.post<string>(this.base, command);
  }

  updateScore(id: string, command: UpdateMatchScoreCommand): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}/score`, command);
  }

  generateFixtures(command: GenerateFixturesCommand): Observable<GenerateFixturesResult> {
    return this.http.post<GenerateFixturesResult>(`${this.base}/generate`, command);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}

export interface GenerateFixturesCommand {
  divisionId: string;
  season: number;
  isHomeAndAway: boolean;
  startDate: string; // ISO date string
  daysBetweenMatchweeks: number;
}

export interface GenerateFixturesResult {
  fixturesGenerated: number;
  matchweeksCreated: number;
  firstMatchDate: string;
  lastMatchDate: string;
}
