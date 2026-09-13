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
  divisionId?: string | null;
  teamId?: string | null;
  playerId?: string | null;
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
  /** Admin lists only: the tagged player has left the team this was made about. */
  isLocked?: boolean;
}

export interface CreateVideoRequest {
  title: string;
  videoUrl: string;
  description?: string;
  thumbnailUrl?: string;
  author: string;
  publishImmediately: boolean;
  divisionId?: string | null;
  teamId?: string | null;
  playerId?: string | null;
}

export interface UpdateVideoRequest {
  id: string;
  title: string;
  videoUrl: string;
  description?: string;
  thumbnailUrl?: string;
  author: string;
  isPinned?: boolean;
  divisionId?: string | null;
  teamId?: string | null;
  playerId?: string | null;
}
