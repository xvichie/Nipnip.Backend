using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NipNip.Modules.Storefronts.Bog;

public record BogToken(string AccessToken, long ExpiresInSeconds);

public record BogCreatePreOrderResult(string PreOrderId, string PaymentLink);

public record BogPreOrderStatus(string StatusKey);

public class BogApiClient(IHttpClientFactory httpClientFactory)
{
    private const string AuthUrl = "https://oauth2.bog.ge/auth/realms/bog/protocol/openid-connect/token";
    private const string ApiBaseUrl = "https://api.bog.ge/payments";

    public async Task<BogToken> RequestTokenAsync(string clientId, string clientSecret)
    {
        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, AuthUrl)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
            }),
        };
        var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

        var response = await client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        var node = JsonNode.Parse(json);

        if (!response.IsSuccessStatusCode)
        {
            var message = node?["error_description"]?.GetValue<string>() ?? node?["error"]?.GetValue<string>();
            throw new ArgumentException(message ?? "Could not authenticate with Bank of Georgia. Check the client ID and secret.");
        }

        var accessToken = node?["access_token"]?.GetValue<string>();
        if (string.IsNullOrEmpty(accessToken))
            throw new ArgumentException("Bank of Georgia did not return an access token.");

        // The docs' own example response shows an implausibly large expires_in (looks like a
        // stray millisecond timestamp rather than a duration) — fall back defensively, matching
        // QuickShipperAuthClient/TbcApiClient, rather than trusting an unbounded value blindly.
        var expiresIn = node?["expires_in"]?.GetValue<long?>();
        if (expiresIn is null or <= 0 or > 86400) expiresIn = 3600;

        return new BogToken(accessToken, expiresIn.Value);
    }

    public async Task<BogCreatePreOrderResult> CreatePreOrderAsync(string accessToken, object requestBody)
    {
        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBaseUrl}/v1/pre-orders")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        var node = JsonNode.Parse(json);

        if (!response.IsSuccessStatusCode)
        {
            var message = node?["message"]?.GetValue<string>() ?? node?["error"]?.GetValue<string>();
            throw new ArgumentException(message ?? "Could not start the Bank of Georgia checkout session.");
        }

        var preOrderId = node?["id"]?.GetValue<string>();
        var paymentLink = node?["_links"]?["payment_link"]?["href"]?.GetValue<string>();
        if (string.IsNullOrEmpty(preOrderId) || string.IsNullOrEmpty(paymentLink))
            throw new ArgumentException("Bank of Georgia did not return a checkout link.");

        return new BogCreatePreOrderResult(preOrderId, paymentLink);
    }

    public async Task<BogPreOrderStatus> GetPreOrderStatusAsync(string accessToken, string preOrderId)
    {
        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}/v1/pre-orders/{preOrderId}/detail");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        var node = JsonNode.Parse(json);

        var statusKey = node?["pre_order_status"]?["key"]?.GetValue<string>();
        if (!response.IsSuccessStatusCode || string.IsNullOrEmpty(statusKey))
        {
            var message = node?["message"]?.GetValue<string>() ?? node?["error"]?.GetValue<string>();
            throw new ArgumentException(message ?? "Could not fetch Bank of Georgia pre-order status.");
        }

        return new BogPreOrderStatus(statusKey);
    }
}
