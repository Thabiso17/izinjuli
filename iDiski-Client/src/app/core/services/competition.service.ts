import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CompetitionDto,
  CompetitionEntrantDto,
  CreateCompetitionCommand,
  UpdateCompetitionCommand,
} from '../models';

@Injectable({ providedIn: 'root' })
export class CompetitionService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/competitions`;

  getAll(divisionId?: string, season?: number, isActive?: boolean): Observable<CompetitionDto[]> {
    let params = new HttpParams();
    if (divisionId) params = params.set('divisionId', divisionId);
    if (season) params = params.set('season', season.toString());
    if (isActive !== undefined) params = params.set('isActive', isActive.toString());

    return this.http.get<CompetitionDto[]>(this.base, { params });
  }

  getById(id: string): Observable<CompetitionDto> {
    return this.http.get<CompetitionDto>(`${this.base}/${id}`);
  }

  /** Who is entered, and which of them were invited from another division. */
  getEntrants(id: string): Observable<CompetitionEntrantDto[]> {
    return this.http.get<CompetitionEntrantDto[]>(`${this.base}/${id}/entrants`);
  }

  create(command: CreateCompetitionCommand): Observable<string> {
    return this.http.post<string>(this.base, command);
  }

  update(command: UpdateCompetitionCommand): Observable<void> {
    return this.http.put<void>(`${this.base}/${command.competitionId}`, command);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  /**
   * Enters a club. It may come from another division — that is what a sponsor's cup is for —
   * but the API refuses one from a division of a different gender.
   */
  enter(competitionId: string, teamId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${competitionId}/entrants/${teamId}`, {});
  }

  withdraw(competitionId: string, teamId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${competitionId}/entrants/${teamId}`);
  }
}
