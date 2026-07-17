using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts;

public record FacebookPageAccount(string Id, string Name, string AccessToken);

public record FacebookPostSummary(string Id, string? Message, DateTimeOffset CreatedTime, string? ThumbnailUrl, bool HasVideo);

public record FacebookPostDetail(string? Message, List<string> ImageUrls, string? VideoNodeId);

public record InstagramAccount(string Id, string? Username);

public record InstagramMediaSummary(string Id, string? Caption, string? ThumbnailUrl, bool HasVideo);

public record InstagramMediaDetail(string? Caption, List<string> ImageUrls, string? VideoUrl);

public class GraphApiClient(IHttpClientFactory httpClientFactory, IOptions<FacebookOptions> options)
{
    private const string ApiVersion = "v21.0";
    private readonly FacebookOptions _options = options.Value;

    public async Task<string> ExchangeCodeAsync(string code)
    {
        var client = CreateClient();
        var url = $"/{ApiVersion}/oauth/access_token?client_id={_options.AppId}" +
                  $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}" +
                  $"&client_secret={_options.AppSecret}&code={Uri.EscapeDataString(code)}";

        var response = await client.GetAsync(url);
        var json = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, json, "Failed to complete Facebook sign-in.");

        return JsonNode.Parse(json)?["access_token"]?.GetValue<string>()
            ?? throw new ArgumentException("Facebook did not return an access token.");
    }

    public async Task<string> ExchangeLongLivedTokenAsync(string shortLivedToken)
    {
        var client = CreateClient();
        var url = $"/{ApiVersion}/oauth/access_token?grant_type=fb_exchange_token&client_id={_options.AppId}" +
                  $"&client_secret={_options.AppSecret}&fb_exchange_token={Uri.EscapeDataString(shortLivedToken)}";

        var response = await client.GetAsync(url);
        var json = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, json, "Failed to establish a lasting Facebook connection.");

        return JsonNode.Parse(json)?["access_token"]?.GetValue<string>()
            ?? throw new ArgumentException("Facebook did not return a long-lived access token.");
    }

    public async Task<List<FacebookPageAccount>> GetManagedPagesAsync(string userAccessToken)
    {
        var client = CreateClient();
        var response = await client.GetAsync($"/{ApiVersion}/me/accounts?access_token={Uri.EscapeDataString(userAccessToken)}");
        var json = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, json, "Failed to list the Facebook Pages you manage.");

        var pages = new List<FacebookPageAccount>();
        if (JsonNode.Parse(json)?["data"] is JsonArray data)
        {
            foreach (var p in data)
            {
                var id = p?["id"]?.GetValue<string>();
                var name = p?["name"]?.GetValue<string>();
                var token = p?["access_token"]?.GetValue<string>();
                if (id is not null && name is not null && token is not null)
                    pages.Add(new FacebookPageAccount(id, name, token));
            }
        }
        return pages;
    }

    public async Task<List<FacebookPostSummary>> GetPagePostsAsync(string pageId, string pageAccessToken)
    {
        var client = CreateClient();
        const string fields = "id,message,created_time,attachments{type,media,subattachments{type,media}}";
        var response = await client.GetAsync(
            $"/{ApiVersion}/{pageId}/posts?fields={fields}&limit=25&access_token={Uri.EscapeDataString(pageAccessToken)}");
        var json = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, json, "Failed to load recent posts from your Facebook Page.");

        var posts = new List<FacebookPostSummary>();
        if (JsonNode.Parse(json)?["data"] is JsonArray data)
        {
            foreach (var p in data)
            {
                var id = p?["id"]?.GetValue<string>();
                if (id is null) continue;

                var message = p?["message"]?.GetValue<string>();
                var createdTime = p?["created_time"] is { } ct && DateTimeOffset.TryParse(ct.GetValue<string>(), out var parsed)
                    ? parsed
                    : DateTimeOffset.UtcNow;
                var (imageUrls, hasVideo, _) = ExtractMedia(p?["attachments"]);
                posts.Add(new FacebookPostSummary(id, message, createdTime, imageUrls.Count > 0 ? imageUrls[0] : null, hasVideo));
            }
        }
        return posts;
    }

    public async Task<FacebookPostDetail> GetPostDetailAsync(string postId, string pageAccessToken)
    {
        var client = CreateClient();
        const string fields = "id,message,attachments{type,media,target,subattachments{type,media,target}}";
        var response = await client.GetAsync($"/{ApiVersion}/{postId}?fields={fields}&access_token={Uri.EscapeDataString(pageAccessToken)}");
        var json = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, json, "Failed to load that Facebook post.");

        var node = JsonNode.Parse(json);
        var message = node?["message"]?.GetValue<string>();
        var (imageUrls, hasVideo, videoNodeId) = ExtractMedia(node?["attachments"]);
        return new FacebookPostDetail(message, imageUrls, hasVideo ? videoNodeId : null);
    }

    public async Task<string?> GetVideoSourceAsync(string videoId, string pageAccessToken)
    {
        var client = CreateClient();
        var response = await client.GetAsync($"/{ApiVersion}/{videoId}?fields=source&access_token={Uri.EscapeDataString(pageAccessToken)}");
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(json)?["source"]?.GetValue<string>();
    }

    // With an image: /{page-id}/photos publishes the photo and creates the feed post in
    // one call. Without one: /{page-id}/feed posts plain text. Either response shape
    // carries the resulting post id under "post_id" (photos) or "id" (feed).
    public async Task<string> PublishPostAsync(string pageId, string pageAccessToken, string message, string? imageUrl)
    {
        var client = CreateClient();
        var url = imageUrl is not null
            ? $"/{ApiVersion}/{pageId}/photos?url={Uri.EscapeDataString(imageUrl)}&caption={Uri.EscapeDataString(message)}&access_token={Uri.EscapeDataString(pageAccessToken)}"
            : $"/{ApiVersion}/{pageId}/feed?message={Uri.EscapeDataString(message)}&access_token={Uri.EscapeDataString(pageAccessToken)}";

        var response = await client.PostAsync(url, null);
        var json = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, json, "Failed to publish to your Facebook Page.");

        var node = JsonNode.Parse(json);
        return node?["post_id"]?.GetValue<string>() ?? node?["id"]?.GetValue<string>()
            ?? throw new ArgumentException("Facebook did not return a post id.");
    }

    // The same Page access token that reads/posts Facebook content also authorizes
    // Graph API calls against whatever Instagram Business/Creator account is linked to
    // that Page — no separate Instagram OAuth needed.
    public async Task<InstagramAccount?> GetInstagramBusinessAccountAsync(string pageId, string pageAccessToken)
    {
        var client = CreateClient();
        var response = await client.GetAsync(
            $"/{ApiVersion}/{pageId}?fields=instagram_business_account{{id,username}}&access_token={Uri.EscapeDataString(pageAccessToken)}");
        var json = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, json, "Failed to look up your linked Instagram account.");

        var node = JsonNode.Parse(json)?["instagram_business_account"];
        var id = node?["id"]?.GetValue<string>();
        return id is null ? null : new InstagramAccount(id, node?["username"]?.GetValue<string>());
    }

    public async Task<List<InstagramMediaSummary>> GetInstagramMediaAsync(string igUserId, string pageAccessToken)
    {
        var client = CreateClient();
        const string fields = "id,caption,media_type,media_url,thumbnail_url,timestamp";
        var response = await client.GetAsync(
            $"/{ApiVersion}/{igUserId}/media?fields={fields}&limit=25&access_token={Uri.EscapeDataString(pageAccessToken)}");
        var json = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, json, "Failed to load recent posts from your Instagram account.");

        var items = new List<InstagramMediaSummary>();
        if (JsonNode.Parse(json)?["data"] is JsonArray data)
        {
            foreach (var m in data)
            {
                var id = m?["id"]?.GetValue<string>();
                if (id is null) continue;

                var caption = m?["caption"]?.GetValue<string>();
                var isVideo = m?["media_type"]?.GetValue<string>() == "VIDEO";
                // Videos only expose a still via thumbnail_url; media_url on a video post is the clip itself.
                var thumbnailUrl = isVideo ? m?["thumbnail_url"]?.GetValue<string>() : m?["media_url"]?.GetValue<string>();
                items.Add(new InstagramMediaSummary(id, caption, thumbnailUrl, isVideo));
            }
        }
        return items;
    }

    // Unlike Facebook, Instagram's media_url is already the final playable/downloadable
    // URL for both images and videos — no separate "resolve video source" step needed.
    public async Task<InstagramMediaDetail> GetInstagramMediaDetailAsync(string mediaId, string pageAccessToken)
    {
        var client = CreateClient();
        const string fields = "caption,media_type,media_url,children{media_type,media_url}";
        var response = await client.GetAsync($"/{ApiVersion}/{mediaId}?fields={fields}&access_token={Uri.EscapeDataString(pageAccessToken)}");
        var json = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, json, "Failed to load that Instagram post.");

        var node = JsonNode.Parse(json);
        var caption = node?["caption"]?.GetValue<string>();
        var mediaType = node?["media_type"]?.GetValue<string>();

        var imageUrls = new List<string>();
        string? videoUrl = null;

        if (mediaType == "CAROUSEL_ALBUM" && node?["children"]?["data"] is JsonArray children)
        {
            foreach (var child in children)
            {
                var childUrl = child?["media_url"]?.GetValue<string>();
                if (childUrl is null) continue;

                if (child?["media_type"]?.GetValue<string>() == "VIDEO")
                    videoUrl ??= childUrl;
                else
                    imageUrls.Add(childUrl);
            }
        }
        else if (mediaType == "VIDEO")
        {
            videoUrl = node?["media_url"]?.GetValue<string>();
        }
        else
        {
            var url = node?["media_url"]?.GetValue<string>();
            if (url is not null) imageUrls.Add(url);
        }

        return new InstagramMediaDetail(caption, imageUrls, videoUrl);
    }

    // Instagram publishing is a two-step container flow, unlike Facebook's one-call
    // /photos: create a media container from the image, then publish that container.
    // Feed posts require an image — there's no Instagram equivalent of a text-only post.
    public async Task<string> PublishInstagramPostAsync(string igUserId, string pageAccessToken, string caption, string imageUrl, string? username)
    {
        var client = CreateClient();

        var createUrl = $"/{ApiVersion}/{igUserId}/media?image_url={Uri.EscapeDataString(imageUrl)}" +
                         $"&caption={Uri.EscapeDataString(caption)}&access_token={Uri.EscapeDataString(pageAccessToken)}";
        var createResponse = await client.PostAsync(createUrl, null);
        var createJson = await createResponse.Content.ReadAsStringAsync();
        EnsureSuccess(createResponse, createJson, "Failed to prepare your Instagram post.");
        var creationId = JsonNode.Parse(createJson)?["id"]?.GetValue<string>()
            ?? throw new ArgumentException("Instagram did not return a media container id.");

        var publishUrl = $"/{ApiVersion}/{igUserId}/media_publish?creation_id={Uri.EscapeDataString(creationId)}&access_token={Uri.EscapeDataString(pageAccessToken)}";
        var publishResponse = await client.PostAsync(publishUrl, null);
        var publishJson = await publishResponse.Content.ReadAsStringAsync();
        EnsureSuccess(publishResponse, publishJson, "Failed to publish to your Instagram account.");
        var mediaId = JsonNode.Parse(publishJson)?["id"]?.GetValue<string>()
            ?? throw new ArgumentException("Instagram did not return a post id.");

        // Best-effort — the post already went through even if this lookup fails, so fall
        // back to the profile URL rather than surfacing an error for a successful publish.
        try
        {
            var permalinkResponse = await client.GetAsync(
                $"/{ApiVersion}/{mediaId}?fields=permalink&access_token={Uri.EscapeDataString(pageAccessToken)}");
            if (permalinkResponse.IsSuccessStatusCode)
            {
                var permalinkJson = await permalinkResponse.Content.ReadAsStringAsync();
                var permalink = JsonNode.Parse(permalinkJson)?["permalink"]?.GetValue<string>();
                if (permalink is not null) return permalink;
            }
        }
        catch (JsonException) { /* fall through to profile link */ }

        return username is not null ? $"https://www.instagram.com/{username}/" : "https://www.instagram.com/";
    }

    public async Task SendMessengerMessageAsync(string pageAccessToken, string recipientPsid, string text)
    {
        var client = CreateClient();
        var body = JsonSerializer.Serialize(new
        {
            recipient = new { id = recipientPsid },
            message = new { text }
        });
        var response = await client.PostAsync(
            $"/{ApiVersion}/me/messages?access_token={Uri.EscapeDataString(pageAccessToken)}",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        var json = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, json, "Failed to send a Messenger reply.");
    }

    // Subscribes the app to receive this Page's "messages" webhook field. Meta requires
    // this per-Page even after the app-level webhook product is configured.
    public async Task SubscribePageToMessagingAsync(string pageId, string pageAccessToken)
    {
        var client = CreateClient();
        var response = await client.PostAsync(
            $"/{ApiVersion}/{pageId}/subscribed_apps?subscribed_fields=messages&access_token={Uri.EscapeDataString(pageAccessToken)}",
            null);
        var json = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, json, "Failed to subscribe your Facebook Page to Messenger.");
    }

    // Photo posts carry their single image directly; albums nest every photo (and
    // occasionally a video) under subattachments — all of them get collected, in
    // order, so a multi-photo post imports every image instead of just the first.
    // Videos only expose a thumbnail here — the caller resolves the actual playable
    // source via GetVideoSourceAsync using the returned node id.
    private static (List<string> ImageUrls, bool HasVideo, string? VideoNodeId) ExtractMedia(JsonNode? attachments)
    {
        if (attachments?["data"] is not JsonArray data || data.Count == 0) return ([], false, null);

        var first = data[0];
        var type = first?["type"]?.GetValue<string>();

        if (type == "album" && first?["subattachments"]?["data"] is JsonArray subData && subData.Count > 0)
        {
            var imageUrls = new List<string>();
            var hasVideo = false;
            string? videoNodeId = null;

            foreach (var sub in subData)
            {
                var subType = sub?["type"]?.GetValue<string>();
                var subImageUrl = sub?["media"]?["image"]?["src"]?.GetValue<string>();
                if (subImageUrl is not null) imageUrls.Add(subImageUrl);

                if (!hasVideo && subType is "video_inline" or "video_autoplay" or "video")
                {
                    hasVideo = true;
                    videoNodeId = sub?["target"]?["id"]?.GetValue<string>();
                }
            }

            return (imageUrls, hasVideo, videoNodeId);
        }

        var imageUrl = first?["media"]?["image"]?["src"]?.GetValue<string>();
        var isVideo = type is "video_inline" or "video_autoplay" or "video";
        var videoNodeIdSingle = isVideo ? first?["target"]?["id"]?.GetValue<string>() : null;

        return (imageUrl is not null ? [imageUrl] : [], isVideo, videoNodeIdSingle);
    }

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("https://graph.facebook.com");
        return client;
    }

    private static void EnsureSuccess(HttpResponseMessage response, string json, string fallbackMessage)
    {
        if (response.IsSuccessStatusCode) return;

        JsonNode? error = null;
        try { error = JsonNode.Parse(json)?["error"]; } catch (JsonException) { /* fall through to fallback message */ }

        var code = error?["code"]?.GetValue<int>();
        var message = error?["message"]?.GetValue<string>() ?? fallbackMessage;

        if (code == 190)
            throw new ConflictException("Your Facebook connection has expired. Please reconnect your Page.");

        throw new ArgumentException(message);
    }
}
