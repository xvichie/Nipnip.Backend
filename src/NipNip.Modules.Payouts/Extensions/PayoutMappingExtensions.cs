using NipNip.Data.Entities;
using NipNip.Modules.Payouts.DTOs;

namespace NipNip.Modules.Payouts.Extensions;

public static class PayoutMappingExtensions
{
    public static PayoutResponse ToDto(this Payout payout) =>
        new(
            payout.Id,
            payout.CreatorId,
            payout.Creator?.Name ?? "",
            payout.Creator?.Slug ?? "",
            payout.RequestedAmount,
            payout.AmountSent,
            payout.Currency,
            payout.Status.ToString(),
            payout.Notes,
            payout.CreatedAt,
            payout.PaidAt
        );
}
