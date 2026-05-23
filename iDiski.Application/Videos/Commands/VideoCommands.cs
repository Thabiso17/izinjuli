using FluentValidation;
using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Entities;
using MediatR;

namespace iDiski.Application.Videos.Commands;

// ═════════════════════════════════════════════════════════════════════════════
// CREATE VIDEO
// ═════════════════════════════════════════════════════════════════════════════

public sealed record CreateVideoCommand(
    string  Title,
    string  VideoUrl,
    string? Description,
    string? ThumbnailUrl,
    string  Author,
    bool    PublishImmediately = false
) : IRequest<Guid>;

public sealed class CreateVideoCommandValidator : AbstractValidator<CreateVideoCommand>
{
    public CreateVideoCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.VideoUrl).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Author).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description != null);
    }
}

public sealed class CreateVideoCommandHandler : IRequestHandler<CreateVideoCommand, Guid>
{
    private readonly ILeagueDbContext _db;

    public CreateVideoCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateVideoCommand request, CancellationToken cancellationToken)
    {
        var video = new Video
        {
            Title = request.Title.Trim(),
            VideoUrl = request.VideoUrl.Trim(),
            Description = request.Description?.Trim(),
            ThumbnailUrl = request.ThumbnailUrl?.Trim(),
            Author = request.Author.Trim(),
            IsPublished = request.PublishImmediately,
            PublishedAt = request.PublishImmediately ? DateTime.UtcNow : null
        };

        _db.Videos.Add(video);
        await _db.SaveChangesAsync(cancellationToken);

        return video.Id;
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// UPDATE VIDEO
// ═════════════════════════════════════════════════════════════════════════════

public sealed record UpdateVideoCommand(
    Guid    Id,
    string  Title,
    string  VideoUrl,
    string? Description,
    string? ThumbnailUrl,
    string  Author,
    bool    IsPinned = false
) : IRequest;

public sealed class UpdateVideoCommandValidator : AbstractValidator<UpdateVideoCommand>
{
    public UpdateVideoCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.VideoUrl).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Author).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description != null);
    }
}

public sealed class UpdateVideoCommandHandler : IRequestHandler<UpdateVideoCommand>
{
    private readonly ILeagueDbContext _db;

    public UpdateVideoCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task Handle(UpdateVideoCommand request, CancellationToken cancellationToken)
    {
        var video = await _db.Videos.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException(nameof(Video), request.Id);

        video.Title = request.Title.Trim();
        video.VideoUrl = request.VideoUrl.Trim();
        video.Description = request.Description?.Trim();
        video.ThumbnailUrl = request.ThumbnailUrl?.Trim();
        video.Author = request.Author.Trim();
        video.IsPinned = request.IsPinned;

        await _db.SaveChangesAsync(cancellationToken);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PUBLISH VIDEO
// ═════════════════════════════════════════════════════════════════════════════

public sealed record PublishVideoCommand(Guid Id) : IRequest;

public sealed class PublishVideoCommandHandler : IRequestHandler<PublishVideoCommand>
{
    private readonly ILeagueDbContext _db;

    public PublishVideoCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task Handle(PublishVideoCommand request, CancellationToken cancellationToken)
    {
        var video = await _db.Videos.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException(nameof(Video), request.Id);

        video.IsPublished = true;
        video.PublishedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// UNPUBLISH VIDEO
// ═════════════════════════════════════════════════════════════════════════════

public sealed record UnpublishVideoCommand(Guid Id) : IRequest;

public sealed class UnpublishVideoCommandHandler : IRequestHandler<UnpublishVideoCommand>
{
    private readonly ILeagueDbContext _db;

    public UnpublishVideoCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task Handle(UnpublishVideoCommand request, CancellationToken cancellationToken)
    {
        var video = await _db.Videos.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException(nameof(Video), request.Id);

        video.IsPublished = false;
        video.IsPinned = false; // Unpublishing also unpins

        await _db.SaveChangesAsync(cancellationToken);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// TOGGLE PIN VIDEO
// ═════════════════════════════════════════════════════════════════════════════

public sealed record TogglePinVideoCommand(Guid Id) : IRequest;

public sealed class TogglePinVideoCommandHandler : IRequestHandler<TogglePinVideoCommand>
{
    private readonly ILeagueDbContext _db;

    public TogglePinVideoCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task Handle(TogglePinVideoCommand request, CancellationToken cancellationToken)
    {
        var video = await _db.Videos.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException(nameof(Video), request.Id);

        if (!video.IsPublished)
            throw new InvalidOperationException("Cannot pin an unpublished video.");

        video.IsPinned = !video.IsPinned;

        await _db.SaveChangesAsync(cancellationToken);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// DELETE VIDEO
// ═════════════════════════════════════════════════════════════════════════════

public sealed record DeleteVideoCommand(Guid Id) : IRequest;

public sealed class DeleteVideoCommandHandler : IRequestHandler<DeleteVideoCommand>
{
    private readonly ILeagueDbContext _db;

    public DeleteVideoCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task Handle(DeleteVideoCommand request, CancellationToken cancellationToken)
    {
        var video = await _db.Videos.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException(nameof(Video), request.Id);

        _db.Videos.Remove(video);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
