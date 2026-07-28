using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using NipNip.Shared.Email;

namespace NipNip.Api.Email;

public class SmtpEmailService(IConfiguration config) : IEmailService
{
    // Order confirmation email chrome — the whole email renders in the shopper's checkout-time
    // language (OrderConfirmationEmailData.Lang), unlike the merchant/creator sale-notification
    // email (BuildConversionHtml) which stays Georgian since it's an internal ops notification,
    // not shopper-facing.
    private record OrderEmailStrings(
        string Subject,
        string Heading,
        string ThankYou,
        string Total,
        string PaymentMethodRow,
        string Shipping,
        string ShippingWithZone,
        string Free,
        string BackToStore,
        string Footer,
        Dictionary<string, string> PaymentMethodLabels
    );

    private static readonly Dictionary<string, OrderEmailStrings> OrderEmailStringsByLang = new()
    {
        ["ka"] = new OrderEmailStrings(
            Subject: "შეკვეთა მიღებულია — {0}",
            Heading: "შეკვეთა მიღებულია!",
            ThankYou: "გმადლობთ, {0}. თქვენი შეკვეთა {1}-ში მიღებულია.",
            Total: "სულ",
            PaymentMethodRow: "გადახდის მეთოდი",
            Shipping: "მიწოდება",
            ShippingWithZone: "მიწოდება ({0})",
            Free: "უფასო",
            BackToStore: "მაღაზიაში დაბრუნება →",
            Footer: "ეს შეტყობინება გაიგზავნა ავტომატურად",
            PaymentMethodLabels: new() { ["CashOnDelivery"] = "გადახდა მიტანისას", ["BankTransfer"] = "საბანკო გადარიცხვა" }
        ),
        ["en"] = new OrderEmailStrings(
            Subject: "Order confirmed — {0}",
            Heading: "Order confirmed!",
            ThankYou: "Thank you, {0}. Your order at {1} has been received.",
            Total: "Total",
            PaymentMethodRow: "Payment method",
            Shipping: "Delivery",
            ShippingWithZone: "Delivery ({0})",
            Free: "Free",
            BackToStore: "Back to store →",
            Footer: "This message was sent automatically",
            PaymentMethodLabels: new() { ["CashOnDelivery"] = "Cash on delivery", ["BankTransfer"] = "Bank transfer" }
        ),
        ["ru"] = new OrderEmailStrings(
            Subject: "Заказ получен — {0}",
            Heading: "Заказ получен!",
            ThankYou: "Спасибо, {0}. Ваш заказ в {1} получен.",
            Total: "Итого",
            PaymentMethodRow: "Способ оплаты",
            Shipping: "Доставка",
            ShippingWithZone: "Доставка ({0})",
            Free: "Бесплатно",
            BackToStore: "Вернуться в магазин →",
            Footer: "Это сообщение отправлено автоматически",
            PaymentMethodLabels: new() { ["CashOnDelivery"] = "Оплата при получении", ["BankTransfer"] = "Банковский перевод" }
        ),
    };

    private static OrderEmailStrings GetOrderEmailStrings(string lang) =>
        OrderEmailStringsByLang.TryGetValue(lang, out var strings) ? strings : OrderEmailStringsByLang["ka"];

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
        var subject = string.Format(GetOrderEmailStrings(data.Lang).Subject, data.StoreName);
        await SendAsync(settings, data.ToEmail, subject, html);
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
        var s = GetOrderEmailStrings(d.Lang);
        var storeUrl = $"{appUrl}/store/{d.StoreSlug}";
        var paymentLabel = s.PaymentMethodLabels.GetValueOrDefault(d.PaymentMethod, d.PaymentMethod);

        var itemRows = string.Join("", d.Items.Select(i => Row(
            $"{i.ProductName} ×{i.Quantity}",
            $"{(i.Price * i.Quantity):F2} ₾"
        )));

        var shippingLabel = d.ShippingZoneName is { Length: > 0 } zoneName ? string.Format(s.ShippingWithZone, zoneName) : s.Shipping;
        var shippingRow = d.ShippingZoneName is null && d.ShippingFee == 0
            ? ""
            : Row(shippingLabel, d.ShippingFee == 0 ? s.Free : $"{d.ShippingFee:F2} ₾");

        var notesBlock = string.IsNullOrWhiteSpace(d.PaymentNotes) ? "" : $"""
            <div style="background:rgba(168,85,247,0.08);border:1px solid rgba(168,85,247,0.25);border-radius:12px;padding:16px;margin-bottom:24px;">
              <p style="margin:0 0 6px;font-size:12px;font-weight:700;color:#e879f9;text-transform:uppercase;letter-spacing:0.04em;">{paymentLabel}</p>
              <p style="margin:0;font-size:13px;color:rgba(255,255,255,0.75);line-height:1.6;white-space:pre-line;">{d.PaymentNotes}</p>
            </div>
            """;

        var thankYou = string.Format(s.ThankYou, d.CustomerName, $"<strong style=\"color:rgba(255,255,255,0.8);\">{d.StoreName}</strong>");

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
                  <h2 style="margin:0 0 6px;font-size:22px;font-weight:800;color:#ffffff;">{s.Heading}</h2>
                  <p style="margin:0 0 28px;font-size:14px;color:rgba(255,255,255,0.45);line-height:1.6;">
                    {thankYou}
                  </p>

                  <div style="background:rgba(255,255,255,0.03);border:1px solid rgba(255,255,255,0.07);border-radius:12px;overflow:hidden;margin-bottom:16px;">
                    {itemRows}
                    {shippingRow}
                    {Row(s.Total, $"<span style=\"color:#a855f7;font-weight:700;\">{d.Total:F2} ₾</span>")}
                    {Row(s.PaymentMethodRow, paymentLabel)}
                  </div>

                  {notesBlock}

                  <a href="{storeUrl}" style="display:block;text-align:center;background:linear-gradient(135deg,#7c3aed,#a855f7);color:#ffffff;font-weight:700;font-size:14px;padding:14px 24px;border-radius:10px;text-decoration:none;letter-spacing:0.01em;">
                    {s.BackToStore}
                  </a>

                </div>

                <p style="text-align:center;font-size:12px;color:rgba(255,255,255,0.2);margin-top:20px;padding-bottom:40px;">
                  © {DateTime.UtcNow.Year} NipNip · {s.Footer}
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
