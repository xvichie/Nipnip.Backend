using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts.QuickShipper;

public record QuickShipperToken(string AccessToken, int ExpiresInSeconds);

// Resource Owner Password Credentials (grant_type=password) against QuickShipper's own
// IdentityServer-style token endpoint — the merchant's QuickShipper username/password stand
// in for a browser OAuth redirect, so connecting is just a two-field form, not a popup flow.
public class QuickShipperAuthClient(IHttpClientFactory httpClientFactory, IOptions<QuickShipperOptions> options)
{
    private readonly QuickShipperOptions _options = options.Value;

    public async Task<QuickShipperToken> RequestTokenAsync(string username, string password)
    {
        var client = httpClientFactory.CreateClient();
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.AuthBaseUrl.TrimEnd('/')}/connect/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["scope"] = "DeliveryApi",
                ["username"] = username,
                ["password"] = password,
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

        var response = await client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            JsonNode? node = null;
            try { node = JsonNode.Parse(json); } catch { /* fall through to fallback message */ }
            var description = node?["error_description"]?.GetValue<string>() ?? node?["error"]?.GetValue<string>();
            throw new ArgumentException(description ?? "Could not sign in to QuickShipper with those credentials.");
        }

        var body = JsonNode.Parse(json)!;
        var accessToken = body["access_token"]?.GetValue<string>()
            ?? throw new ArgumentException("QuickShipper did not return an access token.");
        var expiresIn = body["expires_in"]?.GetValue<int>() ?? 3600;

        return new QuickShipperToken(accessToken, expiresIn);
    }
}
