namespace iDiski.Domain.Entities;

public class ArticleAttachment : BaseEntity
{
    public Guid ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    /// <summary>Original filename uploaded by user (e.g., "press-coverage.pdf")</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Server path to the file (e.g., "/uploads/articles/abc-123.pdf")</summary>
    public string FileUrl { get; set; } = string.Empty;

    /// <summary>Type of attachment: PDF, Image, etc.</summary>
    public AttachmentType Type { get; set; }

    /// <summary>File size in bytes</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>Optional caption/description for the attachment</summary>
    public string? Caption { get; set; }

    /// <summary>Display order for multiple attachments</summary>
    public int DisplayOrder { get; set; } = 0;
}

public enum AttachmentType
{
    PDF = 0,
    Image = 1,
    Other = 2
}
