using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Extensions;
using NipNip.Shared.Pagination;

namespace NipNip.Modules.Storefronts;

[ApiController]
[Route("api/stores/me/contact-messages")]
[Authorize]
public class ContactMessageController(ContactMessageService contactMessageService) : ControllerBase
{
    [HttpPost("/api/stores/{slug}/contact-messages")]
    [AllowAnonymous]
    [EnableCors("Public")]
    public async Task<ActionResult<ContactMessageResponse>> Create(string slug, [FromBody] CreateContactMessageRequest request)
    {
        return Ok(await contactMessageService.CreateAsync(slug, request));
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<ContactMessageResponse>>> GetAll([FromQuery] PaginatedRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await contactMessageService.GetAllForOwnStoreAsync(clerkUserId, request));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult<ContactMessageResponse>> MarkAsRead(Guid id)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await contactMessageService.MarkAsReadAsync(clerkUserId, id));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<UnreadContactMessageCountResponse>> GetUnreadCount()
    {
        var clerkUserId = User.GetClerkUserId();
        var count = await contactMessageService.GetUnreadCountForOwnStoreAsync(clerkUserId);
        return Ok(new UnreadContactMessageCountResponse(count));
    }
}
