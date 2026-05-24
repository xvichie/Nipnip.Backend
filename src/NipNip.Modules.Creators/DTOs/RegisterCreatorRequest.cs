namespace NipNip.Modules.Creators.DTOs;

public record RegisterCreatorRequest(
    string Name,
    string Slug,
    string? AvatarUrl,
    string? InstagramHandle,
    string? TiktokHandle
);
