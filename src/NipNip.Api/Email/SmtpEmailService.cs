using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using NipNip.Shared.Email;

namespace NipNip.Api.Email;

public class SmtpEmailService(IConfiguration config) : IEmailService
{
    private static readonly Dictionary<string, string> PaymentMethodLabels = new()
    {
        ["CashOnDelivery"] = "გადახდა მიტანისას",
        ["BankTransfer"] = "საბანკო გადარიცხვა",
    };

    public async Task SendConversionNotificationAsync(ConversionEmailData data)
    {
        var settings = GetSettings();
        if (settings is null) return;

        var html = BuildConversionHtml(data, settings.AppUrl);
        await SendAsync(settings, data.ToEmail, $"🎉 ახალი გაყიდვა — {data.OrderAmount:F2} {data.Currency}", html);
    }

    public async Task SendOrderConfirmationAsync(OrderConfirmationEmailData data)
    {
        var settings = GetSettings();
        if (settings is null) return;

        var html = BuildOrderConfirmationHtml(data, settings.AppUrl);
        await SendAsync(settings, data.ToEmail, $"შეკვეთა მიღებულია — {data.StoreName}", html);
    }

    private SmtpSettings? GetSettings()
    {
        var section = config.GetSection("Email");
        var host = section["SmtpHost"];
        var username = section["Username"];
        var password = section["Password"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username))
            return null;

        var port = int.TryParse(section["SmtpPort"], out var p) ? p : 587;

        return new SmtpSettings(
            host,
            port,
            username,
            password ?? "",
            section["FromAddress"] ?? "noreply@nipnip.ge",
            section["FromName"] ?? "NipNip",
            section["AppUrl"] ?? "http://localhost:3000"
        );
    }

    private static async Task SendAsync(SmtpSettings settings, string toEmail, string subject, string html)
    {
        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(settings.Username, settings.Password),
        };

        using var message = new MailMessage
        {
            From = new MailAddress(settings.FromAddress, settings.FromName),
            Subject = subject,
            Body = html,
            IsBodyHtml = true,
        };
        message.To.Add(toEmail);

        await client.SendMailAsync(message);
    }

    private static string BuildConversionHtml(ConversionEmailData d, string appUrl)
    {
        var detailUrl = $"{appUrl}/dashboard/merchant/conversions/{d.ConversionId}";

        return $"""
            <!DOCTYPE html>
            <html>
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="margin:0;padding:0;background:#0c0c14;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Helvetica,sans-serif;color:#ffffff;">
              <div style="max-width:520px;margin:40px auto;padding:0 16px;">

                <div style="text-align:center;padding:32px 0 20px;">
                  <span style="font-size:26px;font-weight:900;background:linear-gradient(135deg,#a855f7,#e879f9);-webkit-background-clip:text;-webkit-text-fill-color:transparent;background-clip:text;">NipNip</span>
                </div>

                <div style="background:#18181f;border:1px solid rgba(255,255,255,0.08);border-radius:16px;padding:32px;">

                  <div style="font-size:32px;margin-bottom:10px;">🎉</div>
                  <h2 style="margin:0 0 6px;font-size:22px;font-weight:800;color:#ffffff;">ახალი გაყიდვა!</h2>
                  <p style="margin:0 0 28px;font-size:14px;color:rgba(255,255,255,0.45);line-height:1.6;">
                    კრეატორ <strong style="color:rgba(255,255,255,0.8);">{d.CreatorName} (@{d.CreatorSlug})</strong>-ის ლინკიდან განხორციელდა გაყიდვა <strong style="color:rgba(255,255,255,0.8);">{d.MerchantName}</strong>-ში.
                  </p>

                  <div style="background:rgba(255,255,255,0.03);border:1px solid rgba(255,255,255,0.07);border-radius:12px;overflow:hidden;margin-bottom:24px;">
                    {Row("კრეატორი", $"@{d.CreatorSlug}")}
                    {Row("შეკვეთის ID", d.OrderId)}
                    {Row("გაყიდვის თანხა", $"{d.OrderAmount:F2} {d.Currency}")}
                    {Row("შენი კომისია", $"<span style=\"color:#a855f7;font-weight:700;\">{d.CommissionAmount:F2} {d.Currency}</span>")}
                  </div>

                  <a href="{detailUrl}" style="display:block;text-align:center;background:linear-gradient(135deg,#7c3aed,#a855f7);color:#ffffff;font-weight:700;font-size:14px;padding:14px 24px;border-radius:10px;text-decoration:none;letter-spacing:0.01em;">
                    სრული დეტალების ნახვა →
                  </a>

                </div>

                <p style="text-align:center;font-size:12px;color:rgba(255,255,255,0.2);margin-top:20px;padding-bottom:40px;">
                  © {DateTime.UtcNow.Year} NipNip · ეს შეტყობინება გაიგზავნა ავტომატურად
                </p>
              </div>
            </body>
            </html>
            """;
    }

    private static string BuildOrderConfirmationHtml(OrderConfirmationEmailData d, string appUrl)
    {
        var storeUrl = $"{appUrl}/store/{d.StoreSlug}";
        var paymentLabel = PaymentMethodLabels.GetValueOrDefault(d.PaymentMethod, d.PaymentMethod);

        var itemRows = string.Join("", d.Items.Select(i => Row(
            $"{i.ProductName} ×{i.Quantity}",
            $"{(i.Price * i.Quantity):F2} ₾"
        )));

        var shippingLabel = d.ShippingZoneName is { Length: > 0 } zoneName ? $"მიწოდება ({zoneName})" : "მიწოდება";
        var shippingRow = d.ShippingZoneName is null && d.ShippingFee == 0
            ? ""
            : Row(shippingLabel, d.ShippingFee == 0 ? "უფასო" : $"{d.ShippingFee:F2} ₾");

        var notesBlock = string.IsNullOrWhiteSpace(d.PaymentNotes) ? "" : $"""
            <div style="background:rgba(168,85,247,0.08);border:1px solid rgba(168,85,247,0.25);border-radius:12px;padding:16px;margin-bottom:24px;">
              <p style="margin:0 0 6px;font-size:12px;font-weight:700;color:#e879f9;text-transform:uppercase;letter-spacing:0.04em;">{paymentLabel}</p>
              <p style="margin:0;font-size:13px;color:rgba(255,255,255,0.75);line-height:1.6;white-space:pre-line;">{d.PaymentNotes}</p>
            </div>
            """;

        return $"""
            <!DOCTYPE html>
            <html>
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="margin:0;padding:0;background:#0c0c14;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Helvetica,sans-serif;color:#ffffff;">
              <div style="max-width:520px;margin:40px auto;padding:0 16px;">

                <div style="text-align:center;padding:32px 0 20px;">
                  <span style="font-size:26px;font-weight:900;background:linear-gradient(135deg,#a855f7,#e879f9);-webkit-background-clip:text;-webkit-text-fill-color:transparent;background-clip:text;">NipNip</span>
                </div>

                <div style="background:#18181f;border:1px solid rgba(255,255,255,0.08);border-radius:16px;padding:32px;">

                  <div style="font-size:32px;margin-bottom:10px;">✅</div>
                  <h2 style="margin:0 0 6px;font-size:22px;font-weight:800;color:#ffffff;">შეკვეთა მიღებულია!</h2>
                  <p style="margin:0 0 28px;font-size:14px;color:rgba(255,255,255,0.45);line-height:1.6;">
                    გმადლობთ, {d.CustomerName}. თქვენი შეკვეთა <strong style="color:rgba(255,255,255,0.8);">{d.StoreName}</strong>-ში მიღებულია.
                  </p>

                  <div style="background:rgba(255,255,255,0.03);border:1px solid rgba(255,255,255,0.07);border-radius:12px;overflow:hidden;margin-bottom:16px;">
                    {itemRows}
                    {shippingRow}
                    {Row("სულ", $"<span style=\"color:#a855f7;font-weight:700;\">{d.Total:F2} ₾</span>")}
                    {Row("გადახდის მეთოდი", paymentLabel)}
                  </div>

                  {notesBlock}

                  <a href="{storeUrl}" style="display:block;text-align:center;background:linear-gradient(135deg,#7c3aed,#a855f7);color:#ffffff;font-weight:700;font-size:14px;padding:14px 24px;border-radius:10px;text-decoration:none;letter-spacing:0.01em;">
                    მაღაზიაში დაბრუნება →
                  </a>

                </div>

                <p style="text-align:center;font-size:12px;color:rgba(255,255,255,0.2);margin-top:20px;padding-bottom:40px;">
                  © {DateTime.UtcNow.Year} NipNip · ეს შეტყობინება გაიგზავნა ავტომატურად
                </p>
              </div>
            </body>
            </html>
            """;
    }

    private static string Row(string label, string value) =>
        $"<div style=\"display:flex;justify-content:space-between;align-items:center;padding:12px 16px;border-bottom:1px solid rgba(255,255,255,0.05);\">" +
        $"<span style=\"font-size:13px;color:rgba(255,255,255,0.4);\">{label}</span>" +
        $"<span style=\"font-size:13px;color:rgba(255,255,255,0.8);\">{value}</span>" +
        "</div>";

    private record SmtpSettings(
        string Host,
        int Port,
        string Username,
        string Password,
        string FromAddress,
        string FromName,
        string AppUrl
    );
}
