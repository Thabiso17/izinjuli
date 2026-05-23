namespace iDiski.Domain.Entities;

/// <summary>
/// Represents a standalone video that can be featured on the homepage.
/// Videos are decoupled from articles - they can be pinned independently.
/// </summary>
public class Video : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    /// <summary>YouTube video ID or full URL.</summary>
    public string VideoUrl { get; set; } = string.Empty;

    /// <summary>Short description shown on video cards.</summary>
    public string? Description { get; set; }

    /// <summary>Thumbnail image URL (can be auto-generated from YouTube or custom).</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Author or source of the video (e.g., "iDiski Media Team").</summary>
    public string Author { get; set; } = string.Empty;

    public bool IsPublished { get; set; } = false;
    public DateTime? PublishedAt { get; set; }

    /// <summary>Pinned videos appear first on the homepage.</summary>
    public bool IsPinned { get; set; } = false;

    public int ViewCount { get; set; }
}
