import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  PageLayoutConfigDto,
  UpsertPageLayoutCommand,
  BulkUpdatePageLayoutCommand,
} from '../models';

@Injectable({ providedIn: 'root' })
export class PageLayoutService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/pagelayoutconfigs`;

  /**
   * Returns the ordered component list for a page.
   * @param pageName Page identifier (e.g., 'main', 'matches', 'news')
   */
  getLayout(pageName: string): Observable<PageLayoutConfigDto[]> {
    const params = new HttpParams().set('pageName', pageName);
    return this.http.get<PageLayoutConfigDto[]>(this.base, { params });
  }

  /**
   * Upserts a single component's layout config.
   * @returns The entity ID (Guid)
   */
  upsert(command: UpsertPageLayoutCommand): Observable<string> {
    return this.http.put<string>(this.base, command);
  }

  /**
   * Replaces the entire layout for a page in one bulk operation.
   * Use this after drag-and-drop reorder.
   */
  bulkUpdate(command: BulkUpdatePageLayoutCommand): Observable<void> {
    return this.http.put<void>(`${this.base}/bulk`, command);
  }
}
