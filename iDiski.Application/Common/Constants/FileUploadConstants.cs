namespace iDiski.Application.Common.Constants;

/// <summary>
/// Centralized constants for file upload configuration and validation.
/// </summary>
public static class FileUploadConstants
{
    /// <summary>
    /// Maximum file size for general image uploads (5 MB)
    /// </summary>
    public const long MaxImageFileSizeBytes = 5 * 1024 * 1024;

    /// <summary>
    /// Maximum file size for document uploads like PDFs (10 MB)
    /// </summary>
    public const long MaxDocumentFileSizeBytes = 10 * 1024 * 1024;

    /// <summary>
    /// Allowed image file extensions
    /// </summary>
    public static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    /// <summary>
    /// Allowed MIME types for image uploads
    /// </summary>
    public static readonly string[] AllowedImageMimeTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };

    /// <summary>
    /// Allowed file extensions for document uploads
    /// </summary>
    public static readonly string[] AllowedDocumentExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".gif" };

    /// <summary>
    /// Allowed MIME types for document uploads
    /// </summary>
    public static readonly string[] AllowedDocumentMimeTypes =
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/gif"
    };

    /// <summary>
    /// Default upload folder for general uploads
    /// </summary>
    public const string DefaultUploadFolder = "general";

    /// <summary>
    /// Upload folder for article attachments
    /// </summary>
    public const string ArticleAttachmentsFolder = "articles";

    /// <summary>
    /// Upload folder for team logos
    /// </summary>
    public const string TeamLogosFolder = "teams";

    /// <summary>
    /// Upload folder for player photos
    /// </summary>
    public const string PlayerPhotosFolder = "players";

    /// <summary>
    /// Upload folder for sponsor logos
    /// </summary>
    public const string SponsorLogosFolder = "sponsors";
}
