namespace NipNip.Modules.Storefronts.Bog;

public class BogOptions
{
    /// <summary>
    /// Base URL of this API — used to build each order's callback_url. A static config value
    /// (like FlittOptions/TbcOptions.ApiBaseUrl) rather than derived from the live HttpContext,
    /// since checkout can also be triggered from the AI messaging agent's background
    /// tool-calling path, where there may be no active request to read from.
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://localhost:7073";
}
