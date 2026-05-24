using NipNip.Data.Enums;

namespace NipNip.Data.Entities;

public class Conversion
{
    public Guid Id { get; set; }
    public Guid MerchantId { get; set; }
    public Guid CreatorId { get; set; }
    public Guid? ClickId { get; set; }
    public string OrderId { get; set; } = "";
    public decimal OrderAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal CreatorFeeAmount { get; set; }
    public decimal MerchantFeeAmount { get; set; }
    public decimal CreatorEarnings { get; set; }
    public string Currency { get; set; } = "GEL";
    public ConversionSource Source { get; set; }
    public ConversionStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Merchant Merchant { get; set; } = null!;
    public Creator Creator { get; set; } = null!;
    public Click? Click { get; set; }
}
