namespace NipNip.Modules.Storefronts.DTOs;

public record TikTokConnectUrlResponse(string Url);

public record TikTokStatusResponse(bool Connected, string? DisplayName);

public record TikTokProductPreviewResponse(List<string> ImageUrls, string Title, string Description);

public record TikTokPublishRequest(string Title, string Description);

public record TikTokPublishImagesRequest(List<string> ImageUrls, string Title, string Description);

public record TikTokPublishResponse(bool Posted, string PrivacyLevel);
