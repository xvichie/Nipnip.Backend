using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace NipNip.Modules.Storefronts;

public record DomainDnsInstruction(string Type, string Name, string Value);

public record VercelDomainStatus(bool Verified, List<DomainDnsInstruction> Instructions);

public class VercelDomainService(IHttpClientFactory httpClientFactory, IOptions<VercelOptions> options)
{
    private readonly VercelOptions _options = options.Value;

    public async Task<VercelDomainStatus> AddDomainAsync(string domain)
    {
        var client = CreateClient();
        var body = new StringContent(JsonSerializer.Serialize(new { name = domain }), Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"/v10/projects/{_options.ProjectId}/domains{TeamQuery("?")}", body);

        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new ArgumentException(ExtractErrorMessage(json) ?? "Failed to connect the domain. Please try again.");

        return ParseStatus(domain, json);
    }

    public async Task<VercelDomainStatus> GetStatusAsync(string domain)
    {
        var client = CreateClient();
        var response = await client.GetAsync($"/v9/projects/{_options.ProjectId}/domains/{domain}{TeamQuery("?")}");

        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return new VercelDomainStatus(false, DefaultInstructions(domain));

        return ParseStatus(domain, json);
    }

    public async Task RemoveDomainAsync(string domain)
    {
        var client = CreateClient();
        var response = await client.DeleteAsync($"/v9/projects/{_options.ProjectId}/domains/{domain}{TeamQuery("?")}");

        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            throw new ArgumentException(ExtractErrorMessage(await response.Content.ReadAsStringAsync()) ?? "Failed to disconnect the domain.");
    }

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("https://api.vercel.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);
        return client;
    }

    private string TeamQuery(string separator) =>
        string.IsNullOrWhiteSpace(_options.TeamId) ? "" : $"{separator}teamId={_options.TeamId}";

    private static VercelDomainStatus ParseStatus(string domain, string json)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return new VercelDomainStatus(false, DefaultInstructions(domain));
        }

        var verified = node?["verified"]?.GetValue<bool>() ?? false;

        // Vercel only returns explicit ownership-verification challenges when the
        // domain is already claimed elsewhere on Vercel — the common case (a fresh
        // domain that just needs DNS pointed here) has no challenges, so fall back
        // to the standard apex/subdomain instructions.
        var challenges = new List<DomainDnsInstruction>();
        if (node?["verification"] is JsonArray array)
        {
            foreach (var c in array)
            {
                var type = c?["type"]?.GetValue<string>();
                var name = c?["domain"]?.GetValue<string>();
                var value = c?["value"]?.GetValue<string>();
                if (type is not null && name is not null && value is not null)
                    challenges.Add(new DomainDnsInstruction(type, name, value));
            }
        }

        return new VercelDomainStatus(verified, challenges.Count > 0 ? challenges : DefaultInstructions(domain));
    }

    private static List<DomainDnsInstruction> DefaultInstructions(string domain)
    {
        var isApex = domain.Count(c => c == '.') == 1;
        return isApex
            ? [new DomainDnsInstruction("A", "@", "76.76.21.21")]
            : [new DomainDnsInstruction("CNAME", domain[..domain.IndexOf('.')], "cname.vercel-dns.com")];
    }

    private static string? ExtractErrorMessage(string json)
    {
        try
        {
            return JsonNode.Parse(json)?["error"]?["message"]?.GetValue<string>();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
