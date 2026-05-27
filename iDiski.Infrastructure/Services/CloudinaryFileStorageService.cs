using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using iDiski.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace iDiski.Infrastructure.Services;

/// <summary>
/// Cloudinary-based file storage implementation.
/// Recommended for production deployments on platforms like Railway/Vercel.
/// </summary>
public sealed class CloudinaryFileStorageService : IFileStorageService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryFileStorageService(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"]
            ?? throw new InvalidOperationException("Cloudinary:CloudName not configured");
        var apiKey = configuration["Cloudinary:ApiKey"]
            ?? throw new InvalidOperationException("Cloudinary:ApiKey not configured");
        var apiSecret = configuration["Cloudinary:ApiSecret"]
            ?? throw new InvalidOperationException("Cloudinary:ApiSecret not configured");

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> SaveFileAsync(
        Stream fileStream,
        string fileName,
        string folder,
        CancellationToken cancellationToken = default)
    {
        // Generate a unique public ID
        var fileExtension = Path.GetExtension(fileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var publicId = $"{folder}/{Path.GetFileNameWithoutExtension(uniqueFileName)}";

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            PublicId = publicId,
            Folder = folder,
            Overwrite = false,
            UniqueFilename = true,
            UseFilename = false
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (uploadResult.Error != null)
        {
            throw new InvalidOperationException(
                $"Cloudinary upload failed: {uploadResult.Error.Message}");
        }

        // Return the secure URL (HTTPS)
        return uploadResult.SecureUrl.ToString();
    }

    public async Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return;
        }

        // Extract public ID from Cloudinary URL
        // Example: https://res.cloudinary.com/{cloud}/image/upload/v123456/{folder}/{filename}.jpg
        var publicId = ExtractPublicIdFromUrl(fileUrl);
        if (string.IsNullOrEmpty(publicId))
        {
            return;
        }

        var deletionParams = new DeletionParams(publicId);
        await _cloudinary.DestroyAsync(deletionParams);
    }

    private static string? ExtractPublicIdFromUrl(string url)
    {
        // Cloudinary URL format: https://res.cloudinary.com/{cloud}/image/upload/v{version}/{public_id}.{ext}
        var uri = new Uri(url);
        var segments = uri.AbsolutePath.Split('/');

        // Find "upload" segment and get everything after version
        var uploadIndex = Array.IndexOf(segments, "upload");
        if (uploadIndex < 0 || uploadIndex + 2 >= segments.Length)
        {
            return null;
        }

        // Get segments after version (skip "v123456")
        var publicIdSegments = segments.Skip(uploadIndex + 2).ToArray();
        var publicIdWithExt = string.Join("/", publicIdSegments);

        // Remove file extension
        var lastDotIndex = publicIdWithExt.LastIndexOf('.');
        return lastDotIndex > 0 ? publicIdWithExt.Substring(0, lastDotIndex) : publicIdWithExt;
    }
}
