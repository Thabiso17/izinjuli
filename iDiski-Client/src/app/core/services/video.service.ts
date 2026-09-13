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
    divisionId?: string;
    teamId?: string;
    playerId?: string;
  } = {}): Observable<VideoSummaryDto[]> {
    let params = new HttpParams();
    // The API parameter is `limit`; sending maxResults meant the cap was silently ignored.
    if (options.maxResults) params = params.set('limit', options.maxResults);
    if (options.divisionId) params = params.set('divisionId', options.divisionId);
    if (options.teamId)     params = params.set('teamId', options.teamId);
    if (options.playerId)   params = params.set('playerId', options.playerId);
    return this.http.get<VideoSummaryDto[]>(this.base, { params });
  }

  /**
   * Returns a single published video by ID.
   */
  /** Retires a video from public view without destroying it. */
  archive(id: string): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/archive`, {});
  }

  /** Restores an archived video to public view. */
  unarchive(id: string): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/unarchive`, {});
  }

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
