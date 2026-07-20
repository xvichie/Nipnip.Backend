namespace NipNip.Modules.Storefronts.TikTok;

public class TikTokOptions
{
    public string ClientKey { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string RedirectUri { get; set; } = "";

    /// <summary>
    /// Base URL of the merchant dashboard frontend — where TikTokConnectionService redirects
    /// the browser back to once the OAuth callback finishes (mirrors FacebookOptions.FrontendUrl).
    /// </summary>
    public string FrontendUrl { get; set; } = "http://localhost:3000";
}
