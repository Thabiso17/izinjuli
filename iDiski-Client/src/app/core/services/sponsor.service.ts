import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SponsorDto, AdPlacement } from '../models';

@Injectable({ providedIn: 'root' })
export class SponsorService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/sponsors`;

  /**
   * Returns active sponsors for a given ad placement.
   * @param placement One of: Header | Sidebar | Footer | MatchDay | Homepage | NewsPage
   */
  getByPlacement(placement: AdPlacement): Observable<SponsorDto[]> {
    const params = new HttpParams().set('placement', placement.toString());
    return this.http.get<SponsorDto[]>(this.base, { params });
  }
}
