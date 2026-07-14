using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Extensions;
using NipNip.Shared.Pagination;

namespace NipNip.Modules.Storefronts;

[ApiController]
[Route("api/stores/me/orders")]
[Authorize]
public class OrderController(OrderService orderService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<OrderDetailResponse>>> GetAll(
        [FromQuery] PaginatedRequest request,
        [FromQuery] string? status,
        [FromQuery] int? month,
        [FromQuery] int? year)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await orderService.GetAllForOwnStoreAsync(clerkUserId, request, status, month, year));
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<OrderDetailResponse>> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await orderService.UpdateStatusAsync(clerkUserId, id, request));
    }

    [HttpPut("{id:guid}/payment-confirmed")]
    public async Task<ActionResult<OrderDetailResponse>> UpdatePaymentConfirmed(Guid id, [FromBody] UpdatePaymentConfirmedRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await orderService.UpdatePaymentConfirmedAsync(clerkUserId, id, request));
    }

    [HttpPost("{id:guid}/notes")]
    public async Task<ActionResult<OrderDetailResponse>> AddNote(Guid id, [FromBody] CreateOrderNoteRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await orderService.AddNoteAsync(clerkUserId, id, request));
    }

    [HttpGet("new-count")]
    public async Task<ActionResult<NewOrderCountResponse>> GetNewCount()
    {
        var clerkUserId = User.GetClerkUserId();
        var count = await orderService.GetNewOrderCountForOwnStoreAsync(clerkUserId);
        return Ok(new NewOrderCountResponse(count));
    }

    [HttpGet("monthly")]
    public async Task<ActionResult<List<MonthlyOrderSummary>>> GetMonthlySummary([FromQuery] string? status)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await orderService.GetMonthlySummaryForOwnStoreAsync(clerkUserId, status));
    }
}
