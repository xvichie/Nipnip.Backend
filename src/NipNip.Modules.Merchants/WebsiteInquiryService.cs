using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Data.Extensions;
using NipNip.Modules.Merchants.DTOs;
using NipNip.Modules.Merchants.Extensions;
using NipNip.Shared.Exceptions;
using NipNip.Shared.Pagination;

namespace NipNip.Modules.Merchants;

public class WebsiteInquiryService(AppDbContext db)
{
    // clerkUserId is null for the anonymous marketing-site footer form, and set for the
    // post-signup onboarding-replacement flow (see WebsiteInquiryController.Create). When set,
    // a second submission from the same signed-in user just returns their existing row instead
    // of creating a duplicate — both as a UX nicety (repeat visits to /onboarding don't need to
    // resubmit) and because ClerkUserId is DB-unique when non-null.
    public async Task<WebsiteInquiryResponse> CreateAsync(CreateWebsiteInquiryRequest request, string? clerkUserId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required.");

        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Phone))
            throw new ArgumentException("Either email or phone number is required.");

        if (!string.IsNullOrWhiteSpace(request.Email) && !IsValidEmail(request.Email))
            throw new ArgumentException("Email address is not valid.");

        if (clerkUserId is not null)
        {
            var existing = await db.WebsiteInquiries.FirstOrDefaultAsync(i => i.ClerkUserId == clerkUserId);
            if (existing is not null) return existing.ToDto();
        }

        var inquiry = new WebsiteInquiry
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            StoreName = string.IsNullOrWhiteSpace(request.StoreName) ? null : request.StoreName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Message = string.IsNullOrWhiteSpace(request.Message) ? "" : request.Message.Trim(),
            ClerkUserId = clerkUserId,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.WebsiteInquiries.Add(inquiry);
        await db.SaveChangesAsync();

        return inquiry.ToDto();
    }

    public async Task<WebsiteInquiryResponse?> GetOwnAsync(string clerkUserId)
    {
        var inquiry = await db.WebsiteInquiries.FirstOrDefaultAsync(i => i.ClerkUserId == clerkUserId);
        return inquiry?.ToDto();
    }

    public async Task<PaginatedResult<WebsiteInquiryResponse>> GetAllAdminAsync(PaginatedRequest pagination)
    {
        var result = await db.WebsiteInquiries
            .OrderByDescending(i => i.CreatedAt)
            .ToPaginatedResultAsync(pagination);
        return result.Map(i => i.ToDto());
    }

    public async Task<WebsiteInquiryResponse> MarkAsReadAsync(Guid id)
    {
        var inquiry = await db.WebsiteInquiries.FindAsync(id)
            ?? throw new NotFoundException("Inquiry not found.");

        if (!inquiry.IsRead)
        {
            inquiry.IsRead = true;
            await db.SaveChangesAsync();
        }

        return inquiry.ToDto();
    }

    public async Task<int> GetUnreadCountAdminAsync() =>
        await db.WebsiteInquiries.CountAsync(i => !i.IsRead);

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
