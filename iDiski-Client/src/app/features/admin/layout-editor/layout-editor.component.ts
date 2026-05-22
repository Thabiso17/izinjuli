import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CdkDragDrop, DragDropModule, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { PageLayoutService } from '../../../core/services/page-layout.service';
import { ComponentRegistryService } from '../../home/services/component-registry.service';
import { PageLayoutConfigDto, LayoutZone, PageLayoutConfigEditorItem } from '../../../core/models';

@Component({
  selector: 'app-layout-editor',
  standalone: true,
  imports: [CommonModule, DragDropModule],
  template: `
    <div class="container mx-auto px-4 py-8">
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-gray-900">Page Layout Editor</h1>
        <p class="text-gray-600 mt-2">
          Drag and drop components to reorder or move between zones. Toggle visibility and save changes.
        </p>
      </div>

      @if (loading()) {
        <div class="text-center py-12">
          <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
        </div>
      } @else if (error()) {
        <div class="bg-red-50 border border-red-200 rounded-lg p-6">
          <p class="text-red-800">{{ error() }}</p>
        </div>
      } @else {
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-6">
          <!-- Main Zone -->
          <div class="lg:col-span-2">
            <div class="bg-white rounded-lg shadow-md p-6">
              <h2 class="text-xl font-bold mb-4 flex items-center">
                <span class="w-3 h-3 bg-blue-600 rounded-full mr-2"></span>
                Main Content Zone
              </h2>
              <div
                cdkDropList
                #mainList="cdkDropList"
                [cdkDropListData]="mainZoneItems()"
                [cdkDropListConnectedTo]="[sidebarList]"
                (cdkDropListDropped)="onDrop($event, 'main')"
                class="min-h-[200px] space-y-3"
              >
                @for (item of mainZoneItems(); track item.id) {
                  <div
                    cdkDrag
                    class="bg-gray-50 border-2 border-gray-200 rounded-lg p-4 cursor-move hover:border-blue-400 transition-colors"
                    [class.opacity-50]="!item.isVisible"
                  >
                    <div class="flex items-center justify-between">
                      <div class="flex items-center space-x-3">
                        <svg class="w-5 h-5 text-gray-400" fill="currentColor" viewBox="0 0 20 20">
                          <path d="M10 6a2 2 0 110-4 2 2 0 010 4zM10 12a2 2 0 110-4 2 2 0 010 4zM10 18a2 2 0 110-4 2 2 0 010 4z"></path>
                        </svg>
                        <span class="font-semibold text-gray-800">{{ item.componentName }}</span>
                        @if (!item.isVisible) {
                          <span class="text-xs bg-gray-300 text-gray-700 px-2 py-1 rounded">Hidden</span>
                        }
                      </div>
                      <button
                        (click)="toggleVisibility(item)"
                        class="px-3 py-1 text-sm rounded transition-colors"
                        [class.bg-green-100]="item.isVisible"
                        [class.text-green-700]="item.isVisible"
                        [class.bg-gray-200]="!item.isVisible"
                        [class.text-gray-600]="!item.isVisible"
                      >
                        {{ item.isVisible ? 'Visible' : 'Hidden' }}
                      </button>
                    </div>
                  </div>
                }
                @empty {
                  <div class="text-center py-8 text-gray-400 border-2 border-dashed border-gray-300 rounded-lg">
                    Drop components here
                  </div>
                }
              </div>
            </div>
          </div>

          <!-- Sidebar Zone -->
          <div>
            <div class="bg-white rounded-lg shadow-md p-6">
              <h2 class="text-xl font-bold mb-4 flex items-center">
                <span class="w-3 h-3 bg-purple-600 rounded-full mr-2"></span>
                Sidebar Zone
              </h2>
              <div
                cdkDropList
                #sidebarList="cdkDropList"
                [cdkDropListData]="sidebarZoneItems()"
                [cdkDropListConnectedTo]="[mainList]"
                (cdkDropListDropped)="onDrop($event, 'sidebar')"
                class="min-h-[200px] space-y-3"
              >
                @for (item of sidebarZoneItems(); track item.id) {
                  <div
                    cdkDrag
                    class="bg-gray-50 border-2 border-gray-200 rounded-lg p-4 cursor-move hover:border-purple-400 transition-colors"
                    [class.opacity-50]="!item.isVisible"
                  >
                    <div class="flex items-center justify-between">
                      <div class="flex items-center space-x-3">
                        <svg class="w-5 h-5 text-gray-400" fill="currentColor" viewBox="0 0 20 20">
                          <path d="M10 6a2 2 0 110-4 2 2 0 010 4zM10 12a2 2 0 110-4 2 2 0 010 4zM10 18a2 2 0 110-4 2 2 0 010 4z"></path>
                        </svg>
                        <span class="font-semibold text-gray-800">{{ item.componentName }}</span>
                        @if (!item.isVisible) {
                          <span class="text-xs bg-gray-300 text-gray-700 px-2 py-1 rounded">Hidden</span>
                        }
                      </div>
                      <button
                        (click)="toggleVisibility(item)"
                        class="px-3 py-1 text-sm rounded transition-colors"
                        [class.bg-green-100]="item.isVisible"
                        [class.text-green-700]="item.isVisible"
                        [class.bg-gray-200]="!item.isVisible"
                        [class.text-gray-600]="!item.isVisible"
                      >
                        {{ item.isVisible ? 'Visible' : 'Hidden' }}
                      </button>
                    </div>
                  </div>
                }
                @empty {
                  <div class="text-center py-8 text-gray-400 border-2 border-dashed border-gray-300 rounded-lg">
                    Drop components here
                  </div>
                }
              </div>
            </div>
          </div>
        </div>

        <!-- Action Buttons -->
        <div class="bg-white rounded-lg shadow-md p-6 flex justify-between items-center">
          <div>
            @if (hasUnsavedChanges()) {
              <span class="text-orange-600 font-semibold">Unsaved changes</span>
            } @else {
              <span class="text-green-600">All changes saved</span>
            }
          </div>
          <div class="space-x-3">
            <button
              (click)="resetChanges()"
              [disabled]="!hasUnsavedChanges() || saving()"
              class="px-4 py-2 border border-gray-300 rounded text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Reset
            </button>
            <button
              (click)="saveLayout()"
              [disabled]="!hasUnsavedChanges() || saving()"
              class="px-6 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {{ saving() ? 'Saving...' : 'Save Layout' }}
            </button>
          </div>
        </div>

        @if (saveSuccess()) {
          <div class="mt-4 bg-green-50 border border-green-200 rounded-lg p-4 text-green-800">
            Layout saved successfully!
          </div>
        }
      }
    </div>
  `,
  styles: [`
    .cdk-drag-preview {
      box-shadow: 0 5px 15px rgba(0,0,0,0.3);
      opacity: 0.9;
    }

    .cdk-drag-animating {
      transition: transform 250ms cubic-bezier(0, 0, 0.2, 1);
    }

    .cdk-drop-list-dragging .cdk-drag {
      transition: transform 250ms cubic-bezier(0, 0, 0.2, 1);
    }
  `],
})
export class LayoutEditorComponent implements OnInit {
  private readonly layoutService = inject(PageLayoutService);
  private readonly registry = inject(ComponentRegistryService);

  loading = signal(true);
  saving = signal(false);
  error = signal<string | null>(null);
  saveSuccess = signal(false);
  hasUnsavedChanges = signal(false);

  mainZoneItems = signal<PageLayoutConfigEditorItem[]>([]);
  sidebarZoneItems = signal<PageLayoutConfigEditorItem[]>([]);

  private originalConfig: PageLayoutConfigDto[] = [];

  ngOnInit(): void {
    this.loadLayout();
  }

  loadLayout(): void {
    this.loading.set(true);
    this.error.set(null);

    this.layoutService.getLayout('main').subscribe({
      next: (configs) => {
        this.originalConfig = configs;
        this.buildEditorState(configs);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load layout:', err);
        this.error.set('Failed to load layout configuration.');
        this.loading.set(false);
      },
    });
  }

  private buildEditorState(configs: PageLayoutConfigDto[]): void {
    const items: PageLayoutConfigEditorItem[] = configs.map((config) => {
      const entry = this.registry.getComponent(config.componentName);
      return {
        ...config,
        zone: entry?.defaultZone || 'main',
      };
    });

    this.mainZoneItems.set(items.filter((item) => item.zone === 'main'));
    this.sidebarZoneItems.set(items.filter((item) => item.zone === 'sidebar'));
    this.hasUnsavedChanges.set(false);
  }

  onDrop(event: CdkDragDrop<PageLayoutConfigEditorItem[]>, targetZone: LayoutZone): void {
    if (event.previousContainer === event.container) {
      // Reorder within same zone
      const items = [...event.container.data];
      moveItemInArray(items, event.previousIndex, event.currentIndex);

      if (targetZone === 'main') {
        this.mainZoneItems.set(items);
      } else {
        this.sidebarZoneItems.set(items);
      }
    } else {
      // Transfer between zones
      const sourceItems = [...event.previousContainer.data];
      const targetItems = [...event.container.data];

      transferArrayItem(sourceItems, targetItems, event.previousIndex, event.currentIndex);

      // Update zone property
      targetItems[event.currentIndex].zone = targetZone;

      if (targetZone === 'main') {
        this.mainZoneItems.set(targetItems);
        this.sidebarZoneItems.set(sourceItems);
      } else {
        this.sidebarZoneItems.set(targetItems);
        this.mainZoneItems.set(sourceItems);
      }
    }

    this.hasUnsavedChanges.set(true);
    this.saveSuccess.set(false);
  }

  toggleVisibility(item: PageLayoutConfigEditorItem): void {
    item.isVisible = !item.isVisible;

    // Trigger change detection by creating new arrays
    this.mainZoneItems.set([...this.mainZoneItems()]);
    this.sidebarZoneItems.set([...this.sidebarZoneItems()]);

    this.hasUnsavedChanges.set(true);
    this.saveSuccess.set(false);
  }

  saveLayout(): void {
    this.saving.set(true);
    this.saveSuccess.set(false);

    // Combine zones and assign display orders
    const allItems = [
      ...this.mainZoneItems().map((item, index) => ({ ...item, displayOrder: index })),
      ...this.sidebarZoneItems().map((item, index) => ({ ...item, displayOrder: index })),
    ];

    const command = {
      pageName: 'main',
      modifiedByUser: 'admin', // TODO: Replace with actual user from auth service
      components: allItems.map((item) => ({
        pageName: 'main',
        componentName: item.componentName,
        displayOrder: item.displayOrder,
        isVisible: item.isVisible,
        configJson: item.configJson || undefined,
        modifiedByUser: 'admin',
      })),
    };

    this.layoutService.bulkUpdate(command).subscribe({
      next: () => {
        this.saving.set(false);
        this.hasUnsavedChanges.set(false);
        this.saveSuccess.set(true);

        // Hide success message after 3 seconds
        setTimeout(() => this.saveSuccess.set(false), 3000);
      },
      error: (err) => {
        console.error('Failed to save layout:', err);
        this.error.set('Failed to save layout. Please try again.');
        this.saving.set(false);
      },
    });
  }

  resetChanges(): void {
    this.buildEditorState(this.originalConfig);
    this.saveSuccess.set(false);
  }
}
