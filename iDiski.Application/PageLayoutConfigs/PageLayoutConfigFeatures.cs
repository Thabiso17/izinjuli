using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.PageLayoutConfigs;

// ── DTO ───────────────────────────────────────────────────────────────────────

public sealed record PageLayoutConfigDto(
    Guid    Id,
    string  PageName,
    string  ComponentName,
    int     DisplayOrder,
    bool    IsVisible,
    string? ConfigJson,
    string  ModifiedByUser
);

// ═════════════════════════════════════════════════════════════════════════════
// QUERY
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Returns the ordered, visible component list for a given page.
/// The Angular app calls GET /api/page-layout?pageName=main on init
/// and renders components in DisplayOrder sequence.
/// </summary>
public sealed record GetPageLayoutQuery(string PageName)
    : IRequest<IReadOnlyList<PageLayoutConfigDto>>;

public sealed class GetPageLayoutQueryHandler
    : IRequestHandler<GetPageLayoutQuery, IReadOnlyList<PageLayoutConfigDto>>
{
    private readonly ILeagueDbContext _db;

    public GetPageLayoutQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<IReadOnlyList<PageLayoutConfigDto>> Handle(
        GetPageLayoutQuery request,
        CancellationToken cancellationToken)
    {
        return await _db.PageLayoutConfigs
            .AsNoTracking()
            .Where(p => p.PageName == request.PageName)
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new PageLayoutConfigDto(
                p.Id, p.PageName, p.ComponentName,
                p.DisplayOrder, p.IsVisible, p.ConfigJson, p.ModifiedByUser))
            .ToListAsync(cancellationToken);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// COMMANDS
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Upserts a single component's layout entry.
/// Idempotent: creates if (PageName + ComponentName) doesn't exist, updates if it does.
/// Call this per-component from the Angular drag-and-drop admin panel.
/// </summary>
public sealed record UpsertPageLayoutCommand(
    string  PageName,
    string  ComponentName,
    int     DisplayOrder,
    bool    IsVisible,
    string? ConfigJson,
    string  ModifiedByUser
) : IRequest<Guid>;

public sealed class UpsertPageLayoutCommandValidator
    : AbstractValidator<UpsertPageLayoutCommand>
{
    public UpsertPageLayoutCommandValidator()
    {
        RuleFor(x => x.PageName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ComponentName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ModifiedByUser).NotEmpty().MaximumLength(100);
    }
}

public sealed class UpsertPageLayoutCommandHandler
    : IRequestHandler<UpsertPageLayoutCommand, Guid>
{
    private readonly ILeagueDbContext _db;

    public UpsertPageLayoutCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task<Guid> Handle(
        UpsertPageLayoutCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await _db.PageLayoutConfigs
            .FirstOrDefaultAsync(
                p => p.PageName == request.PageName &&
                     p.ComponentName == request.ComponentName,
                cancellationToken);

        if (existing is not null)
        {
            existing.DisplayOrder   = request.DisplayOrder;
            existing.IsVisible      = request.IsVisible;
            existing.ConfigJson     = request.ConfigJson;
            existing.ModifiedByUser = request.ModifiedByUser;

            await _db.SaveChangesAsync(cancellationToken);
            return existing.Id;
        }

        var config = new PageLayoutConfig
        {
            PageName       = request.PageName,
            ComponentName  = request.ComponentName,
            DisplayOrder   = request.DisplayOrder,
            IsVisible      = request.IsVisible,
            ConfigJson     = request.ConfigJson,
            ModifiedByUser = request.ModifiedByUser
        };

        _db.PageLayoutConfigs.Add(config);
        await _db.SaveChangesAsync(cancellationToken);

        return config.Id;
    }
}

/// <summary>Bulk-replaces the entire layout for a page in one call.</summary>
public sealed record BulkUpdatePageLayoutCommand(
    string PageName,
    IReadOnlyList<UpsertPageLayoutCommand> Components,
    string ModifiedByUser
) : IRequest;

public sealed class BulkUpdatePageLayoutCommandHandler
    : IRequestHandler<BulkUpdatePageLayoutCommand>
{
    private readonly ILeagueDbContext _db;

    public BulkUpdatePageLayoutCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task Handle(
        BulkUpdatePageLayoutCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await _db.PageLayoutConfigs
            .Where(p => p.PageName == request.PageName)
            .ToListAsync(cancellationToken);

        foreach (var component in request.Components)
        {
            var row = existing.FirstOrDefault(e => e.ComponentName == component.ComponentName);

            if (row is not null)
            {
                row.DisplayOrder   = component.DisplayOrder;
                row.IsVisible      = component.IsVisible;
                row.ConfigJson     = component.ConfigJson;
                row.ModifiedByUser = request.ModifiedByUser;
            }
            else
            {
                _db.PageLayoutConfigs.Add(new PageLayoutConfig
                {
                    PageName       = request.PageName,
                    ComponentName  = component.ComponentName,
                    DisplayOrder   = component.DisplayOrder,
                    IsVisible      = component.IsVisible,
                    ConfigJson     = component.ConfigJson,
                    ModifiedByUser = request.ModifiedByUser
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
