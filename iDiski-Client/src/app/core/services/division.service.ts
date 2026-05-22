import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DivisionDto, CreateDivisionCommand, UpdateDivisionCommand } from '../models';

@Injectable({ providedIn: 'root' })
export class DivisionService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/divisions`;

  getAvailableSeasons(): Observable<number[]> {
    return this.http.get<number[]>(`${this.base}/seasons`);
  }

  getAll(season?: number, isActive?: boolean): Observable<DivisionDto[]> {
    let params = new HttpParams();
    if (season) params = params.set('season', season.toString());
    if (isActive !== undefined) params = params.set('isActive', isActive.toString());
    return this.http.get<DivisionDto[]>(this.base, { params });
  }

  getById(id: string): Observable<DivisionDto> {
    return this.http.get<DivisionDto>(`${this.base}/${id}`);
  }

  create(command: CreateDivisionCommand): Observable<string> {
    return this.http.post<string>(this.base, command);
  }

  update(command: UpdateDivisionCommand): Observable<void> {
    return this.http.put<void>(`${this.base}/${command.id}`, command);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
