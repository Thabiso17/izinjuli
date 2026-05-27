export interface ArticleAttachmentDto {
  id: string;
  fileName: string;
  fileUrl: string;
  type: 'PDF' | 'Image';
  fileSizeBytes: number;
  caption?: string;
  displayOrder: number;
}
