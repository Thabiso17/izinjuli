using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Sponsors;

// ── DTO ───────────────────────────────────────────────────────────────────────

public sealed record SponsorDto(
    Guid        Id,
    string      Name,
    string?     LogoUrl,
    string?     WebsiteUrl,
    string?     AdImageUrl,
    string?     AdLinkUrl,
    SponsorTier Tier,
    AdPlacement Placement,
    bool        IsActive,
    DateTime?   ContractStart,
    DateTime?   ContractEnd,
    int         DisplayOrder
);

// ═════════════════════════════════════════════════════════════════════════════
// QUERIES
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Returns active sponsors for a given placement, ordered by DisplayOrder.
/// The Angular ad rotator calls this on page init.
/// </summary>
public sealed record GetActiveSponsorsByPlacementQuery(AdPlacement Placement)
    : IRequest<IReadOnlyList<SponsorDto>>;

public sealed class GetActiveSponsorsByPlacementQueryHandler
    : IRequestHandler<GetActiveSponsorsByPlacementQuery, IReadOnlyList<SponsorDto>>
{
    private readonly ILeagueDbContext _db;

    public GetActiveSponsorsByPlacementQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<IReadOnlyList<SponsorDto>> Handle(
        GetActiveSponsorsByPlacementQuery request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        return await _db.Sponsors
            .AsNoTracking()
            .Where(s =>
                s.IsActive &&
                s.Placement == request.Placement &&
                (s.ContractEnd == null || s.ContractEnd >= now))
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new SponsorDto(
                s.Id, s.Name, s.LogoUrl, s.WebsiteUrl, s.AdImageUrl,
                s.AdLinkUrl, s.Tier, s.Placement, s.IsActive,
                s.ContractStart, s.ContractEnd, s.DisplayOrder))
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Returns all sponsors (for admin panel), ordered by DisplayOrder.
/// </summary>
public sealed record GetAllSponsorsQuery : IRequest<IReadOnlyList<SponsorDto>>;

public sealed class GetAllSponsorsQueryHandler
    : IRequestHandler<GetAllSponsorsQuery, IReadOnlyList<SponsorDto>>
{
    private readonly ILeagueDbContext _db;

    public GetAllSponsorsQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<IReadOnlyList<SponsorDto>> Handle(
        GetAllSponsorsQuery request,
        CancellationToken cancellationToken)
    {
        return await _db.Sponsors
            .AsNoTracking()
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new SponsorDto(
                s.Id, s.Name, s.LogoUrl, s.WebsiteUrl, s.AdImageUrl,
                s.AdLinkUrl, s.Tier, s.Placement, s.IsActive,
                s.ContractStart, s.ContractEnd, s.DisplayOrder))
            .ToListAsync(cancellationToken);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// COMMANDS
// ═════════════════════════════════════════════════════════════════════════════

public sealed record CreateSponsorCommand(
    string      Name,
    string?     LogoUrl,
    string?     WebsiteUrl,
    string?     AdImageUrl,
    string?     AdLinkUrl,
    SponsorTier Tier,
    AdPlacement Placement,
    DateTime?   ContractStart,
    DateTime?   ContractEnd,
    int         DisplayOrder
) : IRequest<Guid>;

public sealed class CreateSponsorCommandValidator : AbstractValidator<CreateSponsorCommand>
{
    public CreateSponsorCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);

        RuleFor(x => x.ContractEnd)
            .GreaterThan(x => x.ContractStart)
            .WithMessage("ContractEnd must be after ContractStart.")
            .When(x => x.ContractStart.HasValue && x.ContractEnd.HasValue);

        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateSponsorCommandHandler
    : IRequestHandler<CreateSponsorCommand, Guid>
{
    private readonly ILeagueDbContext _db;

    public CreateSponsorCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task<Guid> Handle(
        CreateSponsorCommand request,
        CancellationToken cancellationToken)
    {
        var sponsor = new Sponsor
        {
            Name          = request.Name,
            LogoUrl       = request.LogoUrl,
            WebsiteUrl    = request.WebsiteUrl,
            AdImageUrl    = request.AdImageUrl,
            AdLinkUrl     = request.AdLinkUrl,
            Tier          = request.Tier,
            Placement     = request.Placement,
            ContractStart = request.ContractStart,
            ContractEnd   = request.ContractEnd,
            DisplayOrder  = request.DisplayOrder,
            IsActive      = true
        };

        _db.Sponsors.Add(sponsor);
        await _db.SaveChangesAsync(cancellationToken);

        return sponsor.Id;
    }
}

public sealed record UpdateSponsorCommand(
    Guid        Id,
    string      Name,
    string?     LogoUrl,
    string?     WebsiteUrl,
    string?     AdImageUrl,
    string?     AdLinkUrl,
    SponsorTier Tier,
    AdPlacement Placement,
    bool        IsActive,
    DateTime?   ContractStart,
    DateTime?   ContractEnd,
    int         DisplayOrder
) : IRequest;

public sealed class UpdateSponsorCommandValidator : AbstractValidator<UpdateSponsorCommand>
{
    public UpdateSponsorCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);

        RuleFor(x => x.ContractEnd)
            .GreaterThan(x => x.ContractStart)
            .WithMessage("ContractEnd must be after ContractStart.")
            .When(x => x.ContractStart.HasValue && x.ContractEnd.HasValue);

        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateSponsorCommandHandler
    : IRequestHandler<UpdateSponsorCommand>
{
    private readonly ILeagueDbContext _db;

    public UpdateSponsorCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task Handle(UpdateSponsorCommand request, CancellationToken cancellationToken)
    {
        var sponsor = await _db.Sponsors.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException(nameof(Sponsor), request.Id);

        sponsor.Name          = request.Name;
        sponsor.LogoUrl       = request.LogoUrl;
        sponsor.WebsiteUrl    = request.WebsiteUrl;
        sponsor.AdImageUrl    = request.AdImageUrl;
        sponsor.AdLinkUrl     = request.AdLinkUrl;
        sponsor.Tier          = request.Tier;
        sponsor.Placement     = request.Placement;
        sponsor.IsActive      = request.IsActive;
        sponsor.ContractStart = request.ContractStart;
        sponsor.ContractEnd   = request.ContractEnd;
        sponsor.DisplayOrder  = request.DisplayOrder;

        await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record DeleteSponsorCommand(Guid Id) : IRequest;

public sealed class DeleteSponsorCommandHandler
    : IRequestHandler<DeleteSponsorCommand>
{
    private readonly ILeagueDbContext _db;

    public DeleteSponsorCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task Handle(DeleteSponsorCommand request, CancellationToken cancellationToken)
    {
        var sponsor = await _db.Sponsors.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException(nameof(Sponsor), request.Id);

        _db.Sponsors.Remove(sponsor);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record ToggleSponsorActiveCommand(Guid Id) : IRequest;

public sealed class ToggleSponsorActiveCommandHandler
    : IRequestHandler<ToggleSponsorActiveCommand>
{
    private readonly ILeagueDbContext _db;

    public ToggleSponsorActiveCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task Handle(ToggleSponsorActiveCommand request, CancellationToken cancellationToken)
    {
        var sponsor = await _db.Sponsors.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException(nameof(Sponsor), request.Id);

        sponsor.IsActive = !sponsor.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
