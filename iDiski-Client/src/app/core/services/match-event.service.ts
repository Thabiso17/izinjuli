import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { MatchEventDto, RecordMatchEventsCommand } from '../models';

@Injectable({ providedIn: 'root' })
export class MatchEventService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/matchevents`;

  getByMatch(matchId: string): Observable<MatchEventDto[]> {
    return this.http.get<MatchEventDto[]>(`${this.base}/match/${matchId}`);
  }

  getByPlayer(playerId: string, season?: number): Observable<MatchEventDto[]> {
    let params = new HttpParams();
    if (season) params = params.set('season', season.toString());
    return this.http.get<MatchEventDto[]>(`${this.base}/player/${playerId}`, { params });
  }

  recordEvents(command: RecordMatchEventsCommand): Observable<void> {
    return this.http.post<void>(this.base, command);
  }
}
