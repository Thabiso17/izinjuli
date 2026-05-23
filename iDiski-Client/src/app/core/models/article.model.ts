// src/app/core/models/article.model.ts

/**
 * Lightweight card variant returned by the article list endpoints.
 * Does NOT include the full Content body — use ArticleDto for that.
 */
export interface ArticleSummaryDto {
  id: string;
  title: string;
  /** URL-friendly slug, e.g. "player-of-the-month-march-2025" */
  slug: string;
  excerpt: string | null;
  coverImageUrl: string | null;
  /** YouTube video ID or embed URL */
  videoUrl: string | null;
  featuredImageUrl: string | null;
  author: string;
  publishedAt: string | null; // ISO 8601 — pipe through DatePipe in templates
  tags: string[];
  isPinned: boolean;
}

/**
 * Full article returned by GET /api/articles/:slug.
 * Content is Markdown — pass through a Markdown renderer in the Angular template.
 */
export interface ArticleDto {
  id: string;
  title: string;
  slug: string;
  /** Full Markdown body */
  content: string;
  excerpt: string | null;
  coverImageUrl: string | null;
  /** YouTube video ID or embed URL for rich media content */
  videoUrl: string | null;
  /** High-quality banner image URL for featured articles */
  featuredImageUrl: string | null;
  author: string;
  isPublished: boolean;
  publishedAt: string | null;
  tags: string[];
  isPinned: boolean;
  viewCount: number;
  createdAt: string;
  updatedAt: string | null;
}

/** Returned by POST /api/articles — contains both the ID and the auto-generated slug. */
export interface CreateArticleResult {
  id: string;
  slug: string;
}

/** Request body for POST /api/articles */
export interface CreateArticleRequest {
  title: string;
  content: string;
  excerpt?: string;
  coverImageUrl?: string;
  videoUrl?: string;
  featuredImageUrl?: string;
  author: string;
  tags: string[];
  publishImmediately: boolean;
}

/** Request body for PUT /api/articles/:id */
export interface UpdateArticleRequest {
  id: string;
  title: string;
  content: string;
  excerpt?: string;
  coverImageUrl?: string;
  videoUrl?: string;
  featuredImageUrl?: string;
  author: string;
  tags: string[];
  isPinned?: boolean;
}
