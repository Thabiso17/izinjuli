using iDiski.Application.AuditLogs;
using iDiski.Application.AuditLogs.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iDiski.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "SuperAdminOnly")]
public class AuditLogsController : BaseApiController
{
    /// <summary>
    /// Get audit logs with optional filtering (SuperAdmin only).
    /// </summary>
    /// <param name="entityType">Optional: Filter by entity type (e.g., "Team", "Player")</param>
    /// <param name="entityId">Optional: Filter by specific entity ID</param>
    /// <param name="userId">Optional: Filter by user who made the change</param>
    /// <param name="fromDate">Optional: Filter from this date (UTC)</param>
    /// <param name="toDate">Optional: Filter until this date (UTC)</param>
    /// <param name="pageNumber">Default 1</param>
    /// <param name="pageSize">Default 50, max 100</param>
    /// <response code="200">List of audit logs.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Not authorized (SuperAdmin only).</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? entityId = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        // Limit page size to prevent large data transfers
        pageSize = Math.Min(pageSize, 100);

        var query = new GetAuditLogsQuery(
            entityType,
            entityId,
            userId,
            fromDate,
            toDate,
            pageNumber,
            pageSize);

        var auditLogs = await Sender.Send(query, ct);
        return Ok(auditLogs);
    }

    /// <summary>
    /// Get audit logs for a specific entity (SuperAdmin only).
    /// Shows the complete change history for an entity.
    /// </summary>
    /// <param name="entityType">Entity type (e.g., "Team", "Player")</param>
    /// <param name="entityId">Entity ID</param>
    /// <response code="200">List of audit logs for the entity.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Not authorized (SuperAdmin only).</response>
    [HttpGet("{entityType}/{entityId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEntityHistory(
        string entityType,
        Guid entityId,
        CancellationToken ct = default)
    {
        var query = new GetAuditLogsQuery(
            EntityType: entityType,
            EntityId: entityId,
            PageSize: 1000); // No pagination for history

        var auditLogs = await Sender.Send(query, ct);
        return Ok(auditLogs);
    }
}
