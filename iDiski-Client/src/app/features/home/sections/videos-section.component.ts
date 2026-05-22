import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

interface VideoArticle {
  id: string;
  title: string;
  videoUrl: string;
  publishedAt: string;
}

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
  private readonly http = inject(HttpClient);
  private readonly sanitizer = inject(DomSanitizer);

  videos = signal<VideoArticle[]>([]);
  loading = signal(true);

  ngOnInit() {
    this.loadVideos();
  }

  loadVideos() {
    this.http
      .get<{ items: any[] }>(`${environment.apiBaseUrl}/articles?pageSize=10`)
      .subscribe({
        next: (response) => {
          // Filter only articles with videoUrl
          const videoArticles = response.items
            .filter((a) => a.videoUrl)
            .slice(0, 2)
            .map((a) => ({
              id: a.id,
              title: a.title,
              videoUrl: a.videoUrl,
              publishedAt: a.publishedAt,
            }));
          this.videos.set(videoArticles);
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
