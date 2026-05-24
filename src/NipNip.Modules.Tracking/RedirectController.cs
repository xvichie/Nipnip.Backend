using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace NipNip.Modules.Tracking;

[ApiController]
[Route("r")]
[AllowAnonymous]
public class RedirectController(TrackingService trackingService, IServiceScopeFactory scopeFactory) : ControllerBase
{
    [HttpGet("{creatorSlug}/{merchantSlug}")]
    [EnableCors("Public")]
    public async Task<IActionResult> TrackAndRedirect(string creatorSlug, string merchantSlug)
    {
        var info = await trackingService.PrepareRedirectAsync(creatorSlug, merchantSlug);

        Response.Cookies.Append("_nn_ref", info.RefCode, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(30),
            SameSite = SameSiteMode.None,
            Secure = true,
            HttpOnly = true,
        });

        var creatorId = info.CreatorId;
        var merchantId = info.MerchantId;
        var refCode = info.RefCode;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var svc = scope.ServiceProvider.GetRequiredService<TrackingService>();
                await svc.LogClickAsync(creatorId, merchantId, refCode, ipAddress, userAgent);
            }
            catch
            {
                // Click logging is best-effort; a failed write must never break the redirect.
            }
        });

        return Redirect(info.RedirectUrl);
    }
}
