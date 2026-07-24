using NipNip.Data.Entities;
using NipNip.Modules.Merchants.DTOs;

namespace NipNip.Modules.Merchants.Extensions;

public static class WebsiteInquiryMappingExtensions
{
    public static WebsiteInquiryResponse ToDto(this WebsiteInquiry inquiry) =>
        new(inquiry.Id, inquiry.Name, inquiry.StoreName, inquiry.Email, inquiry.Phone, inquiry.Message, inquiry.IsRead, inquiry.CreatedAt);
}
