using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NipNip.Modules.Storefronts.Flitt;

public class FlittApiClient(IHttpClientFactory httpClientFactory)
{
    private const string BaseUrl = "https://pay.flitt.com";

    public async Task<string> CreateCheckoutUrlAsync(Dictionary<string, object?> requestFields)
    {
        var client = httpClientFactory.CreateClient();
        var body = JsonSerializer.Serialize(new { request = requestFields });

        var response = await client.PostAsync(
            $"{BaseUrl}/api/checkout/url",
            new StringContent(body, Encoding.UTF8, "application/json"));

        var json = await response.Content.ReadAsStringAsync();
        var node = JsonNode.Parse(json)?["response"];
        var status = node?["response_status"]?.GetValue<string>();

        if (status != "success")
        {
            var message = node?["error_message"]?.GetValue<string>();
            throw new ArgumentException(message ?? "Could not start the Flitt checkout session.");
        }

        return node!["checkout_url"]!.GetValue<string>();
    }
}
