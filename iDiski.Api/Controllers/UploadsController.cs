using iDiski.Application.Common.Constants;
using iDiski.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace iDiski.Api.Controllers;

/// <summary>
/// Handles file uploads (images for teams, players, sponsors, etc.)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class UploadsController : ControllerBase
{
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<UploadsController> _logger;

    public UploadsController(
        IFileStorageService fileStorage,
        ILogger<UploadsController> logger)
    {
        _fileStorage = fileStorage;
        _logger = logger;
    }

    /// <summary>
    /// Upload an image file (for teams, players, sponsors, etc.)
    /// </summary>
    /// <param name="file">The image file</param>
    /// <param name="folder">Folder to store the file in (e.g., "teams", "players", "sponsors")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The URL to the uploaded file</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] string folder = "general",
        CancellationToken cancellationToken = default)
    {
        // Validate file
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded" });
        }

        if (file.Length > FileUploadConstants.MaxImageFileSizeBytes)
        {
            return BadRequest(new { message = $"File size exceeds maximum of {FileUploadConstants.MaxImageFileSizeBytes / 1024 / 1024} MB" });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!FileUploadConstants.AllowedImageExtensions.Contains(extension))
        {
            return BadRequest(new
            {
                message = $"Invalid file type. Allowed types: {string.Join(", ", FileUploadConstants.AllowedImageExtensions)}"
            });
        }

        if (file.ContentType != null && !FileUploadConstants.AllowedImageMimeTypes.Contains(file.ContentType))
        {
            return BadRequest(new
            {
                message = "Invalid file content type. File may be corrupted or of wrong type."
            });
        }

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Save file
            using var stream = file.OpenReadStream();
            var fileUrl = await _fileStorage.SaveFileAsync(
                stream,
                file.FileName,
                folder,
                cancellationToken);

            sw.Stop();

            _logger.LogInformation(
                "File uploaded successfully. Duration: {DurationMs}ms, Size: {FileSizeKB}KB, Folder: {Folder}, FileName: {FileName}",
                sw.ElapsedMilliseconds,
                file.Length / 1024,
                folder,
                file.FileName);

            return Ok(new UploadResponse(fileUrl));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file: {FileName} to {Folder}", file.FileName, folder);
            return StatusCode(500, new { message = "Failed to upload file" });
        }
    }

    /// <summary>
    /// Delete an uploaded file
    /// </summary>
    /// <param name="fileUrl">The URL of the file to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        [FromQuery] string fileUrl,
        CancellationToken cancellationToken = default)
    {
        await _fileStorage.DeleteFileAsync(fileUrl, cancellationToken);
        return NoContent();
    }
}

public sealed record UploadResponse(string FileUrl);
