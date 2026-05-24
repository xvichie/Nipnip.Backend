namespace NipNip.Modules.Creators.DTOs;

public record UpdateCreatorRequest(
    string? Name,
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
    int? LinkedinFollowers
);
