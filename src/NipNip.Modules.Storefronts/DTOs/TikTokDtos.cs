namespace NipNip.Modules.Storefronts.DTOs;

public record TikTokConnectUrlResponse(string Url);

public record TikTokStatusResponse(bool Connected, string? DisplayName);

public record TikTokProductPreviewResponse(List<string> ImageUrls, string Title, string Description);

public record TikTokPublishRequest(string? Title = null, string? Description = null, string? Hashtags = null, bool AutoAddMusic = false);

public record TikTokPublishImagesRequest(List<string> ImageUrls, string Title, string Description, bool AutoAddMusic = false);

public record TikTokPublishResponse(bool Posted, string PrivacyLevel);
