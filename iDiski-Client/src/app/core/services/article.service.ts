// src/app/core/services/article.service.ts

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ArticleDto,
  ArticleSummaryDto,
  CreateArticleRequest,
  CreateArticleResult,
  UpdateArticleRequest,
  PaginatedList,
} from '../models';

@Injectable({ providedIn: 'root' })
export class ArticleService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/articles`;

  // ── PUBLIC ─────────────────────────────────────────────────────────────────

  /**
   * Returns a paginated list of published articles.
   *
   * @param tag         Optional tag filter, e.g. 'Player of the Month', 'Awards'.
   * @param authorName  Optional author filter.
   * @param pageNumber  1-based page number (default 1).
   * @param pageSize    Items per page (default 10).
   *
   * @example
   * // Angular template — awards section
   * articles$ = this.articleService.getPublished({ tag: 'Player of the Month' });
   */
  getPublished(options: {
    tag?: string;
    authorName?: string;
    pageNumber?: number;
    pageSize?: number;
  } = {}): Observable<PaginatedList<ArticleSummaryDto>> {
    let params = new HttpParams();
    if (options.tag)        params = params.set('tag', options.tag);
    if (options.authorName) params = params.set('authorName', options.authorName);
    if (options.pageNumber) params = params.set('pageNumber', options.pageNumber);
    if (options.pageSize)   params = params.set('pageSize', options.pageSize);
    return this.http.get<PaginatedList<ArticleSummaryDto>>(this.base, { params });
  }

  /**
   * Returns a single published article by URL slug.
   * Call this from your Angular route resolver or component on /news/:slug.
   */
  getBySlug(slug: string): Observable<ArticleDto> {
    return this.http.get<ArticleDto>(`${this.base}/${slug}`);
  }

  // ── ADMIN ──────────────────────────────────────────────────────────────────

  /**
   * [Admin] Returns all articles including drafts.
   */
  getAllAdmin(options: {
    publishedOnly?: boolean;
    pageNumber?: number;
    pageSize?: number;
  } = {}): Observable<PaginatedList<ArticleSummaryDto>> {
    let params = new HttpParams();
    if (options.publishedOnly != null)
      params = params.set('publishedOnly', options.publishedOnly);
    if (options.pageNumber) params = params.set('pageNumber', options.pageNumber);
    if (options.pageSize)   params = params.set('pageSize', options.pageSize);
    return this.http.get<PaginatedList<ArticleSummaryDto>>(`${this.base}/admin`, { params });
  }

  /**
   * Creates a new article.
   * The SEO slug is auto-generated server-side from the title —
   * do NOT send a slug field.
   *
   * Returns the generated ID and slug so you can redirect to /news/:slug.
   *
   * @example
   * this.articleService.create({
   *   title: 'Player of the Month – March 2025',
   *   content: '## Lebo Molefe shines...',
   *   author: 'iDiski Editorial',
   *   tags: ['Awards', 'Player of the Month', 'Midfielders'],
   *   publishImmediately: true,
   * }).subscribe(result => this.router.navigate(['/news', result.slug]));
   */
  create(request: CreateArticleRequest): Observable<CreateArticleResult> {
    return this.http.post<CreateArticleResult>(this.base, request);
  }

  /**
   * Updates article content. The slug is immutable after creation.
   */
  update(id: string, request: UpdateArticleRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}`, request);
  }

  /**
   * Publishes a draft article. Returns 409 if already published.
   */
  publish(id: string): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/publish`, {});
  }

  /**
   * Retracts a published article back to draft status.
   * Must be called before delete.
   */
  unpublish(id: string): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/unpublish`, {});
  }

  /**
   * Permanently deletes a draft article.
   * Will return 409 if the article is still published — call unpublish() first.
   */
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
