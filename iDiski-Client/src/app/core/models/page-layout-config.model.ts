export interface PageLayoutConfigDto {
  id: string;
  pageName: string;
  componentName: string;
  displayOrder: number;
  isVisible: boolean;
  configJson: string | null;
  modifiedByUser: string;
}

export interface UpsertPageLayoutCommand {
  pageName: string;
  componentName: string;
  displayOrder: number;
  isVisible: boolean;
  configJson?: string;
  modifiedByUser: string;
}

export interface BulkUpdatePageLayoutCommand {
  pageName: string;
  modifiedByUser: string;
  components: UpsertPageLayoutCommand[];
}

// Zone enum for layout editor
export type LayoutZone = 'main' | 'sidebar';

// Extended interface for editor state
export interface PageLayoutConfigEditorItem extends PageLayoutConfigDto {
  zone: LayoutZone;
}
