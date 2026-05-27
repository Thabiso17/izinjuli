import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ArticleAttachmentDto } from '../models';

@Injectable({ providedIn: 'root' })
export class ArticleAttachmentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getAttachments(articleId: string): Observable<ArticleAttachmentDto[]> {
    return this.http.get<ArticleAttachmentDto[]>(
      `${this.baseUrl}/articles/${articleId}/attachments`
    );
  }

  uploadAttachment(
    articleId: string,
    file: File,
    caption?: string,
    displayOrder: number = 0
  ): Observable<ArticleAttachmentDto> {
    const formData = new FormData();
    formData.append('file', file);
    if (caption) {
      formData.append('caption', caption);
    }
    formData.append('displayOrder', displayOrder.toString());

    return this.http.post<ArticleAttachmentDto>(
      `${this.baseUrl}/articles/${articleId}/attachments`,
      formData
    );
  }

  deleteAttachment(articleId: string, attachmentId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/articles/${articleId}/attachments/${attachmentId}`
    );
  }
}
