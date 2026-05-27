import { environment } from '../../../environments/environment';

/**
 * Converts a relative image URL to an absolute URL using the API base URL.
 * If the URL is already absolute, returns it as-is.
 * If the URL is null/empty, returns null.
 */
export function getImageUrl(url: string | null | undefined): string | null {
  if (!url) return null;

  // If it's already a full URL, return as-is
  if (url.startsWith('http://') || url.startsWith('https://')) {
    return url;
  }

  // Get the API server URL without the /api suffix
  const apiBase = environment.apiBaseUrl; // e.g., "https://izinjuli-production.up.railway.app/api"
  const serverUrl = apiBase.substring(0, apiBase.lastIndexOf('/api')); // e.g., "https://izinjuli-production.up.railway.app"

  // Ensure url starts with /
  const path = url.startsWith('/') ? url : `/${url}`;

  return `${serverUrl}${path}`;
}
