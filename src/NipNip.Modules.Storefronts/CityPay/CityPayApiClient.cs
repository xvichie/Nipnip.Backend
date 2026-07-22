using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace NipNip.Modules.Storefronts.CityPay;

public record CityPayCreateOrderResult(long? OrderId, string PaymentUrl, string Token);

public record CityPayOrderStatus(string StatusCode);

public class CityPayApiClient(IHttpClientFactory httpClientFactory, IOptions<CityPayOptions> options)
{
    private readonly CityPayOptions _options = options.Value;

    // CityPay's own docs show two different response envelopes for this same endpoint across
    // different doc pages (one wraps the payload in "message", the other in "data") — checking
    // both keeps this working regardless of which shape the live API actually returns.
    public async Task<CityPayCreateOrderResult> CreateOrderAsync(
        string customerId, string accessToken, string orderId, string orderToken, decimal amount)
    {
        var client = httpClientFactory.CreateClient();
        var body = new
        {
            customer_id = customerId,
            access_token = accessToken,
            order_id = orderId,
            order_token = orderToken,
            amount,
        };

        var response = await client.PostAsync(
            $"{_options.ListenerBaseUrl.TrimEnd('/')}/api/generateOrder",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

        var json = await response.Content.ReadAsStringAsync();
        var node = JsonNode.Parse(json);
        var payload = node?["message"] as JsonObject ?? node?["data"] as JsonObject;

        var paymentUrl = payload?["payment_url"]?.GetValue<string>();
        var token = payload?["token"]?.GetValue<string>();

        if (!response.IsSuccessStatusCode || string.IsNullOrEmpty(paymentUrl) || string.IsNullOrEmpty(token))
        {
            var errorMessage = node?["message"]?.ToString();
            throw new ArgumentException(
                string.IsNullOrEmpty(errorMessage) || errorMessage == "null"
                    ? "Could not start the CityPay checkout session."
                    : errorMessage);
        }

        var orderIdValue = payload?["id"]?.GetValue<long?>();
        return new CityPayCreateOrderResult(orderIdValue, paymentUrl, token);
    }

    // No auth required — this endpoint is keyed only by the (unguessable) order token, per
    // CityPay's docs. Used as the trustworthy "check now" pull after a callback arrives, since
    // the callback itself carries no signature we could otherwise verify.
    public async Task<CityPayOrderStatus> GetOrderStatusAsync(string orderToken)
    {
        var client = httpClientFactory.CreateClient();
        var response = await client.GetAsync($"{_options.OrderApiBaseUrl.TrimEnd('/')}/order/{orderToken}");

        var json = await response.Content.ReadAsStringAsync();
        var node = JsonNode.Parse(json);
        var statusCode = node?["data"]?["status"]?["code"]?.GetValue<string>();

        if (!response.IsSuccessStatusCode || string.IsNullOrEmpty(statusCode))
            throw new ArgumentException("Could not fetch the CityPay order status.");

        return new CityPayOrderStatus(statusCode);
    }
}
