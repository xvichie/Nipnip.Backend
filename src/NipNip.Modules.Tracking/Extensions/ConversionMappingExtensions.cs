using NipNip.Data.Entities;
using NipNip.Modules.Tracking.DTOs;

namespace NipNip.Modules.Tracking.Extensions;

public static class ConversionMappingExtensions
{
    public static ConversionResponse ToDto(this Conversion conversion) =>
        new(
            conversion.Id,
            conversion.MerchantId,
            conversion.CreatorId,
            conversion.ClickId,
            conversion.OrderId,
            conversion.OrderAmount,
            conversion.CommissionAmount,
            conversion.CreatorFeeAmount,
            conversion.MerchantFeeAmount,
            conversion.CreatorEarnings,
            conversion.Currency,
            conversion.Source.ToString(),
            conversion.Status.ToString(),
            conversion.CreatedAt
        );
}
