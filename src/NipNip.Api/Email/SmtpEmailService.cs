using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using NipNip.Shared.Email;

namespace NipNip.Api.Email;

public class SmtpEmailService(IConfiguration config) : IEmailService
{
    public async Task SendConversionNotificationAsync(ConversionEmailData data)
    {
        var section = config.GetSection("Email");
        var host = section["SmtpHost"];
        var username = section["Username"];
        var password = section["Password"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username))
            return;

        var port = int.TryParse(section["SmtpPort"], out var p) ? p : 587;
        var fromAddress = section["FromAddress"] ?? "noreply@nipnip.ge";
        var fromName = section["FromName"] ?? "NipNip";
        var appUrl = section["AppUrl"] ?? "http://localhost:3000";

        var html = BuildHtml(data, appUrl);

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(username, password),
        };

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = $"🎉 ახალი გაყიდვა — {data.OrderAmount:F2} {data.Currency}",
            Body = html,
            IsBodyHtml = true,
        };
        message.To.Add(data.ToEmail);

        await client.SendMailAsync(message);
    }

    private static string BuildHtml(ConversionEmailData d, string appUrl)
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

    private static string Row(string label, string value) =>
        $"<div style=\"display:flex;justify-content:space-between;align-items:center;padding:12px 16px;border-bottom:1px solid rgba(255,255,255,0.05);\">" +
        $"<span style=\"font-size:13px;color:rgba(255,255,255,0.4);\">{label}</span>" +
        $"<span style=\"font-size:13px;color:rgba(255,255,255,0.8);\">{value}</span>" +
        "</div>";
}
