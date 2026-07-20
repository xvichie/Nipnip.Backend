using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts.TikTok;

public record TikTokTokenResult(string AccessToken, string RefreshToken, int ExpiresInSeconds, string OpenId, string? Scope);

public record TikTokUserInfo(string DisplayName, string? AvatarUrl);

public record TikTokCreatorInfo(List<string> PrivacyLevelOptions, string? CreatorNickname);

public class TikTokApiClient(IHttpClientFactory httpClientFactory, IOptions<TikTokOptions> options, ILogger<TikTokApiClient> logger)
{
    private const string BaseUrl = "https://open.tiktokapis.com";
    private readonly TikTokOptions _options = options.Value;

    public Task<TikTokTokenResult> ExchangeCodeAsync(string code) =>
        RequestTokenAsync(new Dictionary<string, string>
        {
            ["client_key"] = _options.ClientKey,
            ["client_secret"] = _options.ClientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = _options.RedirectUri,
        });

    public Task<TikTokTokenResult> RefreshTokenAsync(string refreshToken) =>
        RequestTokenAsync(new Dictionary<string, string>
        {
            ["client_key"] = _options.ClientKey,
            ["client_secret"] = _options.ClientSecret,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        });

    private async Task<TikTokTokenResult> RequestTokenAsync(Dictionary<string, string> form)
    {
        var client = httpClientFactory.CreateClient();
        var response = await client.PostAsync($"{BaseUrl}/v2/oauth/token/", new FormUrlEncodedContent(form));
        var json = await response.Content.ReadAsStringAsync();
        var node = JsonNode.Parse(json);

        if (!response.IsSuccessStatusCode || node?["access_token"] is null)
        {
            var message = node?["error_description"]?.GetValue<string>() ?? node?["error"]?.GetValue<string>();
            throw new ArgumentException(message ?? "Failed to authenticate with TikTok.");
        }

        return new TikTokTokenResult(
            node["access_token"]!.GetValue<string>(),
            node["refresh_token"]!.GetValue<string>(),
            node["expires_in"]!.GetValue<int>(),
            node["open_id"]!.GetValue<string>(),
            node["scope"]?.GetValue<string>()
        );
    }

    public async Task<TikTokUserInfo> GetUserInfoAsync(string accessToken)
    {
        var client = CreateJsonClient(accessToken);
        var response = await client.GetAsync($"{BaseUrl}/v2/user/info/?fields=display_name,avatar_url");
        var node = await ParseAsync(response, "Could not fetch your TikTok profile.");

        var user = node["data"]?["user"];
        return new TikTokUserInfo(
            user?["display_name"]?.GetValue<string>() ?? "TikTok account",
            user?["avatar_url"]?.GetValue<string>()
        );
    }

    public async Task<TikTokCreatorInfo> QueryCreatorInfoAsync(string accessToken)
    {
        var client = CreateJsonClient(accessToken);
        var response = await client.PostAsync($"{BaseUrl}/v2/post/publish/creator_info/query/", null);
        var node = await ParseAsync(response, "Could not query your TikTok posting settings.");

        var data = node["data"];
        var options = data?["privacy_level_options"] is JsonArray arr
            ? arr.Select(v => v!.GetValue<string>()).ToList()
            : [];

        return new TikTokCreatorInfo(options, data?["creator_nickname"]?.GetValue<string>());
    }

    public async Task<string> InitPhotoPostAsync(string accessToken, string title, string description, string privacyLevel, List<string> photoImageUrls, int coverIndex)
    {
        var client = CreateJsonClient(accessToken);
        var body = new
        {
            media_type = "PHOTO",
            post_mode = "DIRECT_POST",
            post_info = new
            {
                title,
                description,
                privacy_level = privacyLevel,
                disable_comment = false,
                // Required commercial-content disclosure for Direct Post. Merchants are always
                // publishing photos of their own products/store, never paid third-party content.
                brand_content_toggle = false,
                brand_organic_toggle = true,
            },
            source_info = new
            {
                source = "PULL_FROM_URL",
                photo_images = photoImageUrls,
                photo_cover_index = coverIndex,
            },
        };

        var response = await client.PostAsync(
            $"{BaseUrl}/v2/post/publish/content/init/",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
        var node = await ParseAsync(response, "Could not publish to TikTok.");

        return node["data"]?["publish_id"]?.GetValue<string>()
            ?? throw new ArgumentException("TikTok did not return a publish id.");
    }

    public async Task<string> GetPublishStatusAsync(string accessToken, string publishId)
    {
        var client = CreateJsonClient(accessToken);
        var response = await client.PostAsync(
            $"{BaseUrl}/v2/post/publish/status/fetch/",
            new StringContent(JsonSerializer.Serialize(new { publish_id = publishId }), Encoding.UTF8, "application/json"));
        var node = await ParseAsync(response, "Could not check the TikTok post's status.");

        return node["data"]?["status"]?.GetValue<string>() ?? "UNKNOWN";
    }

    private HttpClient CreateJsonClient(string accessToken)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private async Task<JsonNode> ParseAsync(HttpResponseMessage response, string fallbackMessage)
    {
        var json = await response.Content.ReadAsStringAsync();
        JsonNode? node = null;
        try { node = JsonNode.Parse(json); } catch { /* fall through to fallback message */ }

        var errorCode = node?["error"]?["code"]?.GetValue<string>();
        if (!response.IsSuccessStatusCode || (errorCode is not null && errorCode != "ok"))
        {
            var message = node?["error"]?["message"]?.GetValue<string>();
            logger.LogWarning(
                "TikTok API error on {Url}: status={Status} code={Code} body={Body}",
                response.RequestMessage?.RequestUri, (int)response.StatusCode, errorCode, json);
            throw new ArgumentException(message ?? fallbackMessage);
        }

        return node ?? new JsonObject();
    }
}
