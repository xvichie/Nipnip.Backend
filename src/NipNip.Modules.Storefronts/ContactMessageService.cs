using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Data.Extensions;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Modules.Storefronts.Extensions;
using NipNip.Shared.Exceptions;
using NipNip.Shared.Pagination;
using System.Net.Mail;

namespace NipNip.Modules.Storefronts;

public class ContactMessageService(AppDbContext db, StoreService storeService)
{
    public async Task<ContactMessageResponse> CreateAsync(string slug, CreateContactMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required.");

        if (string.IsNullOrWhiteSpace(request.Message))
            throw new ArgumentException("Message is required.");

        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Phone))
            throw new ArgumentException("Either email or phone number is required.");

        if (!string.IsNullOrWhiteSpace(request.Email) && !IsValidEmail(request.Email))
            throw new ArgumentException("Email address is not valid.");

        var store = await db.Stores.FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive)
            ?? throw new NotFoundException($"Store '{slug}' not found.");

        var message = new ContactMessage
        {
            Id = Guid.NewGuid(),
            StoreId = store.Id,
            Name = request.Name.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Message = request.Message.Trim(),
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.ContactMessages.Add(message);
        await db.SaveChangesAsync();

        return message.ToDto();
    }

    public async Task<PaginatedResult<ContactMessageResponse>> GetAllForOwnStoreAsync(string clerkUserId, PaginatedRequest pagination)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var query = db.ContactMessages
            .Where(m => m.StoreId == store.Id)
            .OrderByDescending(m => m.CreatedAt);

        var result = await query.ToPaginatedResultAsync(pagination);
        return result.Map(m => m.ToDto());
    }

    public async Task<ContactMessageResponse> MarkAsReadAsync(string clerkUserId, Guid id)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var message = await db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id && m.StoreId == store.Id)
            ?? throw new NotFoundException("Message not found.");

        if (!message.IsRead)
        {
            message.IsRead = true;
            await db.SaveChangesAsync();
        }

        return message.ToDto();
    }

    public async Task<int> GetUnreadCountForOwnStoreAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        return await db.ContactMessages.CountAsync(m => m.StoreId == store.Id && !m.IsRead);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
