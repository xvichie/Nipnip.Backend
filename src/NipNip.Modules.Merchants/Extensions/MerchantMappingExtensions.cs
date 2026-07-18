using NipNip.Data.Entities;
using NipNip.Modules.Merchants.DTOs;

namespace NipNip.Modules.Merchants.Extensions;

public static class MerchantMappingExtensions
{
    public static MerchantResponse ToDto(this Merchant merchant) =>
        new(
            merchant.Id,
            merchant.Name,
            merchant.Slug,
            merchant.LogoUrl,
            merchant.WebsiteUrl,
            merchant.InstagramHandle,
            merchant.Description,
            merchant.CommissionPercent,
            merchant.Balance,
            merchant.ApiKey,
            merchant.NotificationEmail,
            merchant.IsActive,
            merchant.IsHighlighted,
            merchant.IsTest,
            merchant.CreatedAt
        );
}
