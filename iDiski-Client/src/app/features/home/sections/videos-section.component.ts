import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { VideoService } from '../../../core/services/video.service';
import { VideoSummaryDto } from '../../../core/models/video.model';

@Component({
  selector: 'app-videos-section',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="card shadow-sm mb-4">
      <div class="card-body">
        <h2 class="card-title h4 mb-4">
          <i class="bi bi-play-circle text-danger me-2"></i>Latest Videos
        </h2>

        @if (loading()) {
          <div class="text-center py-4">
            <div class="spinner-border spinner-border-sm" role="status"></div>
          </div>
        }

        @if (!loading() && videos().length > 0) {
          <div class="row g-3">
            @for (video of videos(); track video.id) {
              <div class="col-md-6">
                <div class="ratio ratio-16x9 mb-2">
                  <iframe
                    [src]="getEmbedUrl(video.videoUrl)"
                    frameborder="0"
                    allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                    allowfullscreen
                    class="rounded"
                  ></iframe>
                </div>
                <h5 class="fw-semibold small">{{ video.title }}</h5>
              </div>
            }
          </div>
        }

        @if (!loading() && videos().length === 0) {
          <p class="text-muted text-center py-3">No videos available</p>
        }
      </div>
    </section>
  `,
})
export class VideosSectionComponent implements OnInit {
  private readonly videoService = inject(VideoService);
  private readonly sanitizer = inject(DomSanitizer);

  videos = signal<VideoSummaryDto[]>([]);
  loading = signal(true);

  ngOnInit() {
    this.loadVideos();
  }

  loadVideos() {
    this.videoService
      .getPublished({ maxResults: 2 })
      .subscribe({
        next: (videos) => {
          this.videos.set(videos);
          this.loading.set(false);
        },
        error: (err) => {
          console.error('Failed to load videos:', err);
          this.loading.set(false);
        },
      });
  }

  getEmbedUrl(url: string): SafeResourceUrl {
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
