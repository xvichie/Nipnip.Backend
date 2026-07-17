namespace NipNip.Modules.Storefronts;

public class FacebookOptions
{
    public string AppId { get; set; } = "";
    public string AppSecret { get; set; } = "";
    public string RedirectUri { get; set; } = "";
    public string TokenEncryptionKey { get; set; } = "";

    /// <summary>
    /// Base URL of the merchant dashboard frontend — where FacebookConnectionService
    /// redirects the browser back to once the OAuth callback finishes. Was previously
    /// (wrongly) borrowed from Email:AppUrl; kept separate since the two have no reason
    /// to be coupled and each environment may need to set them independently.
    /// </summary>
    public string FrontendUrl { get; set; } = "http://localhost:3000";

    /// <summary>
    /// Login Connection ("Configuration") ID from Facebook Login for Business.
    /// That product requires permissions to be pre-declared in a saved configuration
    /// rather than passed as a free-form `scope` list — set this and the OAuth dialog
    /// is built with `config_id` instead. Leave blank only if using classic Facebook
    /// Login, which still accepts a raw `scope` parameter.
    /// </summary>
    public string? ConfigId { get; set; }

    /// <summary>
    /// Shared secret configured once on the Meta App's Messenger webhook product
    /// (not per-Page) — checked against `hub.verify_token` during the GET verification
    /// handshake.
    /// </summary>
    public string MessengerVerifyToken { get; set; } = "";
}
