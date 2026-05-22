import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TeamDto, CreateTeamRequest, UpdateTeamRequest } from '../models';

@Injectable({ providedIn: 'root' })
export class TeamService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/teams`;

  getAll(): Observable<TeamDto[]> {
    return this.http.get<TeamDto[]>(this.base);
  }

  getById(id: string): Observable<TeamDto> {
    return this.http.get<TeamDto>(`${this.base}/${id}`);
  }

  create(command: CreateTeamRequest): Observable<string> {
    return this.http.post<string>(this.base, command);
  }

  update(id: string, command: UpdateTeamRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}`, command);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
