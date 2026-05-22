using iDiski.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace iDiski.Infrastructure.Services;

/// <summary>
/// Local file system implementation of IFileStorageService.
/// Stores files in wwwroot/uploads folder.
/// </summary>
public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _uploadPath;
    private readonly string _baseUrl;

    public LocalFileStorageService(IConfiguration configuration)
    {
        // Default to wwwroot/uploads
        _uploadPath = configuration["FileStorage:UploadPath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

        _baseUrl = configuration["FileStorage:BaseUrl"] ?? "/uploads";

        // Ensure upload directory exists
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<string> SaveFileAsync(
        Stream fileStream,
        string fileName,
        string folder,
        CancellationToken cancellationToken = default)
    {
        // Sanitize folder name
        folder = SanitizePath(folder);

        // Create subfolder if it doesn't exist
        var folderPath = Path.Combine(_uploadPath, folder);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Generate unique filename to avoid collisions
        var fileExtension = Path.GetExtension(fileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(folderPath, uniqueFileName);

        // Save file
        using var fileStreamOutput = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(fileStreamOutput, cancellationToken);

        // Return URL: /uploads/{folder}/{uniqueFileName}
        return $"{_baseUrl}/{folder}/{uniqueFileName}";
    }

    public Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl) || !fileUrl.StartsWith(_baseUrl))
        {
            return Task.CompletedTask;
        }

        // Convert URL back to file path
        var relativePath = fileUrl.Replace(_baseUrl, "").TrimStart('/');
        var filePath = Path.Combine(_uploadPath, relativePath);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    private static string SanitizePath(string path)
    {
        // Remove any invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", path.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }
}
