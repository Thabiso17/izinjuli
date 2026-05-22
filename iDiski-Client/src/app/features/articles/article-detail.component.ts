import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ArticleService } from '../../core/services/article.service';
import { ArticleDto } from '../../core/models';

@Component({
  selector: 'app-article-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="container py-4">
      @if (loading()) {
        <div class="text-center py-5">
          <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Loading...</span>
          </div>
        </div>
      } @else if (error()) {
        <div class="alert alert-danger">
          <h4>Error</h4>
          <p>{{ error() }}</p>
          <button class="btn btn-primary" routerLink="/">Go Home</button>
        </div>
      } @else if (article()) {
        <div class="row">
          <div class="col-lg-8 mx-auto">
            <!-- Back button -->
            <button class="btn btn-link ps-0 mb-3" (click)="goBack()">
              <i class="bi bi-arrow-left me-2"></i>Back
            </button>

            <!-- Article header -->
            <article>
              <h1 class="display-5 fw-bold mb-3">{{ article()!.title }}</h1>

              <div class="d-flex gap-3 mb-4 text-muted">
                <span><i class="bi bi-person me-1"></i>{{ article()!.author }}</span>
                <span><i class="bi bi-calendar me-1"></i>{{ article()!.publishedAt | date: 'MMMM d, y' }}</span>
                <span><i class="bi bi-eye me-1"></i>{{ article()!.viewCount }} views</span>
              </div>

              <!-- Tags -->
              @if (article()!.tags.length > 0) {
                <div class="mb-4">
                  @for (tag of article()!.tags; track tag) {
                    <span class="badge bg-secondary me-2">{{ tag }}</span>
                  }
                </div>
              }

              <!-- Video embed -->
              @if (article()!.videoUrl) {
                <div class="ratio ratio-16x9 mb-4">
                  <iframe
                    [src]="getEmbedUrl(article()!.videoUrl)"
                    frameborder="0"
                    allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                    allowfullscreen
                    class="rounded"
                  ></iframe>
                </div>
              }

              <!-- Featured image -->
              @if (!article()!.videoUrl && article()!.featuredImageUrl) {
                <img
                  [src]="article()!.featuredImageUrl"
                  [alt]="article()!.title"
                  class="img-fluid rounded mb-4"
                />
              }

              <!-- Cover image -->
              @if (!article()!.videoUrl && !article()!.featuredImageUrl && article()!.coverImageUrl) {
                <img
                  [src]="article()!.coverImageUrl"
                  [alt]="article()!.title"
                  class="img-fluid rounded mb-4"
                />
              }

              <!-- Excerpt -->
              @if (article()!.excerpt) {
                <p class="lead text-muted mb-4">{{ article()!.excerpt }}</p>
              }

              <!-- Content -->
              <div class="article-content" [innerHTML]="article()!.content"></div>
            </article>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .article-content {
      font-size: 1.1rem;
      line-height: 1.8;
      white-space: pre-wrap;
    }
  `]
})
export class ArticleDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private articleService = inject(ArticleService);
  private sanitizer = inject(DomSanitizer);

  article = signal<ArticleDto | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit() {
    const slug = this.route.snapshot.paramMap.get('slug');
    if (!slug) {
      this.error.set('Article not found');
      this.loading.set(false);
      return;
    }

    this.loadArticle(slug);
  }

  loadArticle(slug: string) {
    this.articleService.getBySlug(slug).subscribe({
      next: (article) => {
        this.article.set(article);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load article:', err);
        this.error.set('Article not found or failed to load');
        this.loading.set(false);
      }
    });
  }

  goBack() {
    this.router.navigate(['/']);
  }

  getEmbedUrl(url: string | null): SafeResourceUrl {
    if (!url) {
      return this.sanitizer.bypassSecurityTrustResourceUrl('');
    }
    // Convert YouTube watch URL to embed URL
    let embedUrl = url;
    if (url.includes('youtube.com/watch?v=')) {
      const videoId = url.split('v=')[1]?.split('&')[0];
      embedUrl = `https://www.youtube.com/embed/${videoId}`;
    } else if (url.includes('youtu.be/')) {
      const videoId = url.split('youtu.be/')[1]?.split('?')[0];
      embedUrl = `https://www.youtube.com/embed/${videoId}`;
    }
    return this.sanitizer.bypassSecurityTrustResourceUrl(embedUrl);
  }
}
