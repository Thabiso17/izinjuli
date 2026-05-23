// src/app/core/services/video.service.ts

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  VideoDto,
  VideoSummaryDto,
  CreateVideoRequest,
  UpdateVideoRequest,
} from '../models/video.model';

@Injectable({ providedIn: 'root' })
export class VideoService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/videos`;

  // ── PUBLIC ─────────────────────────────────────────────────────────────────

  /**
   * Returns published videos for public homepage.
   * Pinned videos come first, then by most recent.
   */
  getPublished(options: {
    maxResults?: number;
  } = {}): Observable<VideoSummaryDto[]> {
    let params = new HttpParams();
    if (options.maxResults) params = params.set('maxResults', options.maxResults);
    return this.http.get<VideoSummaryDto[]>(this.base, { params });
  }

  /**
   * Returns a single published video by ID.
   */
  getById(id: string): Observable<VideoDto> {
    return this.http.get<VideoDto>(`${this.base}/${id}`);
  }

  // ── ADMIN ──────────────────────────────────────────────────────────────────

  /**
   * [Admin] Returns all videos including unpublished.
   */
  getAllAdmin(options: {
    publishedOnly?: boolean;
  } = {}): Observable<VideoSummaryDto[]> {
    let params = new HttpParams();
    if (options.publishedOnly != null)
      params = params.set('publishedOnly', options.publishedOnly);
    return this.http.get<VideoSummaryDto[]>(`${this.base}/admin`, { params });
  }

  /**
   * Creates a new video.
   * Returns the video ID.
   */
  create(request: CreateVideoRequest): Observable<string> {
    return this.http.post<string>(this.base, request);
  }

  /**
   * Updates video content.
   */
  update(id: string, request: UpdateVideoRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}`, request);
  }

  /**
   * Publishes a video.
   */
  publish(id: string): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/publish`, {});
  }

  /**
   * Unpublishes a video.
   */
  unpublish(id: string): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/unpublish`, {});
  }

  /**
   * Toggles pinned status.
   */
  togglePin(id: string): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/toggle-pin`, {});
  }

  /**
   * Permanently deletes an unpublished video.
   */
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
