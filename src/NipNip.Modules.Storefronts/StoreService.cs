using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Modules.Storefronts.Extensions;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts;

public class StoreService(AppDbContext db, VercelDomainService vercel)
{
    private static readonly Regex SlugRegex = new(@"^[a-z0-9][a-z0-9-]*$", RegexOptions.Compiled);
    private static readonly Regex DomainRegex = new(@"^([a-z0-9](-?[a-z0-9])*\.)+[a-z]{2,}$", RegexOptions.Compiled);
    private static readonly string[] KnownThemeIds = ["minimal", "bold", "classic", "luxury", "vibrant", "commerce", "editorial"];

    public async Task<StoreResponse> CreateAsync(string clerkUserId, CreateStoreRequest request)
    {
        var merchant = await db.Merchants.FirstOrDefaultAsync(m => m.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a merchant account.");

        var store = await CreateForMerchantAsync(merchant.Id, request);
        return store.ToDto();
    }

    public async Task<StoreResponse?> GetByMerchantIdAdminAsync(Guid merchantId)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.MerchantId == merchantId);
        return store?.ToDto();
    }

    public async Task<StoreResponse> CreateAdminAsync(Guid merchantId, CreateStoreRequest request)
    {
        if (!await db.Merchants.AnyAsync(m => m.Id == merchantId))
            throw new NotFoundException("Merchant not found.");

        var store = await CreateForMerchantAsync(merchantId, request);
        return store.ToDto();
    }

    private async Task<Store> CreateForMerchantAsync(Guid merchantId, CreateStoreRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required.");

        if (string.IsNullOrWhiteSpace(request.Slug))
            throw new ArgumentException("Slug is required.");

        if (!SlugRegex.IsMatch(request.Slug))
            throw new ArgumentException("Slug must be lowercase alphanumeric with optional hyphens and cannot start with a hyphen.");

        if (request.ThemeId is not null && !KnownThemeIds.Contains(request.ThemeId))
            throw new ArgumentException($"ThemeId must be one of: {string.Join(", ", KnownThemeIds)}.");

        ValidateThemeConfig(request.ThemeConfig);

        if (await db.Stores.AnyAsync(s => s.MerchantId == merchantId))
            throw new ConflictException("This merchant already has a store.");

        if (await db.Stores.AnyAsync(s => s.Slug == request.Slug))
            throw new ConflictException($"Slug '{request.Slug}' is already taken.");

        var store = new Store
        {
            Id = Guid.NewGuid(),
            MerchantId = merchantId,
            Slug = request.Slug,
            Name = request.Name.Trim(),
            ThemeId = request.ThemeId ?? "minimal",
            ThemeConfig = request.ThemeConfig ?? "{}",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Stores.Add(store);
        await db.SaveChangesAsync();

        return store;
    }

    public async Task<StoreResponse> GetMeAsync(string clerkUserId)
    {
        var store = await GetOwnStoreAsync(clerkUserId);
        return store.ToDto();
    }

    public async Task<StoreResponse> UpdateMeAsync(string clerkUserId, UpdateStoreRequest request)
    {
        var store = await GetOwnStoreAsync(clerkUserId);

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Name cannot be empty.");
            store.Name = request.Name.Trim();
        }

        if (request.ThemeId is not null)
        {
            if (!KnownThemeIds.Contains(request.ThemeId))
                throw new ArgumentException($"ThemeId must be one of: {string.Join(", ", KnownThemeIds)}.");
            store.ThemeId = request.ThemeId;
        }

        if (request.ThemeConfig is not null)
        {
            ValidateThemeConfig(request.ThemeConfig);
            store.ThemeConfig = request.ThemeConfig;
        }

        if (request.IsActive.HasValue) store.IsActive = request.IsActive.Value;
        if (request.AffiliateEnabled.HasValue) store.AffiliateEnabled = request.AffiliateEnabled.Value;

        await db.SaveChangesAsync();
        return store.ToDto();
    }

    public async Task<StoreResponse> GetBySlugAsync(string slug)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive)
            ?? throw new NotFoundException($"Store '{slug}' not found.");

        return store.ToDto();
    }

    // Backs the root-level sitemap index — every active store belonging to a public, active,
    // non-test merchant, so Google can discover every storefront subdomain automatically as
    // soon as it goes live, without anyone needing to submit it by hand.
    public async Task<List<StoreSitemapEntryResponse>> GetSitemapSlugsAsync()
    {
        return await db.Stores
            .Where(s => s.IsActive && s.Merchant.IsActive && s.Merchant.IsPublic && !s.Merchant.IsTest)
            .Select(s => new StoreSitemapEntryResponse(s.Slug))
            .ToListAsync();
    }

    public async Task<StoreDomainResponse> GetDomainStatusAsync(string clerkUserId)
    {
        var store = await GetOwnStoreAsync(clerkUserId);
        if (store.CustomDomain is null)
            return new StoreDomainResponse(null, false, null, []);

        var status = await vercel.GetStatusAsync(store.CustomDomain);

        if (status.Verified != store.CustomDomainVerifiedAt.HasValue)
        {
            store.CustomDomainVerifiedAt = status.Verified ? DateTimeOffset.UtcNow : null;
            await db.SaveChangesAsync();
        }

        return ToDomainResponse(store, status);
    }

    public async Task<StoreDomainResponse> SetDomainAsync(string clerkUserId, SetStoreDomainRequest request)
    {
        var domain = NormalizeDomain(request.Domain);
        if (!DomainRegex.IsMatch(domain))
            throw new ArgumentException("Please enter a valid domain, e.g. myshop.com.");

        var store = await GetOwnStoreAsync(clerkUserId);

        if (await db.Stores.AnyAsync(s => s.Id != store.Id && s.CustomDomain == domain))
            throw new ConflictException("This domain is already connected to another store.");

        if (store.CustomDomain is { } previousDomain && previousDomain != domain)
        {
            try { await vercel.RemoveDomainAsync(previousDomain); }
            catch (ArgumentException) { /* best effort — don't block reassignment on cleanup failure */ }
        }

        var status = await vercel.AddDomainAsync(domain);

        store.CustomDomain = domain;
        store.CustomDomainVerifiedAt = status.Verified ? DateTimeOffset.UtcNow : null;
        await db.SaveChangesAsync();

        return ToDomainResponse(store, status);
    }

    public async Task RemoveDomainAsync(string clerkUserId)
    {
        var store = await GetOwnStoreAsync(clerkUserId);
        if (store.CustomDomain is null) return;

        try { await vercel.RemoveDomainAsync(store.CustomDomain); }
        catch (ArgumentException) { /* best effort — still unbind locally even if Vercel cleanup fails */ }

        store.CustomDomain = null;
        store.CustomDomainVerifiedAt = null;
        await db.SaveChangesAsync();
    }

    public async Task<string?> ResolveSlugByDomainAsync(string domain)
    {
        var normalized = NormalizeDomain(domain);
        var store = await db.Stores.FirstOrDefaultAsync(s =>
            s.CustomDomain == normalized && s.CustomDomainVerifiedAt != null && s.IsActive);
        return store?.Slug;
    }

    private static StoreDomainResponse ToDomainResponse(Store store, VercelDomainStatus status) =>
        new(
            store.CustomDomain,
            status.Verified,
            store.CustomDomainVerifiedAt,
            status.Instructions.Select(i => new DomainDnsRecordResponse(i.Type, i.Name, i.Value)).ToList()
        );

    private static string NormalizeDomain(string domain) =>
        domain.Trim().ToLowerInvariant()
            .Replace("https://", "")
            .Replace("http://", "")
            .Split('/')[0];

    internal async Task<Store> GetOwnStoreAsync(string clerkUserId)
    {
        var merchant = await db.Merchants.FirstOrDefaultAsync(m => m.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a merchant account.");

        return await db.Stores.FirstOrDefaultAsync(s => s.MerchantId == merchant.Id)
            ?? throw new NotFoundException("You don't have a store yet.");
    }

    private static void ValidateThemeConfig(string? themeConfig)
    {
        if (themeConfig is null) return;
        try
        {
            JsonDocument.Parse(themeConfig);
        }
        catch (JsonException)
        {
            throw new ArgumentException("ThemeConfig must be valid JSON.");
        }
    }
}
