using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace NipNip.Modules.Storefronts.AiAgent;

// No official Voyage AI .NET SDK exists, so this is raw HTTP via IHttpClientFactory —
// same reasoning and shape as GraphApiClient.
public class VoyageClient(IHttpClientFactory httpClientFactory, IOptions<VoyageOptions> options)
{
    private const string Model = "voyage-4-lite";
    private readonly VoyageOptions _options = options.Value;

    // inputType is "document" for content being stored/indexed, "query" for the search
    // string at retrieval time — Voyage recommends distinguishing the two for quality.
    public async Task<List<float[]>> EmbedAsync(IReadOnlyList<string> texts, string inputType)
    {
        if (texts.Count == 0) return [];

        var client = CreateClient();
        var body = JsonSerializer.Serialize(new { input = texts, model = Model, input_type = inputType });
        var response = await client.PostAsync("/v1/embeddings", new StringContent(body, Encoding.UTF8, "application/json"));
        var json = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, json);

        var data = JsonNode.Parse(json)?["data"] as JsonArray
            ?? throw new ArgumentException("Voyage did not return embedding data.");

        var results = new float[data.Count][];
        foreach (var item in data)
        {
            var index = item?["index"]?.GetValue<int>()
                ?? throw new ArgumentException("Voyage embedding result missing index.");
            var vector = item?["embedding"]?.AsArray().Select(v => v!.GetValue<float>()).ToArray()
                ?? throw new ArgumentException("Voyage embedding result missing vector.");
            results[index] = vector;
        }

        return results.ToList();
    }

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("https://api.voyageai.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        return client;
    }

    private static void EnsureSuccess(HttpResponseMessage response, string json)
    {
        if (response.IsSuccessStatusCode) return;

        string? message = null;
        try { message = JsonNode.Parse(json)?["detail"]?.GetValue<string>(); } catch (JsonException) { /* fall through to fallback message */ }

        throw new ArgumentException(message ?? "Failed to generate embeddings.");
    }
}
