import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ArticleService } from '../../core/services/article.service';
import { ArticleSummaryDto } from '../../core/models';
import { getImageUrl } from '../../core/utils/image.utils';

/**
 * Articles for one division or team, mirroring the videos section: the newest is shown
 * in full-width card form and the rest are titles that swap into it. Renders nothing
 * when there are no articles.
 */
@Component({
  selector: 'app-scoped-articles',
  imports: [CommonModule, RouterLink, DatePipe],
  template: `
    @if (articles().length > 0) {
      <section class="card shadow-sm mb-4">
        <div class="card-body">
          <h2 class="card-title h4 mb-4">
            <i class="bi bi-newspaper text-primary me-2"></i>{{ heading() }}
          </h2>

          @if (selected(); as current) {
            @if (getImageUrl(current.featuredImageUrl || current.coverImageUrl); as image) {
              <img
                [src]="image"
                [alt]="current.title"
                class="img-fluid rounded mb-3 w-100"
                style="max-height: 320px; object-fit: cover"
              />
            }

            <h3 class="h4 fw-bold mb-2">{{ current.title }}</h3>

            <p class="text-muted small mb-3">
              {{ current.author }}
              @if (current.publishedAt) {
                · {{ current.publishedAt | date: 'mediumDate' }}
              }
            </p>

            @if (current.excerpt) {
              <p class="mb-3">{{ current.excerpt }}</p>
            }

            <a [routerLink]="['/news', current.slug]" class="btn btn-outline-primary btn-sm">
              Read full article
            </a>
          }

          @if (others().length > 0) {
            <hr class="my-4" />
            <h4 class="h6 text-uppercase text-muted mb-3">More stories</h4>
            <ul class="list-unstyled mb-0">
              @for (article of others(); track article.id) {
                <li class="mb-2">
                  <button type="button" class="btn btn-link p-0 text-start" (click)="select(article)">
                    {{ article.title }}
                  </button>
                </li>
              }
            </ul>
          }
        </div>
      </section>
    }
  `,
})
export class ScopedArticlesComponent implements OnInit {
  readonly divisionId = input<string | undefined>(undefined);
  readonly teamId = input<string | undefined>(undefined);
  readonly playerId = input<string | undefined>(undefined);
  readonly heading = input<string>('Latest News');
  readonly limit = input<number>(10);

  private readonly articleService = inject(ArticleService);

  articles = signal<ArticleSummaryDto[]>([]);
  selected = signal<ArticleSummaryDto | null>(null);

  getImageUrl = getImageUrl;

  others = computed(() => {
    const current = this.selected();
    return this.articles().filter((article) => article.id !== current?.id);
  });

  ngOnInit(): void {
    // Same reasoning as the videos section: an unscoped request would pull the whole site.
    if (!this.divisionId() && !this.teamId() && !this.playerId()) return;

    this.articleService
      .getPublished({
        pageSize: this.limit(),
        divisionId: this.divisionId(),
        teamId: this.teamId(),
        playerId: this.playerId(),
      })
      .subscribe({
        next: (page) => {
          this.articles.set(page.items);
          this.selected.set(page.items[0] ?? null);
        },
        error: (err) => console.error('Failed to load articles:', err),
      });
  }

  select(article: ArticleSummaryDto): void {
    this.selected.set(article);
  }
}
