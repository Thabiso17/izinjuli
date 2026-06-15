using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using iDiski.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Retry;

namespace iDiski.Infrastructure.Services;

/// <summary>
/// Cloudinary-based file storage implementation.
/// Recommended for production deployments on platforms like Railway/Vercel.
/// </summary>
public sealed class CloudinaryFileStorageService : IFileStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly IAsyncPolicy<ImageUploadResult> _uploadRetryPolicy;

    public CloudinaryFileStorageService(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"]
            ?? throw new InvalidOperationException("Cloudinary:CloudName not configured");
        var apiKey = configuration["Cloudinary:ApiKey"]
            ?? throw new InvalidOperationException("Cloudinary:ApiKey not configured");
        var apiSecret = configuration["Cloudinary:ApiSecret"]
            ?? throw new InvalidOperationException("Cloudinary:ApiSecret not configured");

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account)
        {
            Api = { Timeout = 30000 }
        };

        _uploadRetryPolicy = Policy
            .Handle<Exception>()
            .OrResult<ImageUploadResult>(r => r.Error != null && IsTransientError(r.Error.Message))
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, delay, retryCount, context) =>
                {
                    var errorMsg = outcome.Exception?.Message ?? outcome.Result?.Error?.Message ?? "Unknown error";
                    System.Diagnostics.Debug.WriteLine($"Cloudinary upload retry {retryCount} after {delay.TotalSeconds}s: {errorMsg}");
                });
    }

    private static bool IsTransientError(string errorMessage)
    {
        return errorMessage.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
               errorMessage.Contains("temporarily", StringComparison.OrdinalIgnoreCase) ||
               errorMessage.Contains("unavailable", StringComparison.OrdinalIgnoreCase) ||
               errorMessage.Contains("rate limit", StringComparison.OrdinalIgnoreCase);
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

        var uploadResult = await _uploadRetryPolicy.ExecuteAsync(
            async () => await _cloudinary.UploadAsync(uploadParams, cancellationToken));

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

        var publicId = ExtractPublicIdFromUrl(fileUrl);
        if (string.IsNullOrEmpty(publicId))
        {
            return;
        }

        try
        {
            var deletionParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);

            if (result.Error != null)
            {
                throw new InvalidOperationException($"Failed to delete file from Cloudinary: {result.Error.Message}");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to delete file from Cloudinary: {ex.Message}", ex);
        }
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
