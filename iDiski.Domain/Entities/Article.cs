namespace iDiski.Domain.Entities;

public class Article : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    /// <summary>URL-friendly slug, e.g. "idiski-wins-local-derby". Must be unique.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Full body — store as Markdown or HTML, your Angular renderer decides.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Short teaser shown on cards / previews.</summary>
    public string? Excerpt { get; set; }

    public string? CoverImageUrl { get; set; }

    /// <summary>YouTube video ID or embed URL for rich media articles.</summary>
    public string? VideoUrl { get; set; }

    /// <summary>High-quality banner image for news articles and featured content.</summary>
    public string? FeaturedImageUrl { get; set; }

    public string Author { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = false;
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Stored as a native PostgreSQL text[] column via Npgsql.
    /// EF config: .HasColumnType("text[]")
    /// </summary>
    public string[] Tags { get; set; } = Array.Empty<string>();

    public int ViewCount { get; set; }
}
