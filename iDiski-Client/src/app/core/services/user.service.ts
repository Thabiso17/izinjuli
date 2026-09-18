import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AdminUserDto,
  AdminUserDetailDto,
  CreateAdminUserRequest,
  UpdateAdminUserRequest,
} from '../models';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/users`;

  // Creating a user lives under auth rather than users, because it is the same command the
  // sign-up path uses and it carries the password.
  private readonly authBase = `${environment.apiBaseUrl}/auth`;

  getAll(): Observable<AdminUserDto[]> {
    return this.http.get<AdminUserDto[]>(this.base);
  }

  getById(id: string): Observable<AdminUserDetailDto> {
    return this.http.get<AdminUserDetailDto>(`${this.base}/${id}`);
  }

  create(request: CreateAdminUserRequest): Observable<{ userId: string }> {
    return this.http.post<{ userId: string }>(`${this.authBase}/create-user`, request);
  }

  update(request: UpdateAdminUserRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/${request.id}`, request);
  }

  assignRole(userId: string, role: number): Observable<void> {
    return this.http.post<void>(`${this.base}/${userId}/roles`, { userId, role });
  }

  removeRole(userId: string, role: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${userId}/roles/${role}`);
  }

  assignTeam(userId: string, teamId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${userId}/teams`, { userId, teamId });
  }

  removeTeam(userId: string, teamId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${userId}/teams/${teamId}`);
  }

  assignCompetition(userId: string, competitionId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${userId}/competitions`, { userId, competitionId });
  }

  removeCompetition(userId: string, competitionId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${userId}/competitions/${competitionId}`);
  }
}
