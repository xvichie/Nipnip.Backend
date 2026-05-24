using NipNip.Data.Entities;
using NipNip.Modules.Codes.DTOs;

namespace NipNip.Modules.Codes.Extensions;

public static class CodeMappingExtensions
{
    public static CodeResponse ToDto(this DiscountCode code) =>
        new(code.Id, code.CreatorId, code.MerchantId, code.Code, code.CreatedAt);
}
