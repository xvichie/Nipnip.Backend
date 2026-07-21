using System.Text.RegularExpressions;
using NipNip.Data;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts.MyMarket;

public class MyMarketService(AppDbContext db, StoreService storeService)
{
    // --- Merchant-side connection (shop ID entered once in Store Settings) ---

    public async Task ConnectAsync(string clerkUserId, ConnectMyMarketRequest request)
    {
        var shopId = ExtractShopId(request.ShopId);
        if (shopId is null)
            throw new ArgumentException("Enter a valid MyMarket shop ID or shop URL.");

        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        store.MyMarketShopId = shopId;
        store.MyMarketConnectedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task DisconnectAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        store.MyMarketShopId = null;
        store.MyMarketConnectedAt = null;
        await db.SaveChangesAsync();
    }

    public async Task<MyMarketStatusResponse> GetStatusAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        return new MyMarketStatusResponse(store.MyMarketConnectedAt.HasValue, store.MyMarketShopId, store.MyMarketConnectedAt);
    }

    // Accepts either a bare numeric ID ("15915") or a full shop URL
    // (e.g. "https://mymarket.ge/shops/15915/") and extracts the numeric ID.
    private static string? ExtractShopId(string input)
    {
        var trimmed = input.Trim();
        if (trimmed.Length == 0) return null;

        var match = Regex.Match(trimmed, @"/shops/(\d+)");
        if (match.Success) return match.Groups[1].Value;

        return Regex.IsMatch(trimmed, @"^\d+$") ? trimmed : null;
    }
}
