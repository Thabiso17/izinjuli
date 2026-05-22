import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ArticleService } from '../../../core/services';
import { ArticleSummaryDto, CreateArticleRequest, UpdateArticleRequest } from '../../../core/models';

interface ArticleFormData {
  title: string;
  content: string;
  author: string;
  tags: string;
  featuredImageUrl: string;
  publishImmediately: boolean;
}

@Component({
  selector: 'app-articles-admin',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container-fluid py-4">
      <div class="row mb-4">
        <div class="col">
          <h1 class="display-6">Articles Management</h1>
          <p class="text-muted">Create and manage news articles and announcements</p>
        </div>
        <div class="col-auto">
          <button class="btn btn-primary" (click)="showAddModal()">
            <i class="bi bi-plus-circle"></i> Create Article
          </button>
        </div>
      </div>

      <!-- Filters -->
      <div class="card mb-4">
        <div class="card-body">
          <div class="row g-3">
            <div class="col-md-4">
              <label class="form-label">Status</label>
              <select
                class="form-select"
                [(ngModel)]="filterPublishedOnly"
                (ngModelChange)="loadArticles()"
              >
                <option [ngValue]="undefined">All Articles</option>
                <option [ngValue]="true">Published Only</option>
                <option [ngValue]="false">Drafts Only</option>
              </select>
            </div>
          </div>
        </div>
      </div>

      <!-- Loading State -->
      @if (loading()) {
        <div class="text-center py-5">
          <div class="spinner-border text-primary" role="status"></div>
        </div>
      }

      <!-- Articles Table -->
      @if (!loading() && articles().length > 0) {
        <div class="card">
          <div class="table-responsive">
            <table class="table table-hover mb-0">
              <thead class="table-light">
                <tr>
                  <th>Title</th>
                  <th>Author</th>
                  <th>Tags</th>
                  <th>Status</th>
                  <th>Published</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                @for (article of articles(); track article.id) {
                  <tr>
                    <td>
                      <div class="fw-semibold">{{ article.title }}</div>
                      <small class="text-muted">{{ article.slug }}</small>
                    </td>
                    <td>{{ article.author }}</td>
                    <td>
                      @for (tag of article.tags; track tag) {
                        <span class="badge bg-secondary me-1">{{ tag }}</span>
                      }
                    </td>
                    <td>
                      @if (article.publishedAt) {
                        <span class="badge bg-success">Published</span>
                      } @else {
                        <span class="badge bg-warning">Draft</span>
                      }
                    </td>
                    <td>
                      @if (article.publishedAt) {
                        <small class="text-muted">
                          {{ article.publishedAt | date: 'MMM d, y' }}
                        </small>
                      } @else {
                        <small class="text-muted">-</small>
                      }
                    </td>
                    <td>
                      <div class="btn-group btn-group-sm">
                        <button
                          class="btn btn-outline-primary"
                          (click)="showEditModal(article)"
                          title="Edit"
                        >
                          <i class="bi bi-pencil"></i>
                        </button>
                        @if (!article.publishedAt) {
                          <button
                            class="btn btn-outline-success"
                            (click)="publishArticle(article.id)"
                            title="Publish"
                          >
                            <i class="bi bi-check-circle"></i>
                          </button>
                        } @else {
                          <button
                            class="btn btn-outline-warning"
                            (click)="unpublishArticle(article.id)"
                            title="Unpublish"
                          >
                            <i class="bi bi-x-circle"></i>
                          </button>
                        }
                        <button
                          class="btn btn-outline-danger"
                          (click)="confirmDelete(article)"
                          [disabled]="!!article.publishedAt"
                          [title]="article.publishedAt ? 'Unpublish first' : 'Delete'"
                        >
                          <i class="bi bi-trash"></i>
                        </button>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      }

      <!-- Empty State -->
      @if (!loading() && articles().length === 0) {
        <div class="card">
          <div class="card-body text-center py-5">
            <i class="bi bi-newspaper display-1 text-muted"></i>
            <h3 class="mt-3">No Articles Found</h3>
            <p class="text-muted">Create your first article to get started</p>
            <button class="btn btn-primary" (click)="showAddModal()">
              <i class="bi bi-plus-circle"></i> Create Article
            </button>
          </div>
        </div>
      }

      <!-- Alerts -->
      @if (error()) {
        <div class="alert alert-danger alert-dismissible fade show mt-3" role="alert">
          {{ error() }}
          <button type="button" class="btn-close" (click)="error.set(null)"></button>
        </div>
      }

      @if (success()) {
        <div class="alert alert-success alert-dismissible fade show mt-3" role="alert">
          {{ success() }}
          <button type="button" class="btn-close" (click)="success.set(null)"></button>
        </div>
      }
    </div>

    <!-- Add/Edit Modal -->
    @if (showModal()) {
      <div class="modal fade show d-block" tabindex="-1" style="background-color: rgba(0,0,0,0.5)">
        <div class="modal-dialog modal-xl">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">
                {{ editingArticle() ? 'Edit Article' : 'Create Article' }}
              </h5>
              <button type="button" class="btn-close" (click)="closeModal()"></button>
            </div>
            <div class="modal-body">
              <form #articleForm="ngForm">
                <div class="row g-3">
                  <div class="col-12">
                    <label class="form-label">Title *</label>
                    <input
                      type="text"
                      class="form-control"
                      [(ngModel)]="formData.title"
                      name="title"
                      required
                      maxlength="300"
                      placeholder="e.g., Player of the Month - March 2026"
                    />
                    <small class="text-muted">
                      {{ formData.title.length }}/300 characters
                    </small>
                  </div>

                  <div class="col-md-6">
                    <label class="form-label">Author *</label>
                    <input
                      type="text"
                      class="form-control"
                      [(ngModel)]="formData.author"
                      name="author"
                      required
                      maxlength="100"
                      placeholder="e.g., iDiski Editorial Team"
                    />
                  </div>

                  <div class="col-md-6">
                    <label class="form-label">Tags</label>
                    <input
                      type="text"
                      class="form-control"
                      [(ngModel)]="formData.tags"
                      name="tags"
                      placeholder="e.g., Awards, Player of the Month, Midfielders"
                    />
                    <small class="text-muted">Comma-separated</small>
                  </div>

                  <div class="col-12">
                    <label class="form-label">Featured Image URL</label>
                    <input
                      type="url"
                      class="form-control"
                      [(ngModel)]="formData.featuredImageUrl"
                      name="featuredImageUrl"
                      placeholder="https://example.com/image.jpg"
                    />
                    <small class="text-muted">Paste image URL from web</small>
                  </div>

                  <div class="col-12">
                    <label class="form-label">Content * (Markdown supported)</label>
                    <textarea
                      class="form-control font-monospace"
                      [(ngModel)]="formData.content"
                      name="content"
                      required
                      rows="15"
                      placeholder="## Headline

Lebo Molefe has been named Player of the Month for March 2026...

**Key Stats:**
- 5 Goals
- 3 Assists
- 90% Pass Accuracy"
                    ></textarea>
                    <small class="text-muted">
                      Use Markdown for formatting. {{ formData.content.length }} characters
                    </small>
                  </div>

                  @if (!editingArticle()) {
                    <div class="col-12">
                      <div class="form-check">
                        <input
                          type="checkbox"
                          class="form-check-input"
                          [(ngModel)]="formData.publishImmediately"
                          name="publishImmediately"
                          id="publishImmediately"
                        />
                        <label class="form-check-label" for="publishImmediately">
                          Publish immediately
                        </label>
                        <small class="text-muted d-block">
                          Uncheck to save as draft
                        </small>
                      </div>
                    </div>
                  }
                </div>
              </form>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" (click)="closeModal()">
                Cancel
              </button>
              <button
                type="button"
                class="btn btn-primary"
                (click)="saveArticle()"
                [disabled]="articleForm.invalid || saving()"
              >
                @if (saving()) {
                  <span class="spinner-border spinner-border-sm me-2"></span>
                }
                {{ editingArticle() ? 'Update' : 'Create' }}
              </button>
            </div>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .modal.show { display: block; }
  `],
})
export class ArticlesAdminComponent implements OnInit {
  private articleService = inject(ArticleService);

  articles = signal<ArticleSummaryDto[]>([]);
  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);
  showModal = signal(false);
  editingArticle = signal<ArticleSummaryDto | null>(null);

  filterPublishedOnly?: boolean = undefined;

  formData: ArticleFormData = this.getEmptyForm();

  ngOnInit() {
    this.loadArticles();
  }

  loadArticles() {
    this.loading.set(true);
    this.error.set(null);

    this.articleService.getAllAdmin({ publishedOnly: this.filterPublishedOnly }).subscribe({
      next: (data) => {
        this.articles.set(data.items);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load articles: ' + err.message);
        this.loading.set(false);
      },
    });
  }

  showAddModal() {
    this.editingArticle.set(null);
    this.formData = this.getEmptyForm();
    this.showModal.set(true);
  }

  showEditModal(article: ArticleSummaryDto) {
    this.editingArticle.set(article);
    // Note: We'd need to fetch full article content via getBySlug
    // For now, just show title/author (content editing requires full DTO)
    this.formData = {
      title: article.title,
      content: '',
      author: article.author,
      tags: article.tags.join(', '),
      featuredImageUrl: article.featuredImageUrl || '',
      publishImmediately: false,
    };
    this.showModal.set(true);
  }

  closeModal() {
    this.showModal.set(false);
    this.editingArticle.set(null);
    this.formData = this.getEmptyForm();
  }

  saveArticle() {
    this.saving.set(true);
    this.error.set(null);

    const tags = this.formData.tags
      .split(',')
      .map(t => t.trim())
      .filter(t => t.length > 0);

    if (this.editingArticle()) {
      // Update existing article
      const request: UpdateArticleRequest = {
        id: this.editingArticle()!.id,
        title: this.formData.title,
        content: this.formData.content,
        author: this.formData.author,
        tags,
        featuredImageUrl: this.formData.featuredImageUrl || undefined,
      };

      this.articleService.update(this.editingArticle()!.id, request).subscribe({
        next: () => {
          this.success.set('Article updated successfully');
          this.saving.set(false);
          this.closeModal();
          this.loadArticles();
          setTimeout(() => this.success.set(null), 3000);
        },
        error: (err) => {
          this.error.set(`Failed to update article: ${err.error?.message || err.message}`);
          this.saving.set(false);
        },
      });
    } else {
      // Create new article
      const request: CreateArticleRequest = {
        title: this.formData.title,
        content: this.formData.content,
        author: this.formData.author,
        tags,
        featuredImageUrl: this.formData.featuredImageUrl || undefined,
        publishImmediately: this.formData.publishImmediately,
      };

      this.articleService.create(request).subscribe({
        next: () => {
          this.success.set('Article created successfully');
          this.saving.set(false);
          this.closeModal();
          this.loadArticles();
          setTimeout(() => this.success.set(null), 3000);
        },
        error: (err) => {
          this.error.set(`Failed to create article: ${err.error?.message || err.message}`);
          this.saving.set(false);
        },
      });
    }
  }

  publishArticle(id: string) {
    this.articleService.publish(id).subscribe({
      next: () => {
        this.success.set('Article published successfully');
        this.loadArticles();
        setTimeout(() => this.success.set(null), 3000);
      },
      error: (err) => {
        this.error.set(`Failed to publish article: ${err.error?.message || err.message}`);
      },
    });
  }

  unpublishArticle(id: string) {
    if (!confirm('Are you sure you want to unpublish this article?')) return;

    this.articleService.unpublish(id).subscribe({
      next: () => {
        this.success.set('Article unpublished successfully');
        this.loadArticles();
        setTimeout(() => this.success.set(null), 3000);
      },
      error: (err) => {
        this.error.set(`Failed to unpublish article: ${err.error?.message || err.message}`);
      },
    });
  }

  confirmDelete(article: ArticleSummaryDto) {
    if (!confirm(`Are you sure you want to delete "${article.title}"? This action cannot be undone.`)) {
      return;
    }

    this.articleService.delete(article.id).subscribe({
      next: () => {
        this.success.set('Article deleted successfully');
        this.loadArticles();
        setTimeout(() => this.success.set(null), 3000);
      },
      error: (err) => {
        this.error.set(`Failed to delete article: ${err.error?.message || err.message}`);
      },
    });
  }

  private getEmptyForm(): ArticleFormData {
    return {
      title: '',
      content: '',
      author: 'iDiski Editorial Team',
      tags: '',
      featuredImageUrl: '',
      publishImmediately: true,
    };
  }
}
