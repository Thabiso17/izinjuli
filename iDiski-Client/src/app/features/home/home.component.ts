import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, NgComponentOutlet } from '@angular/common';
import { PageLayoutService } from '../../core/services/page-layout.service';
import { ComponentRegistryService } from './services/component-registry.service';
import { PageLayoutConfigDto } from '../../core/models';

interface RenderedSection {
  componentType: any;
  config: PageLayoutConfigDto;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, NgComponentOutlet],
  template: `
    <div class="container py-4">
      @if (loading()) {
        <div class="text-center py-5">
          <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Loading...</span>
          </div>
          <p class="mt-3 text-muted">Loading...</p>
        </div>
      } @else if (error()) {
        <div class="alert alert-danger text-center">
          <p class="mb-3">{{ error() }}</p>
          <button (click)="loadLayout()" class="btn btn-danger">
            Retry
          </button>
        </div>
      } @else {
        <div class="row">
          <!-- Main Content Area (2/3 width on large screens) -->
          <div class="col-lg-8">
            @for (section of mainSections(); track section.config.id) {
              <ng-container *ngComponentOutlet="section.componentType" />
            }
            @empty {
              <div class="alert alert-secondary text-center">
                No content configured for the main area.
              </div>
            }
          </div>

          <!-- Sidebar (1/3 width on large screens) -->
          <aside class="col-lg-4">
            @for (section of sidebarSections(); track section.config.id) {
              <ng-container *ngComponentOutlet="section.componentType" />
            }
            @empty {
              <div class="alert alert-secondary text-center">
                No sidebar content configured.
              </div>
            }
          </aside>
        </div>
      }
    </div>
  `,
})
export class HomeComponent implements OnInit {
  private readonly layoutService = inject(PageLayoutService);
  private readonly registry = inject(ComponentRegistryService);

  loading = signal(true);
  error = signal<string | null>(null);
  mainSections = signal<RenderedSection[]>([]);
  sidebarSections = signal<RenderedSection[]>([]);

  ngOnInit(): void {
    this.loadLayout();
  }

  loadLayout(): void {
    this.loading.set(true);
    this.error.set(null);

    this.layoutService.getLayout('main').subscribe({
      next: (configs) => {
        const sections = this.buildSections(
          configs.length > 0 ? configs : this.defaultLayout(),
        );

        // Separate into main and sidebar zones
        const main = sections.filter(s => {
          const entry = this.registry.getComponent(s.config.componentName);
          return entry?.defaultZone === 'main';
        });

        const sidebar = sections.filter(s => {
          const entry = this.registry.getComponent(s.config.componentName);
          return entry?.defaultZone === 'sidebar';
        });

        this.mainSections.set(main);
        this.sidebarSections.set(sidebar);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load page layout:', err);
        this.error.set('Failed to load page layout. Please try again.');
        this.loading.set(false);
      },
    });
  }

  /**
   * What to show when nothing has been configured at all.
   *
   * The homepage is built entirely from saved layout rows, so an empty table rendered an
   * empty page — no fixtures, no standings, no articles — and the editor could not fix it,
   * because it too listed only saved rows. A fresh install showed visitors nothing.
   *
   * This is the fallback rather than a write: the first save from the layout editor puts
   * real rows in, and from then on the editor is in charge. Only a completely empty table
   * falls back, so an administrator who has deliberately hidden every section keeps their
   * blank page.
   */
  private defaultLayout(): PageLayoutConfigDto[] {
    return this.registry.getAllComponentNames().map((componentName, index) => ({
      id: '',
      pageName: 'main',
      componentName,
      displayOrder: index,
      isVisible: true,
      configJson: null,
      modifiedByUser: '',
    }));
  }

  private buildSections(configs: PageLayoutConfigDto[]): RenderedSection[] {
    return configs
      .filter((config) => config.isVisible)
      .map((config) => {
        const entry = this.registry.getComponent(config.componentName);
        if (!entry) {
          console.warn(`Component not found in registry: ${config.componentName}`);
          return null;
        }
        return { componentType: entry.component, config };
      })
      .filter((section): section is RenderedSection => section !== null);
  }
}
