using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts.Extensions;

public static class StoreDiscountCodeMappingExtensions
{
    public static StoreDiscountCodeResponse ToDto(this StoreDiscountCode code) =>
        new(
            code.Id,
            code.Code,
            code.Type.ToString(),
            code.Value,
            code.MinOrderAmount,
            code.MaxUses,
            code.UsesCount,
            code.ExpiresAt,
            code.IsActive,
            code.CreatedAt
        );
}
