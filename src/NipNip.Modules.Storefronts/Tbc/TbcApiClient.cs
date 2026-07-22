using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NipNip.Modules.Storefronts.Tbc;

public record TbcToken(string AccessToken, int ExpiresInSeconds);

public record TbcCreatePaymentResult(string PayId, string ApprovalUrl);

public record TbcPaymentStatus(string Status);

public class TbcApiClient(IHttpClientFactory httpClientFactory)
{
    private const string BaseUrl = "https://api.tbcbank.ge";

    public async Task<TbcToken> RequestTokenAsync(string apiKey, string clientId, string clientSecret)
    {
        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v1/tpay/access-token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
            }),
        };
        request.Headers.Add("apikey", apiKey);

        var response = await client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        var node = JsonNode.Parse(json);

        if (!response.IsSuccessStatusCode)
        {
            var message = node?["userMessage"]?.GetValue<string>() ?? node?["detail"]?.GetValue<string>();
            throw new ArgumentException(message ?? "Could not authenticate with TBC. Check the client ID and secret.");
        }

        var accessToken = node?["access_token"]?.GetValue<string>();
        if (string.IsNullOrEmpty(accessToken))
            throw new ArgumentException("TBC did not return an access token.");

        // Defensive default, matching QuickShipperAuthClient — the docs don't show the exact
        // success response shape for this endpoint beyond it being a Bearer token.
        var expiresIn = node?["expires_in"]?.GetValue<int?>() ?? 3600;

        return new TbcToken(accessToken, expiresIn);
    }

    public async Task<TbcCreatePaymentResult> CreatePaymentAsync(string apiKey, string accessToken, object requestBody)
    {
        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v1/tpay/payments")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("apikey", apiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        var node = JsonNode.Parse(json);

        if (!response.IsSuccessStatusCode)
        {
            var message = node?["userMessage"]?.GetValue<string>() ?? node?["detail"]?.GetValue<string>();
            throw new ArgumentException(message ?? "Could not start the TBC checkout session.");
        }

        var payId = node?["payId"]?.GetValue<string>();
        if (string.IsNullOrEmpty(payId))
            throw new ArgumentException("TBC did not return a payment id.");

        var approvalUrl = node?["links"]?.AsArray()
            .FirstOrDefault(link => link?["rel"]?.GetValue<string>() == "approval_url")?["uri"]?.GetValue<string>();
        if (string.IsNullOrEmpty(approvalUrl))
            throw new ArgumentException("TBC did not return a checkout redirect URL.");

        return new TbcCreatePaymentResult(payId, approvalUrl);
    }

    public async Task<TbcPaymentStatus> GetPaymentStatusAsync(string apiKey, string accessToken, string payId)
    {
        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/v1/tpay/payments/{payId}");
        request.Headers.Add("apikey", apiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        var node = JsonNode.Parse(json);

        var status = node?["status"]?.GetValue<string>();
        if (!response.IsSuccessStatusCode || string.IsNullOrEmpty(status))
        {
            var message = node?["userMessage"]?.GetValue<string>() ?? node?["detail"]?.GetValue<string>();
            throw new ArgumentException(message ?? "Could not fetch TBC payment status.");
        }

        return new TbcPaymentStatus(status);
    }
}
