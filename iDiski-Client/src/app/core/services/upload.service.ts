import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface UploadResponse {
  fileUrl: string;
}

@Injectable({ providedIn: 'root' })
export class UploadService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/uploads`;

  /**
   * Upload an image file
   * @param file The file to upload
   * @param folder The folder to store it in (e.g., 'teams', 'players', 'sponsors')
   */
  uploadImage(file: File, folder: string): Observable<UploadResponse> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('folder', folder);

    return this.http.post<UploadResponse>(this.base, formData);
  }

  /**
   * Delete an uploaded file
   * @param fileUrl The URL of the file to delete
   */
  deleteFile(fileUrl: string): Observable<void> {
    return this.http.delete<void>(`${this.base}?fileUrl=${encodeURIComponent(fileUrl)}`);
  }
}
