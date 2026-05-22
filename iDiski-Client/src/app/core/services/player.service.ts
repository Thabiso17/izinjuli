import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PlayerDto, CreatePlayerRequest, UpdatePlayerRequest } from '../models';

@Injectable({ providedIn: 'root' })
export class PlayerService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/players`;

  getAll(teamId?: string, activeOnly: boolean = true): Observable<PlayerDto[]> {
    let params = new HttpParams().set('activeOnly', activeOnly.toString());
    if (teamId) params = params.set('teamId', teamId);
    return this.http.get<PlayerDto[]>(this.base, { params });
  }

  getById(id: string): Observable<PlayerDto> {
    return this.http.get<PlayerDto>(`${this.base}/${id}`);
  }

  create(command: CreatePlayerRequest): Observable<string> {
    return this.http.post<string>(this.base, command);
  }

  update(id: string, command: UpdatePlayerRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}`, command);
  }

  transfer(playerId: string, newTeamId: string, newJerseyNumber: number): Observable<void> {
    return this.http.post<void>(`${this.base}/${playerId}/transfer`, {
      playerId,
      newTeamId,
      newJerseyNumber
    });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
