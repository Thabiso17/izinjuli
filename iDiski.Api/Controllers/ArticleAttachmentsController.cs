using iDiski.Application.Articles;
using iDiski.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace iDiski.Api.Controllers;

[ApiController]
[Route("api/articles/{articleId:guid}/attachments")]
public class ArticleAttachmentsController : BaseApiController
{
    private readonly IFileStorageService _fileStorage;

    public ArticleAttachmentsController(IFileStorageService fileStorage)
    {
        _fileStorage = fileStorage;
    }

    /// <summary>
    /// Get all attachments for an article
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAttachments(Guid articleId)
    {
        var query = new GetArticleAttachmentsQuery(articleId);
        var attachments = await Sender.Send(query);
        return Ok(attachments);
    }

    /// <summary>
    /// Upload a new attachment (PDF or image)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Upload(
        Guid articleId,
        [FromForm] IFormFile file,
        [FromForm] string? caption,
        [FromForm] int displayOrder = 0)
    {
        // Validate file
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        // Check file size (max 10MB)
        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { error = "File size exceeds 10MB limit" });

        // Validate file type
        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".gif" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(fileExtension))
            return BadRequest(new { error = "Invalid file type. Allowed: PDF, JPG, PNG, GIF" });

        // Determine attachment type
        var attachmentType = fileExtension == ".pdf" ? "PDF" : "Image";

        string fileUrl = null;
        try
        {
            // Upload file
            await using var stream = file.OpenReadStream();
            fileUrl = await _fileStorage.SaveFileAsync(stream, file.FileName, "articles");

            // Create attachment record
            var command = new AddArticleAttachmentCommand(
                articleId,
                file.FileName,
                fileUrl,
                attachmentType,
                file.Length,
                caption,
                displayOrder
            );

            var attachmentId = await Sender.Send(command);

            return Ok(new
            {
                id = attachmentId,
                fileName = file.FileName,
                fileUrl = fileUrl,
                type = attachmentType,
                fileSizeBytes = file.Length,
                caption = caption,
                displayOrder = displayOrder
            });
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrEmpty(fileUrl))
            {
                try
                {
                    await _fileStorage.DeleteFileAsync(fileUrl);
                }
                catch (Exception deleteEx)
                {
                    _logger.LogError(deleteEx, "Failed to clean up uploaded file after attachment creation error");
                }
            }

            _logger.LogError(ex, "Article attachment upload failed");
            return StatusCode(500, new { error = "Upload failed. Please try again." });
        }
    }

    /// <summary>
    /// Delete an attachment
    /// </summary>
    [HttpDelete("{attachmentId:guid}")]
    public async Task<IActionResult> Delete(Guid articleId, Guid attachmentId)
    {
        // Get attachment to get file URL for deletion
        var query = new GetArticleAttachmentsQuery(articleId);
        var attachments = await Sender.Send(query);
        var attachment = attachments.FirstOrDefault(a => a.Id == attachmentId);

        if (attachment == null)
            return NotFound();

        // Delete file from storage
        await _fileStorage.DeleteFileAsync(attachment.FileUrl);

        // Delete database record
        var command = new RemoveArticleAttachmentCommand(attachmentId);
        await Sender.Send(command);

        return NoContent();
    }
}
