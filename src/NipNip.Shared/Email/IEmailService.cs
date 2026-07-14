namespace NipNip.Shared.Email;

public interface IEmailService
{
    Task SendConversionNotificationAsync(ConversionEmailData data);
    Task SendOrderConfirmationAsync(OrderConfirmationEmailData data);
}

public record ConversionEmailData(
    string ToEmail,
    string MerchantName,
    string CreatorName,
    string CreatorSlug,
    string OrderId,
    decimal OrderAmount,
    decimal CommissionAmount,
    string Currency,
    string ConversionId
);

public record OrderConfirmationEmailItem(
    string ProductName,
    int Quantity,
    decimal Price
);

public record OrderConfirmationEmailData(
    string ToEmail,
    string CustomerName,
    string StoreName,
    string StoreSlug,
    string OrderId,
    List<OrderConfirmationEmailItem> Items,
    decimal Total,
    decimal ShippingFee,
    string? ShippingZoneName,
    string PaymentMethod,
    string? PaymentNotes
);
