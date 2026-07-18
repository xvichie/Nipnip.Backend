namespace NipNip.Modules.Creators.DTOs;

public record CreatorResponse(
    Guid Id,
    string Name,
    string Slug,
    string? AvatarUrl,
    string? InstagramHandle,
    int? InstagramFollowers,
    string? TiktokHandle,
    int? TiktokFollowers,
    string? YoutubeHandle,
    int? YoutubeFollowers,
    string? FacebookHandle,
    int? FacebookFollowers,
    string? XHandle,
    int? XFollowers,
    string? LinkedinHandle,
    int? LinkedinFollowers,
    bool IsActive,
    bool IsHighlighted,
    bool IsTest,
    DateTimeOffset CreatedAt
);
