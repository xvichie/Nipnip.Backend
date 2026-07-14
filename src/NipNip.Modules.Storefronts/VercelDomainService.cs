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

        // The add-domain response's own "verified" flag only means Vercel doesn't
        // require an ownership challenge (a TXT record) — it says nothing about
        // whether DNS actually points here. A brand-new, uncontested domain gets
        // verified:true immediately, before any DNS has been touched. Only an
        // ownership challenge (rare — the domain is already claimed elsewhere on
        // Vercel) is worth reading from this response; real readiness always comes
        // from the DNS config check below.
        var challenges = ExtractOwnershipChallenges(json);
        var configStatus = await CheckDnsConfigAsync(domain);
        return new VercelDomainStatus(configStatus, challenges ?? DefaultInstructions(domain));
    }

    public async Task<VercelDomainStatus> GetStatusAsync(string domain)
    {
        var verified = await CheckDnsConfigAsync(domain);
        return new VercelDomainStatus(verified, DefaultInstructions(domain));
    }

    private async Task<bool> CheckDnsConfigAsync(string domain)
    {
        var client = CreateClient();
        var response = await client.GetAsync($"/v6/domains/{domain}/config{TeamQuery("?")}");
        if (!response.IsSuccessStatusCode) return false;

        try
        {
            var node = JsonNode.Parse(await response.Content.ReadAsStringAsync());
            // misconfigured:false means DNS resolves to Vercel and the domain is actually live.
            var misconfigured = node?["misconfigured"]?.GetValue<bool>() ?? true;
            return !misconfigured;
        }
        catch (JsonException)
        {
            return false;
        }
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

    private static List<DomainDnsInstruction>? ExtractOwnershipChallenges(string json)
    {
        try
        {
            var node = JsonNode.Parse(json);
            if (node?["verification"] is not JsonArray array) return null;

            var challenges = new List<DomainDnsInstruction>();
            foreach (var c in array)
            {
                var type = c?["type"]?.GetValue<string>();
                var name = c?["domain"]?.GetValue<string>();
                var value = c?["value"]?.GetValue<string>();
                if (type is not null && name is not null && value is not null)
                    challenges.Add(new DomainDnsInstruction(type, name, value));
            }
            return challenges.Count > 0 ? challenges : null;
        }
        catch (JsonException)
        {
            return null;
        }
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
