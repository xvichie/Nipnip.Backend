using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Extensions;

namespace NipNip.Modules.Storefronts.QuickShipper;

[ApiController]
[Route("api/stores/me/quickshipper")]
[Authorize]
public class QuickShipperController(QuickShipperService quickShipper) : ControllerBase
{
    [HttpPost("connect")]
    public async Task<ActionResult<QuickShipperStatusResponse>> Connect([FromBody] ConnectQuickShipperRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await quickShipper.ConnectAsync(clerkUserId, request));
    }

    [HttpDelete]
    public async Task<IActionResult> Disconnect()
    {
        var clerkUserId = User.GetClerkUserId();
        await quickShipper.DisconnectAsync(clerkUserId);
        return NoContent();
    }

    [HttpGet("status")]
    public async Task<ActionResult<QuickShipperStatusResponse>> GetStatus()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await quickShipper.GetStatusAsync(clerkUserId));
    }

    [HttpGet("pickup-location")]
    public async Task<ActionResult<PickupLocationResponse>> GetPickupLocation()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await quickShipper.GetPickupLocationAsync(clerkUserId));
    }

    [HttpPut("pickup-location")]
    public async Task<ActionResult<PickupLocationResponse>> SavePickupLocation([FromBody] SavePickupLocationRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await quickShipper.SavePickupLocationAsync(clerkUserId, request));
    }

    [HttpGet("custom-fields")]
    public async Task<ActionResult<List<QuickShipperCustomFieldResponse>>> GetCustomFields()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await quickShipper.GetCustomFieldsAsync(clerkUserId));
    }

    [HttpGet("orders/{orderId:guid}/fees")]
    public async Task<ActionResult<QuickShipperFeesResponse>> GetFees(Guid orderId)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await quickShipper.GetFeesForOrderAsync(clerkUserId, orderId));
    }

    [HttpPost("orders/{orderId:guid}")]
    public async Task<ActionResult<OrderDetailResponse>> CreateDeliveryOrder(Guid orderId, [FromBody] CreateQuickShipperOrderRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await quickShipper.CreateDeliveryOrderAsync(clerkUserId, orderId, request));
    }

    [HttpPost("orders/{orderId:guid}/refresh")]
    public async Task<ActionResult<OrderDetailResponse>> RefreshOrderStatus(Guid orderId)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await quickShipper.RefreshOrderStatusAsync(clerkUserId, orderId));
    }
}
