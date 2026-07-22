namespace NipNip.Modules.Storefronts.CityPay;

public class CityPayOptions
{
    /// <summary>
    /// Base URL for order-creation calls. CityPay's sandbox uses a "test-" hostname prefix
    /// instead of a separate path, so switching environments means overriding this whole value
    /// (e.g. to https://test-v2-listener.citypay.io) rather than toggling a flag.
    /// </summary>
    public string ListenerBaseUrl { get; set; } = "https://v2-listener.citypay.io";

    /// <summary>
    /// Base URL for the no-auth "get order by token" endpoint used to pull trustworthy status
    /// after a callback arrives. Same sandbox-prefix caveat as ListenerBaseUrl.
    /// </summary>
    public string OrderApiBaseUrl { get; set; } = "https://v2-order-api.citypay.io";
}
