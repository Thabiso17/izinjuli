import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SuspensionDto, CreateSuspensionCommand } from '../models';

@Injectable({ providedIn: 'root' })
export class SuspensionService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/suspensions`;

  getActive(divisionId?: string): Observable<SuspensionDto[]> {
    let params = new HttpParams();
    if (divisionId) params = params.set('divisionId', divisionId);
    return this.http.get<SuspensionDto[]>(`${this.base}/active`, { params });
  }

  getPlayerHistory(playerId: string): Observable<SuspensionDto[]> {
    return this.http.get<SuspensionDto[]>(`${this.base}/player/${playerId}`);
  }

  create(command: CreateSuspensionCommand): Observable<string> {
    return this.http.post<string>(this.base, command);
  }
}
