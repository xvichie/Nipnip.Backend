namespace NipNip.Modules.Storefronts.DTOs;

public record InstagramStatusResponse(bool Connected, string? Username);

public record InstagramMediaSummaryResponse(string Id, string? Caption, string? ThumbnailUrl, bool HasVideo);

public record InstagramMediaDetailResponse(string? Caption, List<string> ImageUrls, string? VideoUrl);

public record InstagramProductPreviewResponse(string Message, string? ImageUrl);

public record InstagramPublishRequest(string? Message);

public record InstagramPublishResponse(string PostUrl);
