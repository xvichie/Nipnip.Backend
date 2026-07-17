namespace NipNip.Modules.Storefronts.DTOs;

public record FacebookConnectUrlResponse(string Url);

public record FacebookStatusResponse(bool Connected, string? PageName);

public record FacebookPendingPageResponse(string Id, string Name);

public record SelectFacebookPageRequest(string Pending, string PageId);

public record FacebookPostSummaryResponse(string Id, string? Message, DateTimeOffset CreatedTime, string? ThumbnailUrl, bool HasVideo);

public record FacebookPostDetailResponse(string? Message, List<string> ImageUrls, string? VideoUrl);

public record FacebookProductPreviewResponse(string Message, string? ImageUrl);

public record FacebookPublishRequest(string? Message);

public record FacebookPublishResponse(string PostUrl);
