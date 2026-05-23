export interface VideoDto {
  id: string;
  title: string;
  videoUrl: string;
  description?: string;
  thumbnailUrl?: string;
  author: string;
  isPublished: boolean;
  publishedAt?: string;
  isPinned: boolean;
  viewCount: number;
}

export interface VideoSummaryDto {
  id: string;
  title: string;
  videoUrl: string;
  description?: string;
  thumbnailUrl?: string;
  author: string;
  publishedAt?: string;
  isPinned: boolean;
}

export interface CreateVideoRequest {
  title: string;
  videoUrl: string;
  description?: string;
  thumbnailUrl?: string;
  author: string;
  publishImmediately: boolean;
}

export interface UpdateVideoRequest {
  id: string;
  title: string;
  videoUrl: string;
  description?: string;
  thumbnailUrl?: string;
  author: string;
  isPinned?: boolean;
}
