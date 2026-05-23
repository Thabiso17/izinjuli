import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { VideoService } from '../../../core/services/video.service';
import { VideoSummaryDto, CreateVideoRequest, UpdateVideoRequest } from '../../../core/models/video.model';

interface VideoFormData {
  title: string;
  videoUrl: string;
  description: string;
  thumbnailUrl: string;
  author: string;
  publishImmediately: boolean;
}

@Component({
  selector: 'app-videos-admin',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container-fluid py-4">
      <div class="row mb-4">
        <div class="col">
          <h1 class="display-6">Videos Management</h1>
          <p class="text-muted">Upload and manage featured videos for the homepage</p>
        </div>
        <div class="col-auto">
          <button class="btn btn-primary" (click)="showAddModal()">
            <i class="bi bi-plus-circle"></i> Add Video
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
                (ngModelChange)="loadVideos()"
              >
                <option [ngValue]="undefined">All Videos</option>
                <option [ngValue]="true">Published Only</option>
                <option [ngValue]="false">Unpublished Only</option>
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

      <!-- Videos Grid -->
      @if (!loading() && videos().length > 0) {
        <div class="row g-4">
          @for (video of videos(); track video.id) {
            <div class="col-md-6 col-lg-4">
              <div class="card h-100">
                @if (video.thumbnailUrl) {
                  <img [src]="video.thumbnailUrl" class="card-img-top" [alt]="video.title" style="height: 200px; object-fit: cover;">
                } @else {
                  <div class="card-img-top bg-secondary d-flex align-items-center justify-content-center" style="height: 200px;">
                    <i class="bi bi-play-circle text-white" style="font-size: 3rem;"></i>
                  </div>
                }
                <div class="card-body">
                  <h5 class="card-title">{{ video.title }}</h5>
                  @if (video.description) {
                    <p class="card-text text-muted small">{{ video.description }}</p>
                  }
                  <div class="d-flex gap-2 mb-2">
                    @if (video.publishedAt) {
                      <span class="badge bg-success">Published</span>
                    } @else {
                      <span class="badge bg-warning">Unpublished</span>
                    }
                    @if (video.isPinned) {
                      <span class="badge bg-primary">
                        <i class="bi bi-pin-angle-fill"></i> Pinned
                      </span>
                    }
                  </div>
                  <div class="small text-muted">
                    <div><strong>Author:</strong> {{ video.author }}</div>
                    @if (video.publishedAt) {
                      <div><strong>Published:</strong> {{ video.publishedAt | date: 'MMM d, y' }}</div>
                    }
                  </div>
                </div>
                <div class="card-footer bg-transparent">
                  <div class="btn-group btn-group-sm w-100">
                    <button
                      class="btn btn-outline-primary"
                      (click)="showEditModal(video)"
                      title="Edit"
                    >
                      <i class="bi bi-pencil"></i> Edit
                    </button>
                    @if (video.publishedAt) {
                      <button
                        [class.btn-warning]="!video.isPinned"
                        [class.btn-outline-warning]="video.isPinned"
                        class="btn"
                        (click)="togglePin(video)"
                        [title]="video.isPinned ? 'Unpin' : 'Pin to homepage'"
                      >
                        <i [class.bi-pin-angle-fill]="video.isPinned" [class.bi-pin-angle]="!video.isPinned" class="bi"></i>
                      </button>
                      <button
                        class="btn btn-outline-warning"
                        (click)="unpublishVideo(video.id)"
                        title="Unpublish"
                      >
                        <i class="bi bi-x-circle"></i>
                      </button>
                    } @else {
                      <button
                        class="btn btn-outline-success"
                        (click)="publishVideo(video.id)"
                        title="Publish"
                      >
                        <i class="bi bi-check-circle"></i>
                      </button>
                    }
                    <button
                      class="btn btn-outline-danger"
                      (click)="confirmDelete(video)"
                      [disabled]="!!video.publishedAt"
                      [title]="video.publishedAt ? 'Unpublish first' : 'Delete'"
                    >
                      <i class="bi bi-trash"></i>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          }
        </div>
      }

      <!-- Empty State -->
      @if (!loading() && videos().length === 0) {
        <div class="card">
          <div class="card-body text-center py-5">
            <i class="bi bi-camera-video display-1 text-muted"></i>
            <h3 class="mt-3">No Videos Found</h3>
            <p class="text-muted">Add your first video to get started</p>
            <button class="btn btn-primary" (click)="showAddModal()">
              <i class="bi bi-plus-circle"></i> Add Video
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
        <div class="modal-dialog modal-lg">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">
                {{ editingVideo() ? 'Edit Video' : 'Add Video' }}
              </h5>
              <button type="button" class="btn-close" (click)="closeModal()"></button>
            </div>
            <div class="modal-body">
              <form #videoForm="ngForm">
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
                      placeholder="e.g., Highlights: Derby Match 2026"
                    />
                    <small class="text-muted">
                      {{ formData.title.length }}/300 characters
                    </small>
                  </div>

                  <div class="col-12">
                    <label class="form-label">Video URL * (YouTube embed)</label>
                    <input
                      type="url"
                      class="form-control"
                      [(ngModel)]="formData.videoUrl"
                      name="videoUrl"
                      required
                      maxlength="500"
                      placeholder="https://www.youtube.com/embed/VIDEO_ID"
                    />
                    <small class="text-muted">
                      Use YouTube embed format: https://www.youtube.com/embed/VIDEO_ID
                    </small>
                  </div>

                  <div class="col-12">
                    <label class="form-label">Thumbnail URL</label>
                    <input
                      type="url"
                      class="form-control"
                      [(ngModel)]="formData.thumbnailUrl"
                      name="thumbnailUrl"
                      maxlength="500"
                      placeholder="https://example.com/thumbnail.jpg"
                    />
                    <small class="text-muted">Optional preview image</small>
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
                      placeholder="e.g., iDiski Media Team"
                    />
                  </div>

                  <div class="col-12">
                    <label class="form-label">Description</label>
                    <textarea
                      class="form-control"
                      [(ngModel)]="formData.description"
                      name="description"
                      rows="3"
                      maxlength="1000"
                      placeholder="Brief description of the video content..."
                    ></textarea>
                    <small class="text-muted">
                      {{ formData.description.length }}/1000 characters
                    </small>
                  </div>

                  @if (!editingVideo()) {
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
                          Uncheck to save as unpublished
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
                (click)="saveVideo()"
                [disabled]="videoForm.invalid || saving()"
              >
                @if (saving()) {
                  <span class="spinner-border spinner-border-sm me-2"></span>
                }
                {{ editingVideo() ? 'Update' : 'Add' }}
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
export class VideosAdminComponent implements OnInit {
  private videoService = inject(VideoService);

  videos = signal<VideoSummaryDto[]>([]);
  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);
  showModal = signal(false);
  editingVideo = signal<VideoSummaryDto | null>(null);

  filterPublishedOnly?: boolean = undefined;

  formData: VideoFormData = this.getEmptyForm();

  ngOnInit() {
    this.loadVideos();
  }

  loadVideos() {
    this.loading.set(true);
    this.error.set(null);

    this.videoService.getAllAdmin({ publishedOnly: this.filterPublishedOnly }).subscribe({
      next: (data) => {
        this.videos.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load videos: ' + err.message);
        this.loading.set(false);
      },
    });
  }

  showAddModal() {
    this.editingVideo.set(null);
    this.formData = this.getEmptyForm();
    this.showModal.set(true);
  }

  showEditModal(video: VideoSummaryDto) {
    this.editingVideo.set(video);
    this.formData = {
      title: video.title,
      videoUrl: video.videoUrl,
      description: video.description || '',
      thumbnailUrl: video.thumbnailUrl || '',
      author: video.author,
      publishImmediately: false,
    };
    this.showModal.set(true);
  }

  closeModal() {
    this.showModal.set(false);
    this.editingVideo.set(null);
    this.formData = this.getEmptyForm();
  }

  saveVideo() {
    this.saving.set(true);
    this.error.set(null);

    if (this.editingVideo()) {
      // Update existing video
      const request: UpdateVideoRequest = {
        id: this.editingVideo()!.id,
        title: this.formData.title,
        videoUrl: this.formData.videoUrl,
        description: this.formData.description || undefined,
        thumbnailUrl: this.formData.thumbnailUrl || undefined,
        author: this.formData.author,
      };

      this.videoService.update(this.editingVideo()!.id, request).subscribe({
        next: () => {
          this.success.set('Video updated successfully');
          this.saving.set(false);
          this.closeModal();
          this.loadVideos();
          setTimeout(() => this.success.set(null), 3000);
        },
        error: (err) => {
          this.error.set(`Failed to update video: ${err.error?.message || err.message}`);
          this.saving.set(false);
        },
      });
    } else {
      // Create new video
      const request: CreateVideoRequest = {
        title: this.formData.title,
        videoUrl: this.formData.videoUrl,
        description: this.formData.description || undefined,
        thumbnailUrl: this.formData.thumbnailUrl || undefined,
        author: this.formData.author,
        publishImmediately: this.formData.publishImmediately,
      };

      this.videoService.create(request).subscribe({
        next: () => {
          this.success.set('Video added successfully');
          this.saving.set(false);
          this.closeModal();
          this.loadVideos();
          setTimeout(() => this.success.set(null), 3000);
        },
        error: (err) => {
          this.error.set(`Failed to add video: ${err.error?.message || err.message}`);
          this.saving.set(false);
        },
      });
    }
  }

  publishVideo(id: string) {
    this.videoService.publish(id).subscribe({
      next: () => {
        this.success.set('Video published successfully');
        this.loadVideos();
        setTimeout(() => this.success.set(null), 3000);
      },
      error: (err) => {
        this.error.set(`Failed to publish video: ${err.error?.message || err.message}`);
      },
    });
  }

  unpublishVideo(id: string) {
    if (!confirm('Are you sure you want to unpublish this video?')) return;

    this.videoService.unpublish(id).subscribe({
      next: () => {
        this.success.set('Video unpublished successfully');
        this.loadVideos();
        setTimeout(() => this.success.set(null), 3000);
      },
      error: (err) => {
        this.error.set(`Failed to unpublish video: ${err.error?.message || err.message}`);
      },
    });
  }

  togglePin(video: VideoSummaryDto) {
    this.videoService.togglePin(video.id).subscribe({
      next: () => {
        const action = video.isPinned ? 'unpinned' : 'pinned';
        this.success.set(`Video ${action} successfully`);
        this.loadVideos();
        setTimeout(() => this.success.set(null), 3000);
      },
      error: (err) => {
        this.error.set(`Failed to toggle pin: ${err.error?.message || err.message}`);
      },
    });
  }

  confirmDelete(video: VideoSummaryDto) {
    if (!confirm(`Are you sure you want to delete "${video.title}"? This action cannot be undone.`)) {
      return;
    }

    this.videoService.delete(video.id).subscribe({
      next: () => {
        this.success.set('Video deleted successfully');
        this.loadVideos();
        setTimeout(() => this.success.set(null), 3000);
      },
      error: (err) => {
        this.error.set(`Failed to delete video: ${err.error?.message || err.message}`);
      },
    });
  }

  private getEmptyForm(): VideoFormData {
    return {
      title: '',
      videoUrl: '',
      description: '',
      thumbnailUrl: '',
      author: 'iDiski Media Team',
      publishImmediately: true,
    };
  }
}
