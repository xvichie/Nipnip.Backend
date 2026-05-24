namespace NipNip.Shared.Email;

public interface IEmailService
{
    Task SendConversionNotificationAsync(ConversionEmailData data);
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
