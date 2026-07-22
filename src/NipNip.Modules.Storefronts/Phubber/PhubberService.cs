using System.Text.RegularExpressions;
using NipNip.Data;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts.Phubber;

public class PhubberService(AppDbContext db, StoreService storeService)
{
    // --- Merchant-side connection (seller ID entered once in Store Settings) ---

    public async Task ConnectAsync(string clerkUserId, ConnectPhubberRequest request)
    {
        var sellerId = ExtractSellerId(request.SellerId);
        if (sellerId is null)
            throw new ArgumentException("Enter a valid Phubber seller ID or seller page URL.");

        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        store.PhubberSellerId = sellerId;
        store.PhubberConnectedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task DisconnectAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        store.PhubberSellerId = null;
        store.PhubberConnectedAt = null;
        await db.SaveChangesAsync();
    }

    public async Task<PhubberStatusResponse> GetStatusAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        return new PhubberStatusResponse(store.PhubberConnectedAt.HasValue, store.PhubberSellerId, store.PhubberConnectedAt);
    }

    // Accepts either a bare seller ID (a 24-char Mongo ObjectId, e.g. "5fae60d4421aa904014666d3")
    // or a full seller page URL (e.g. "https://beta.phubber.ge/seller-page/5fae60d4421aa904014666d3").
    private static string? ExtractSellerId(string input)
    {
        var trimmed = input.Trim();
        if (trimmed.Length == 0) return null;

        var match = Regex.Match(trimmed, @"/seller-page/([a-f0-9]{24})", RegexOptions.IgnoreCase);
        if (match.Success) return match.Groups[1].Value.ToLowerInvariant();

        return Regex.IsMatch(trimmed, @"^[a-f0-9]{24}$", RegexOptions.IgnoreCase) ? trimmed.ToLowerInvariant() : null;
    }
}
