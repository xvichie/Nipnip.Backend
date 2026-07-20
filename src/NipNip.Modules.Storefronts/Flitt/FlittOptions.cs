namespace NipNip.Modules.Storefronts.Flitt;

public class FlittOptions
{
    /// <summary>
    /// Base URL of the storefront frontend — used to build each order's response_url
    /// (where Flitt redirects the customer's browser back to after paying).
    /// </summary>
    public string FrontendUrl { get; set; } = "http://localhost:3000";

    /// <summary>
    /// Base URL of this API — used to build each order's server_callback_url. A static
    /// config value (like FacebookOptions.RedirectUri) rather than derived from the live
    /// HttpContext, since checkout can also be triggered from the AI messaging agent's
    /// background tool-calling path, where there may be no active request to read from.
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://localhost:7073";
}
