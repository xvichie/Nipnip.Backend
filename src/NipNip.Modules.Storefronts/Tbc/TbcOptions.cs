namespace NipNip.Modules.Storefronts.Tbc;

public class TbcOptions
{
    /// <summary>
    /// TBC's app-wide "Developer app API key" — identifies NipNip itself as the API consumer,
    /// sent as the `apikey` header on every request. Distinct from the per-merchant
    /// client_id/client_secret pair stored on each Store.
    /// </summary>
    public string ApiKey { get; set; } = "";

    /// <summary>
    /// Base URL of the storefront frontend — used to build each order's returnurl (where TBC
    /// redirects the customer's browser back to after paying).
    /// </summary>
    public string FrontendUrl { get; set; } = "http://localhost:3000";

    /// <summary>
    /// Base URL of this API — used to build each order's callbackUrl. A static config value
    /// (like FlittOptions.ApiBaseUrl) rather than derived from the live HttpContext, since
    /// checkout can also be triggered from the AI messaging agent's background tool-calling
    /// path, where there may be no active request to read from.
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://localhost:7073";
}
