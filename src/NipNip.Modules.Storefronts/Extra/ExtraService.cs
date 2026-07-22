using System.Text.RegularExpressions;
using NipNip.Data;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts.Extra;

public class ExtraService(AppDbContext db, StoreService storeService)
{
    // --- Merchant-side connection (seller ID entered once in Store Settings) ---

    public async Task ConnectAsync(string clerkUserId, ConnectExtraRequest request)
    {
        var sellerId = ExtractSellerId(request.SellerId);
        if (sellerId is null)
            throw new ArgumentException("Enter a valid Extra.ge seller ID or seller page URL.");

        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        store.ExtraSellerId = sellerId;
        store.ExtraConnectedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task DisconnectAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        store.ExtraSellerId = null;
        store.ExtraConnectedAt = null;
        await db.SaveChangesAsync();
    }

    public async Task<ExtraStatusResponse> GetStatusAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        return new ExtraStatusResponse(store.ExtraConnectedAt.HasValue, store.ExtraSellerId, store.ExtraConnectedAt);
    }

    // Accepts either a bare numeric seller ID ("228") or a full seller page URL
    // (e.g. "https://extra.ge/seller/algorithmalgoritmi/228") and extracts the numeric ID.
    private static string? ExtractSellerId(string input)
    {
        var trimmed = input.Trim();
        if (trimmed.Length == 0) return null;

        var match = Regex.Match(trimmed, @"/seller/[^/]+/(\d+)");
        if (match.Success) return match.Groups[1].Value;

        return Regex.IsMatch(trimmed, @"^\d+$") ? trimmed : null;
    }
}
