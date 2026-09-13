import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { VideoService } from '../../core/services/video.service';
import { VideoSummaryDto } from '../../core/models/video.model';

/**
 * Videos for one division or team: the newest plays inline, the rest are titles that
 * swap into the player when clicked. Renders nothing at all when there are no videos.
 */
@Component({
  selector: 'app-scoped-videos',
  imports: [CommonModule],
  template: `
    @if (videos().length > 0) {
      <section class="card shadow-sm mb-4">
        <div class="card-body">
          <h2 class="card-title h4 mb-4">
            <i class="bi bi-play-circle text-danger me-2"></i>{{ heading() }}
          </h2>

          @if (selected(); as current) {
            <div class="ratio ratio-16x9 mb-3">
              <iframe
                [src]="embedUrl()"
                frameborder="0"
                allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                allowfullscreen
                class="rounded"
              ></iframe>
            </div>

            <h3 class="h5 fw-semibold mb-1">{{ current.title }}</h3>
            @if (current.description) {
              <p class="text-muted small mb-0">{{ current.description }}</p>
            }
          }

          @if (others().length > 0) {
            <hr class="my-4" />
            <h4 class="h6 text-uppercase text-muted mb-3">More videos</h4>
            <ul class="list-unstyled mb-0">
              @for (video of others(); track video.id) {
                <li class="mb-2">
                  <button type="button" class="btn btn-link p-0 text-start" (click)="select(video)">
                    {{ video.title }}
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
export class ScopedVideosComponent implements OnInit {
  readonly divisionId = input<string | undefined>(undefined);
  readonly teamId = input<string | undefined>(undefined);
  readonly playerId = input<string | undefined>(undefined);
  readonly heading = input<string>('Videos');
  readonly limit = input<number>(10);

  private readonly videoService = inject(VideoService);
  private readonly sanitizer = inject(DomSanitizer);

  videos = signal<VideoSummaryDto[]>([]);
  selected = signal<VideoSummaryDto | null>(null);

  others = computed(() => {
    const current = this.selected();
    return this.videos().filter((video) => video.id !== current?.id);
  });

  embedUrl = computed<SafeResourceUrl | null>(() => {
    const current = this.selected();
    return current ? this.toEmbedUrl(current.videoUrl) : null;
  });

  ngOnInit(): void {
    // Without a scope this would fall back to every video on the site, which is never
    // what a division or team page wants.
    if (!this.divisionId() && !this.teamId() && !this.playerId()) return;

    this.videoService
      .getPublished({
        maxResults: this.limit(),
        divisionId: this.divisionId(),
        teamId: this.teamId(),
        playerId: this.playerId(),
      })
      .subscribe({
        next: (videos) => {
          this.videos.set(videos);
          this.selected.set(videos[0] ?? null);
        },
        error: (err) => console.error('Failed to load videos:', err),
      });
  }

  select(video: VideoSummaryDto): void {
    this.selected.set(video);
  }

  private toEmbedUrl(url: string): SafeResourceUrl {
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
