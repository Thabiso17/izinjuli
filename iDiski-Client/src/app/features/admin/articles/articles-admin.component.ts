import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ArticleService, DivisionService, TeamService, PlayerService } from '../../../core/services';
import {
  ArticleSummaryDto,
  CreateArticleRequest,
  UpdateArticleRequest,
  DivisionDto,
  TeamDto,
  PlayerDto,
} from '../../../core/models';

interface ArticleFormData {
  title: string;
  content: string;
  author: string;
  tags: string;
  featuredImageUrl: string;
  publishImmediately: boolean;
  divisionId: string | null;
  teamId: string | null;
  playerId: string | null;
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
          <button class="btn btn-primary" data-testid="add-article" (click)="showAddModal()">
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
                  <th>Pinned</th>
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
                      @if (article.isArchived) {
                        <span class="badge bg-dark me-1">Archived</span>
                      }
                      @if (article.isPinned) {
                        <i class="bi bi-pin-angle-fill text-primary" title="Pinned"></i>
                      } @else {
                        <span class="text-muted">-</span>
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
                        @if (article.publishedAt) {
                          <button
                            [class.btn-warning]="!article.isPinned"
                            [class.btn-outline-warning]="article.isPinned"
                            class="btn btn-sm"
                            (click)="togglePin(article)"
                            [title]="article.isPinned ? 'Unpin' : 'Pin to top'"
                          >
                            <i [class.bi-pin-angle-fill]="article.isPinned" [class.bi-pin-angle]="!article.isPinned" class="bi"></i>
                          </button>
                        }
                        @if (!article.publishedAt) {
                          <button
                            class="btn btn-outline-success"
                            (click)="publishArticle(article.id)"
                            title="Publish"
                          >
                            <i class="bi bi-check-circle"></i>
                          </button>
                        }
                        @if (article.isArchived) {
                          <button
                            class="btn btn-outline-secondary"
                            (click)="unarchiveArticle(article.id)"
                            title="Restore to public view"
                          >
                            <i class="bi bi-box-arrow-up"></i>
                          </button>
                        } @else {
                          <button
                            class="btn btn-outline-secondary"
                            (click)="confirmArchive(article)"
                            title="Archive — retires it from public view but keeps the record"
                          >
                            <i class="bi bi-archive"></i>
                          </button>
                        }
                        <button
                          class="btn btn-outline-danger"
                          (click)="confirmDelete(article)"
                          [disabled]="!!article.publishedAt || !!article.isArchived"
                          [title]="
                            article.publishedAt || article.isArchived
                              ? 'Published and archived articles are part of the record — archive rather than delete'
                              : 'Delete'
                          "
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
            <button class="btn btn-primary" data-testid="add-article" (click)="showAddModal()">
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

                  <!-- What the article is about: division → team → player -->
                  <div class="col-12">
                    <label class="form-label">What is this article about?</label>
                    <div class="row g-2">
                      <div class="col-md-4">
                        <select
                          class="form-select"
                          [(ngModel)]="formData.divisionId"
                          name="divisionId"
                          (ngModelChange)="onDivisionChange()"
                        >
                          <option [ngValue]="null">Whole league</option>
                          @for (division of divisions(); track division.id) {
                            <option [ngValue]="division.id">{{ division.name }}</option>
                          }
                        </select>
                      </div>
                      <div class="col-md-4">
                        <select
                          class="form-select"
                          [(ngModel)]="formData.teamId"
                          name="teamId"
                          (ngModelChange)="onTeamChange()"
                          [disabled]="!formData.divisionId"
                        >
                          <option [ngValue]="null">Whole division</option>
                          @for (team of teamsInDivision(); track team.id) {
                            <option [ngValue]="team.id">{{ team.name }}</option>
                          }
                        </select>
                      </div>
                      <div class="col-md-4">
                        <select
                          class="form-select"
                          [(ngModel)]="formData.playerId"
                          name="playerId"
                          [disabled]="!formData.teamId"
                        >
                          <option [ngValue]="null">Whole team</option>
                          @for (player of playersInTeam(); track player.id) {
                            <option [ngValue]="player.id">{{ player.fullName }}</option>
                          }
                        </select>
                      </div>
                    </div>
                    <small class="text-muted">
                      Leave as "Whole league" for general news. Narrow it down to show the
                      article on a division or team page.
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
                data-testid="save-article"
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
  private divisionService = inject(DivisionService);
  private teamService = inject(TeamService);
  private playerService = inject(PlayerService);

  divisions = signal<DivisionDto[]>([]);
  teams = signal<TeamDto[]>([]);
  teamsInDivision = signal<TeamDto[]>([]);
  playersInTeam = signal<PlayerDto[]>([]);

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

    this.divisionService.getAll().subscribe({
      next: (divisions) => this.divisions.set(divisions),
      error: (err) => console.error('Failed to load divisions:', err),
    });

    this.teamService.getAll().subscribe({
      next: (teams) => this.teams.set(teams),
      error: (err) => console.error('Failed to load teams:', err),
    });
  }

  onDivisionChange() {
    // A team only makes sense inside the chosen division, and a player inside that team.
    this.formData.teamId = null;
    this.formData.playerId = null;
    this.playersInTeam.set([]);
    this.refreshTeamsInDivision();
  }

  onTeamChange() {
    this.formData.playerId = null;
    this.refreshPlayersInTeam();
  }

  private refreshTeamsInDivision() {
    const divisionId = this.formData.divisionId;
    this.teamsInDivision.set(
      divisionId ? this.teams().filter((team) => team.divisionId === divisionId) : []
    );
  }

  private refreshPlayersInTeam() {
    const teamId = this.formData.teamId;
    if (!teamId) {
      this.playersInTeam.set([]);
      return;
    }

    this.playerService.getAll(teamId, true).subscribe({
      next: (players) => {
        this.playersInTeam.set(players);
        this.keepTaggedPlayerSelectable(players);
      },
      error: (err) => console.error('Failed to load players:', err),
    });
  }

  /**
   * A tagged player may have since transferred away, which would leave the dropdown with
   * no matching option and quietly drop the tag on save. Keep them in the list instead.
   */
  private keepTaggedPlayerSelectable(squad: PlayerDto[]) {
    const taggedId = this.formData.playerId;
    if (!taggedId || squad.some((player) => player.id === taggedId)) return;

    this.playerService.getById(taggedId).subscribe({
      next: (player) => this.playersInTeam.set([...squad, player]),
      error: (err) => console.error('Failed to load tagged player:', err),
    });
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
    this.showModal.set(true);

    // The body is not on the summary, and loading it from the public by-slug endpoint would
    // miss drafts — so fetch the full article, otherwise saving would blank its content.
    this.articleService.getByIdAdmin(article.id).subscribe({
      next: (full) => {
        this.formData = {
          title: full.title,
          content: full.content,
          author: full.author,
          tags: full.tags.join(', '),
          featuredImageUrl: full.featuredImageUrl || '',
          publishImmediately: false,
          divisionId: full.divisionId ?? null,
          teamId: full.teamId ?? null,
          playerId: full.playerId ?? null,
        };
        this.refreshTeamsInDivision();
        this.refreshPlayersInTeam();
      },
      error: (err) => {
        this.error.set(
          `Failed to load article: ${err.error?.detail || err.error?.title || err.message}`
        );
        this.closeModal();
      },
    });
  }

  closeModal() {
    this.showModal.set(false);
    this.editingArticle.set(null);
    this.formData = this.getEmptyForm();
    this.teamsInDivision.set([]);
    this.playersInTeam.set([]);
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
        divisionId: this.formData.divisionId,
        teamId: this.formData.teamId,
        playerId: this.formData.playerId,
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
          this.error.set(`Failed to update article: ${err.error?.detail || err.error?.title || err.message}`);
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
        divisionId: this.formData.divisionId,
        teamId: this.formData.teamId,
        playerId: this.formData.playerId,
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
          this.error.set(`Failed to create article: ${err.error?.detail || err.error?.title || err.message}`);
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
        this.error.set(`Failed to publish article: ${err.error?.detail || err.error?.title || err.message}`);
      },
    });
  }

  togglePin(article: ArticleSummaryDto) {
    const action = article.isPinned ? 'unpin' : 'pin';
    const newPinnedState = !article.isPinned;

    // For now, we'll fetch the full article, toggle the pin status, and update it
    this.articleService.getBySlug(article.slug).subscribe({
      next: (fullArticle) => {
        const updateRequest: UpdateArticleRequest = {
          id: article.id,
          title: fullArticle.title,
          content: fullArticle.content,
          excerpt: fullArticle.excerpt || '',
          coverImageUrl: fullArticle.coverImageUrl || '',
          videoUrl: fullArticle.videoUrl || '',
          featuredImageUrl: fullArticle.featuredImageUrl || '',
          author: fullArticle.author,
          tags: fullArticle.tags,
          isPinned: newPinnedState,
          divisionId: fullArticle.divisionId ?? null,
          teamId: fullArticle.teamId ?? null,
          playerId: fullArticle.playerId ?? null
        };

        this.articleService.update(article.id, updateRequest).subscribe({
          next: () => {
            this.success.set(`Article ${action}ned successfully`);
            this.loadArticles();
            setTimeout(() => this.success.set(null), 3000);
          },
          error: (err) => {
            this.error.set(`Failed to ${action} article: ${err.error?.detail || err.error?.title || err.message}`);
          },
        });
      },
      error: (err) => {
        this.error.set(`Failed to fetch article details: ${err.error?.detail || err.error?.title || err.message}`);
      },
    });
  }

  confirmArchive(article: ArticleSummaryDto) {
    if (!confirm(`Archive "${article.title}"? It will be removed from public pages but kept on record.`)) {
      return;
    }

    this.articleService.archive(article.id).subscribe({
      next: () => {
        this.success.set('Article archived');
        this.loadArticles();
        setTimeout(() => this.success.set(null), 3000);
      },
      error: (err) => {
        this.error.set(`Failed to archive article: ${err.error?.detail || err.error?.title || err.message}`);
      },
    });
  }

  unarchiveArticle(id: string) {
    this.articleService.unarchive(id).subscribe({
      next: () => {
        this.success.set('Article restored');
        this.loadArticles();
        setTimeout(() => this.success.set(null), 3000);
      },
      error: (err) => {
        this.error.set(`Failed to restore article: ${err.error?.detail || err.error?.title || err.message}`);
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
        this.error.set(`Failed to delete article: ${err.error?.detail || err.error?.title || err.message}`);
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
      divisionId: null,
      teamId: null,
      playerId: null,
    };
  }
}
