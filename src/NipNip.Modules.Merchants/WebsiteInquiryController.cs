using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Merchants.DTOs;
using NipNip.Shared.Extensions;

namespace NipNip.Modules.Merchants;

// "I want a website" lead capture. Two submitters: the marketing site's anonymous footer form
// (no auth, no store slug — unlike ContactMessage this isn't addressed to any particular
// merchant), and a freshly signed-up Clerk user going through /onboarding in place of the old
// self-serve creator registration form — that call carries their bearer token, so Create
// threads the resulting ClerkUserId through even though the endpoint itself stays anonymous.
// Admin-side reading/marking-as-read lives in AdminController alongside the rest of the admin
// surface.
[ApiController]
[Route("api/website-inquiries")]
[Authorize]
public class WebsiteInquiryController(WebsiteInquiryService websiteInquiryService) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [EnableCors("Public")]
    public async Task<ActionResult<WebsiteInquiryResponse>> Create([FromBody] CreateWebsiteInquiryRequest request)
    {
        var clerkUserId = User.TryGetClerkUserId();
        return Ok(await websiteInquiryService.CreateAsync(request, clerkUserId));
    }

    // Lets the onboarding-replacement page skip straight to the "thanks, we'll be in touch"
    // screen on a later visit instead of showing the form again.
    [HttpGet("me")]
    public async Task<ActionResult<WebsiteInquiryResponse?>> GetOwn()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await websiteInquiryService.GetOwnAsync(clerkUserId));
    }
}
