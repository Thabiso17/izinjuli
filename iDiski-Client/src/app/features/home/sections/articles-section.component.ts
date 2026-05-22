import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ArticleService } from '../../../core/services/article.service';
import { ArticleSummaryDto } from '../../../core/models';

@Component({
  selector: 'app-articles-section',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="card shadow-sm mb-4">
      <div class="card-body">
        <h2 class="card-title h4 mb-4">
          <i class="bi bi-newspaper me-2"></i>Latest News
        </h2>

        @if (loading()) {
          <div class="text-center py-4">
            <div class="spinner-border spinner-border-sm text-primary" role="status">
              <span class="visually-hidden">Loading...</span>
            </div>
          </div>
        }

        @if (!loading() && articles().length > 0) {
          @for (article of articles(); track article.id) {
            <article class="article-card d-flex mb-3 pb-3 border-bottom" (click)="navigateToArticle(article.slug)">
              @if (article.coverImageUrl || article.featuredImageUrl) {
                <img
                  [src]="article.coverImageUrl || article.featuredImageUrl"
                  [alt]="article.title"
                  style="width: 96px; height: 96px; min-width: 96px; object-fit: cover"
                  class="rounded me-3"
                />
              } @else {
                <div style="width: 96px; height: 96px; min-width: 96px;" class="bg-secondary rounded me-3"></div>
              }
              <div>
                <h5 class="fw-semibold mb-2">{{ article.title }}</h5>
                @if (article.excerpt) {
                  <p class="text-muted small mb-2">{{ article.excerpt }}</p>
                }
                <div class="d-flex gap-3">
                  <span class="text-muted" style="font-size: 0.75rem;">
                    {{ article.publishedAt | date: 'MMM d, y' }}
                  </span>
                </div>
              </div>
            </article>
          }
        }

        @if (!loading() && articles().length === 0) {
          <div class="text-center py-4 text-muted">
            <i class="bi bi-inbox display-4"></i>
            <p class="mt-2">No news articles available</p>
          </div>
        }

        @if (error()) {
          <div class="alert alert-danger" role="alert">
            {{ error() }}
          </div>
        }
      </div>
    </section>
  `,
  styles: [`
    .article-card {
      cursor: pointer;
      transition: background-color 0.2s;
    }
    .article-card:hover {
      background-color: #f8f9fa;
    }
  `]
})
export class ArticlesSectionComponent implements OnInit {
  private articleService = inject(ArticleService);
  private router = inject(Router);

  articles = signal<ArticleSummaryDto[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  ngOnInit() {
    this.loadArticles();
  }

  loadArticles() {
    this.loading.set(true);

    this.articleService.getPublished({ pageSize: 3, pageNumber: 1 }).subscribe({
      next: (data) => {
        this.articles.set(data.items);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load articles');
        this.loading.set(false);
        console.error('Error loading articles:', err);
      },
    });
  }

  navigateToArticle(slug: string) {
    this.router.navigate(['/news', slug]);
  }
}
