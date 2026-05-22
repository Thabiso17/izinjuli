namespace iDiski.Application.Common.Interfaces;

/// <summary>
/// Service for handling file uploads (images, documents, etc.)
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves an uploaded file and returns the URL/path to access it.
    /// </summary>
    /// <param name="fileStream">The file content stream</param>
    /// <param name="fileName">Original filename</param>
    /// <param name="folder">Folder/category (e.g., "teams", "players", "sponsors")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The URL or relative path to the saved file</returns>
    Task<string> SaveFileAsync(
        Stream fileStream,
        string fileName,
        string folder,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from storage.
    /// </summary>
    /// <param name="fileUrl">The URL or path returned from SaveFileAsync</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
}
