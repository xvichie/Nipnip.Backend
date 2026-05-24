using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Payouts.DTOs;
using NipNip.Shared.Extensions;
using NipNip.Shared.Pagination;

namespace NipNip.Modules.Payouts;

[ApiController]
[Route("api/payouts")]
[Authorize]
public class PayoutController(PayoutService payoutService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PayoutResponse>> RequestPayout([FromBody] RequestPayoutRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await payoutService.RequestPayoutAsync(clerkUserId, request));
    }

    [HttpGet("me")]
    public async Task<ActionResult<PaginatedResult<PayoutResponse>>> GetMyPayouts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await payoutService.GetMyPayoutsAsync(clerkUserId, page, pageSize));
    }

    [HttpGet("balance")]
    public async Task<ActionResult<AvailableBalanceResponse>> GetAvailableBalance()
    {
        var clerkUserId = User.GetClerkUserId();
        var balance = await payoutService.GetAvailableBalanceAsync(clerkUserId);
        return Ok(new AvailableBalanceResponse(balance));
    }
}

public record AvailableBalanceResponse(decimal AvailableBalance);
