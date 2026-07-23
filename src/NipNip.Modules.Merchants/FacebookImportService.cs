using System.Net;
using System.Text.RegularExpressions;
using NipNip.Modules.Merchants.DTOs;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Merchants;

// Pulls a quick starting point (name/description/photo) for a prospect's demo store from a
// Facebook Page — either a URL we fetch ourselves, or HTML the admin already has open in their
// own logged-in browser and pastes in (Facebook's actual content is heavily JS-rendered and
// often blocks server-side fetches, so pasted HTML is the reliable fallback). Either way, this
// only ever reads the same Open Graph meta tags Facebook already serves for public link
// previews — nothing gated behind a login, no scraping of private/JS-hydrated content.
public class FacebookImportService(IHttpClientFactory httpClientFactory)
{
    private const long MaxImageBytes = 8 * 1024 * 1024;

    private static readonly Regex OgTagRegex = new(
        """<meta[^>]+?(?:property=["']og:(?<prop1>[a-z:]+)["'][^>]*?content=["'](?<content1>[^"']*)["']|content=["'](?<content2>[^"']*)["'][^>]*?property=["']og:(?<prop2>[a-z:]+)["'])[^>]*?>""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<ImportFacebookResponse> ImportAsync(ImportFacebookRequest request)
    {
        string html;
        if (!string.IsNullOrWhiteSpace(request.Html))
        {
            html = request.Html;
        }
        else if (!string.IsNullOrWhiteSpace(request.Url))
        {
            html = await FetchHtmlAsync(request.Url);
        }
        else
        {
            throw new ArgumentException("Provide either a Facebook page URL or its pasted HTML.");
        }

        var tags = ExtractOgTags(html);
        tags.TryGetValue("title", out var name);
        tags.TryGetValue("description", out var description);
        tags.TryGetValue("image", out var imageUrl);

        string? imageDataUri = string.IsNullOrWhiteSpace(imageUrl) ? null : await TryDownloadAsDataUriAsync(imageUrl);

        if (name is null && description is null && imageDataUri is null)
            throw new ArgumentException("Couldn't find any Facebook page info there. Try pasting the page's HTML instead — open the link, right-click → View Page Source, copy all, and paste it in.");

        return new ImportFacebookResponse(name, description, imageDataUri);
    }

    private async Task<string> FetchHtmlAsync(string url)
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36");

        try
        {
            return await client.GetStringAsync(url);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new ArgumentException(
                "Couldn't fetch that URL — Facebook often blocks automated requests. Open the link yourself, right-click → View Page Source, copy all, and paste it in instead.");
        }
    }

    private static Dictionary<string, string> ExtractOgTags(string html)
    {
        var result = new Dictionary<string, string>();
        foreach (Match m in OgTagRegex.Matches(html))
        {
            var prop = m.Groups["prop1"].Success ? m.Groups["prop1"].Value : m.Groups["prop2"].Value;
            var content = m.Groups["content1"].Success ? m.Groups["content1"].Value : m.Groups["content2"].Value;
            if (!result.ContainsKey(prop))
                result[prop] = WebUtility.HtmlDecode(content);
        }
        return result;
    }

    // Downloaded (rather than linked directly) so the prospect's demo doesn't depend on
    // Facebook's CDN URL, which is often short-lived/session-scoped — the admin still re-uploads
    // it through the normal image picker on the frontend, this just saves them a download/re-upload.
    private async Task<string?> TryDownloadAsDataUriAsync(string imageUrl)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

            var response = await client.GetAsync(imageUrl);
            if (!response.IsSuccessStatusCode) return null;

            var bytes = await response.Content.ReadAsByteArrayAsync();
            if (bytes.Length == 0 || bytes.Length > MaxImageBytes) return null;

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
        }
        catch
        {
            return null;
        }
    }
}
