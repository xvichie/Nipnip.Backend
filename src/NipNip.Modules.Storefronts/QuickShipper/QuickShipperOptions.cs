namespace NipNip.Modules.Storefronts.QuickShipper;

public class QuickShipperOptions
{
    public string AuthBaseUrl { get; set; } = "";
    public string ApiBaseUrl { get; set; } = "";

    /// <summary>
    /// Issued by QuickShipper support once per integrator (not per merchant) — used as the
    /// Basic-auth credentials when exchanging a merchant's own QuickShipper username/password
    /// for an access token via the OAuth2 password grant.
    /// </summary>
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
}
