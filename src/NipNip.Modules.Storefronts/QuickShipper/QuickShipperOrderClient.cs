using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts.QuickShipper;

public record QuickShipperFeesQuery(
    string FromStreetName, double FromLatitude, double FromLongitude,
    string? ToStreetName, double ToLatitude, double ToLongitude,
    decimal CartAmount);

// Raw wrapper around the v1/Order* endpoints — returns parsed JsonNode rather than strict
// C# models, since QuickShipperService only needs a handful of fields out of each fairly
// large response shape. Mirrors GraphApiClient's CreateClient/EnsureSuccess-style pattern.
public class QuickShipperOrderClient(IHttpClientFactory httpClientFactory, IOptions<QuickShipperOptions> options)
{
    private readonly QuickShipperOptions _options = options.Value;

    public async Task<JsonNode> GetFeesAsync(string accessToken, QuickShipperFeesQuery query)
    {
        var qs = "v1/Order/fees" +
            $"?FromStreetName={Uri.EscapeDataString(query.FromStreetName)}" +
            $"&FromLatitude={query.FromLatitude}&FromLongitude={query.FromLongitude}" +
            (string.IsNullOrWhiteSpace(query.ToStreetName) ? "" : $"&ToStreetName={Uri.EscapeDataString(query.ToStreetName)}") +
            $"&ToLatitude={query.ToLatitude}&ToLongitude={query.ToLongitude}" +
            $"&CartAmount={query.CartAmount}";

        var client = CreateClient(accessToken);
        var response = await client.GetAsync(qs);
        return await ParseAsync(response, "Could not calculate delivery fees.");
    }

    public async Task<JsonNode> GetCustomFieldsAsync(string accessToken)
    {
        var client = CreateClient(accessToken);
        var response = await client.GetAsync("v1/Order/custom-fields");
        return await ParseAsync(response, "Could not load QuickShipper's custom fields.");
    }

    public async Task<JsonNode> CreateOrderAsync(string accessToken, object requestBody)
    {
        var client = CreateClient(accessToken);
        var response = await client.PostAsync(
            "v1/Order",
            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
        return await ParseAsync(response, "Could not create the delivery order.");
    }

    public async Task<JsonNode> GetOrderInfoAsync(string accessToken, int quickShipperOrderId)
    {
        var client = CreateClient(accessToken);
        var response = await client.GetAsync($"v1/Order?OrderId={quickShipperOrderId}");
        return await ParseAsync(response, "Could not fetch the delivery order's status.");
    }

    private HttpClient CreateClient(string accessToken)
    {
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_options.ApiBaseUrl.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private static async Task<JsonNode> ParseAsync(HttpResponseMessage response, string fallbackMessage)
    {
        var json = await response.Content.ReadAsStringAsync();
        JsonNode? node = null;
        try { node = JsonNode.Parse(json); } catch { /* fall through to fallback message */ }

        var success = node?["success"]?.GetValue<bool>() ?? response.IsSuccessStatusCode;
        if (!response.IsSuccessStatusCode || !success)
        {
            var message = node?["userMessage"]?.GetValue<string>() ?? node?["developerMessage"]?.GetValue<string>();
            throw new ArgumentException(message ?? fallbackMessage);
        }

        return node ?? new JsonObject();
    }
}
