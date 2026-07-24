using NipNip.Data.Enums;

namespace NipNip.Data.Entities;

public class StoreDiscountCode
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string Code { get; set; } = "";
    public DiscountCodeType Type { get; set; }
    public decimal Value { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public int? MaxUses { get; set; }
    public int UsesCount { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }

    public Store Store { get; set; } = null!;
}
