import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ClearDataService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/clear-data`;

  /** Removes every player on a team. Returns the number of players removed. */
  clearPlayers(teamId: string): Observable<number> {
    return this.http.delete<number>(`${this.base}/players/team/${teamId}`);
  }

  /** Permanently removes a team and all of its players and match history. */
  clearTeam(teamId: string): Observable<number> {
    return this.http.delete<number>(`${this.base}/team/${teamId}`);
  }

  /** Permanently removes a division and everything nested under it. Returns the number of teams removed. */
  clearDivision(divisionId: string): Observable<number> {
    return this.http.delete<number>(`${this.base}/division/${divisionId}`);
  }
}
