using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Data.Enums;
using NipNip.Data.Extensions;
using NipNip.Modules.Merchants.DTOs;
using NipNip.Modules.Merchants.Extensions;
using NipNip.Shared.Exceptions;
using NipNip.Shared.Pagination;

namespace NipNip.Modules.Merchants;

public class MerchantService(AppDbContext db, IConfiguration configuration)
{
    private static readonly Regex SlugRegex = new(@"^[a-z0-9][a-z0-9-]*$", RegexOptions.Compiled);

    // Test merchants/creators are hidden from the public marketplace by default — only admins and
    // the paired test account (whichever side is signed in) should see them, so QA testing doesn't
    // leak fake listings to real users.
    private async Task<bool> CanSeeTestMerchantsAsync(string? callerClerkUserId)
    {
        if (callerClerkUserId is null) return false;

        var adminIds = (configuration["AdminClerkUserIds"] ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (adminIds.Contains(callerClerkUserId)) return true;

        return await db.Creators.AnyAsync(c => c.ClerkUserId == callerClerkUserId && c.IsTest);
    }

    // Which of the given (private) merchants the caller — if they're a creator — is approved for.
    // Public merchants don't need this; callers should OR it with m.IsPublic.
    private async Task<HashSet<Guid>> GetApprovedMerchantIdsForCallerAsync(string? callerClerkUserId, IEnumerable<Guid> merchantIds)
    {
        if (callerClerkUserId is null) return [];

        var creator = await db.Creators.FirstOrDefaultAsync(c => c.ClerkUserId == callerClerkUserId);
        if (creator is null) return [];

        var ids = merchantIds.ToList();
        if (ids.Count == 0) return [];

        var approved = await db.MerchantAccessRequests
            .Where(a => a.CreatorId == creator.Id && ids.Contains(a.MerchantId) && a.Status == MerchantAccessRequestStatus.Approved)
            .Select(a => a.MerchantId)
            .ToListAsync();

        return approved.ToHashSet();
    }

    public async Task<MerchantResponse> RegisterAsync(string clerkUserId, RegisterMerchantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required.");

        if (string.IsNullOrWhiteSpace(request.Slug))
            throw new ArgumentException("Slug is required.");

        if (!SlugRegex.IsMatch(request.Slug))
            throw new ArgumentException("Slug must be lowercase alphanumeric with optional hyphens and cannot start with a hyphen.");

        if (request.CommissionPercent < 0 || request.CommissionPercent > 100)
            throw new ArgumentException("Commission percent must be between 0 and 100.");

        if (await db.Merchants.AnyAsync(m => m.ClerkUserId == clerkUserId))
            throw new ConflictException("A merchant account already exists for this user.");

        if (await db.Merchants.AnyAsync(m => m.Slug == request.Slug))
            throw new ConflictException($"Slug '{request.Slug}' is already taken.");

        var merchant = new Merchant
        {
            Id = Guid.NewGuid(),
            ClerkUserId = clerkUserId,
            Name = request.Name.Trim(),
            Slug = request.Slug,
            CommissionPercent = request.CommissionPercent,
            WebsiteUrl = request.WebsiteUrl,
            InstagramHandle = request.InstagramHandle,
            Description = request.Description,
            LogoUrl = request.LogoUrl,
            ApiKey = Guid.NewGuid().ToString("N"),
            Balance = 0m,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Merchants.Add(merchant);
        await db.SaveChangesAsync();

        return merchant.ToDto();
    }

    public async Task<PaginatedResult<MerchantResponse>> GetAllAdminAsync(int page, int pageSize)
    {
        var result = await db.Merchants
            .OrderByDescending(m => m.CreatedAt)
            .ToPaginatedResultAsync(page, pageSize);
        return result.Map(m => m.ToDto());
    }

    public async Task<MerchantResponse> UpdateAdminAsync(Guid id, UpdateMerchantRequest request)
    {
        var merchant = await db.Merchants.FindAsync(id)
            ?? throw new NotFoundException("Merchant not found.");

        var wasPrivate = !merchant.IsPublic;

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Name cannot be empty.");
            merchant.Name = request.Name.Trim();
        }

        if (request.CommissionPercent.HasValue)
        {
            if (request.CommissionPercent.Value < 0 || request.CommissionPercent.Value > 100)
                throw new ArgumentException("Commission percent must be between 0 and 100.");
            merchant.CommissionPercent = request.CommissionPercent.Value;
        }

        if (request.WebsiteUrl is not null) merchant.WebsiteUrl = request.WebsiteUrl;
        if (request.InstagramHandle is not null) merchant.InstagramHandle = request.InstagramHandle;
        if (request.Description is not null) merchant.Description = request.Description;
        if (request.LogoUrl is not null) merchant.LogoUrl = request.LogoUrl;
        if (request.NotificationEmail is not null) merchant.NotificationEmail = request.NotificationEmail;
        if (request.IsPublic.HasValue) merchant.IsPublic = request.IsPublic.Value;

        if (wasPrivate && merchant.IsPublic) await ApproveAllPendingRequestsAsync(merchant.Id);

        await db.SaveChangesAsync();
        return merchant.ToDto();
    }

    public async Task<MerchantResponse> DeactivateAsync(Guid id)
    {
        var merchant = await db.Merchants.FindAsync(id)
            ?? throw new NotFoundException("Merchant not found.");

        merchant.IsActive = false;
        await db.SaveChangesAsync();
        return merchant.ToDto();
    }

    // Hard-deletes a real merchant and everything linked to it: Store, Categories, Products (with
    // Images/Options/Variants), Collections, ProductBundles, StoreDiscountCodes, Carts, Orders,
    // StorePages, ContactMessages, Conversations, KnowledgeBaseSections, MerchantAccessRequests,
    // the old per-creator DiscountCodes, LinkTreeItems, and tracking data (Clicks/Conversions — the
    // "affiliate link" data). Payouts are untouched: a Payout belongs to a Creator, not a Merchant,
    // and can span commissions earned across many merchants.
    //
    // Most of the above cascades automatically once the Merchant row is removed, via CASCADE FKs
    // rooted at Merchant/Store. But a handful of children — OrderItem→Variant, OrderBundleItem→
    // Bundle, ProductBundleItem→Product, ProductRelation→RelatedProduct, and Category's own
    // self-referencing ParentCategoryId — are RESTRICT, not CASCADE, and Postgres checks a RESTRICT
    // FK immediately per row rather than deferring to the end of the whole cascading delete. Left
    // alone, that makes the final Merchant delete fail as soon as it tries to remove a Product or
    // Category that one of these still points at. So those have to be cleared by hand, in
    // dependency order, before the cascade delete at the bottom can run cleanly.
    public async Task DeleteMerchantPermanentlyAsync(Guid id)
    {
        var merchant = await db.Merchants.FindAsync(id)
            ?? throw new NotFoundException("Merchant not found.");

        var store = await db.Stores.FirstOrDefaultAsync(s => s.MerchantId == id);

        await using var transaction = await db.Database.BeginTransactionAsync();

        if (store is not null)
        {
            var storeId = store.Id;
            var productIds = await db.Products.Where(p => p.StoreId == storeId).Select(p => p.Id).ToListAsync();

            // Orders/Carts before Products/Bundles: clears OrderItem→Variant and
            // OrderBundleItem→Bundle before the Variants/Bundles they point at disappear.
            await db.Orders.Where(o => o.StoreId == storeId).ExecuteDeleteAsync();
            await db.Carts.Where(c => c.StoreId == storeId).ExecuteDeleteAsync();

            // Bundles before Products: clears ProductBundleItem→Product.
            await db.ProductBundles.Where(b => b.StoreId == storeId).ExecuteDeleteAsync();

            if (productIds.Count > 0)
            {
                // Both directions, regardless of which store the other side belongs to: clears
                // ProductRelation→RelatedProduct (the rare case where some other store's product
                // declared one of this merchant's products as "related").
                await db.ProductRelations
                    .Where(r => productIds.Contains(r.ProductId) || productIds.Contains(r.RelatedProductId))
                    .ExecuteDeleteAsync();

                await db.Products.Where(p => p.StoreId == storeId).ExecuteDeleteAsync();
            }

            // Clears Category's self-referencing ParentCategoryId restrict — Product→Category is
            // already safe since every Product in the store is gone by this point.
            await db.Categories.Where(c => c.StoreId == storeId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.ParentCategoryId, (Guid?)null));
        }

        db.Merchants.Remove(merchant);
        await db.SaveChangesAsync();

        await transaction.CommitAsync();
    }

    public async Task<MerchantResponse> GetMeAsync(string clerkUserId)
    {
        var merchant = await db.Merchants
            .FirstOrDefaultAsync(m => m.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a merchant account.");

        return merchant.ToDto();
    }

    public async Task<PaginatedResult<MerchantResponse>> GetAllAsync(PaginatedRequest request, string? callerClerkUserId)
    {
        var canSeeTest = await CanSeeTestMerchantsAsync(callerClerkUserId);

        var result = await db.Merchants
            .Where(m => m.IsActive && !m.IsProspect && !db.Stores.Any(s => s.MerchantId == m.Id && !s.AffiliateEnabled))
            .Where(m => canSeeTest || !m.IsTest)
            .OrderBy(m => m.Name)
            .ToPaginatedResultAsync(request);

        var approvedIds = await GetApprovedMerchantIdsForCallerAsync(callerClerkUserId, result.Items.Select(m => m.Id));

        return result.Map(m => m.ToDto(m.IsPublic || approvedIds.Contains(m.Id)));
    }

    public async Task<List<MerchantResponse>> GetHighlightedAsync(string? callerClerkUserId)
    {
        var canSeeTest = await CanSeeTestMerchantsAsync(callerClerkUserId);

        // A merchant running their own NipNip storefront with affiliate tracking switched off has
        // explicitly opted out — creators must not be able to discover them or generate a link, since
        // any sale a creator drives there would go untracked and uncompensated. Merchants with no
        // storefront at all (WooCommerce/Shopify/custom-site only) are unaffected by this check.
        var merchants = await db.Merchants
            .Where(m => m.IsActive && m.IsHighlighted && !m.IsProspect && !db.Stores.Any(s => s.MerchantId == m.Id && !s.AffiliateEnabled))
            .Where(m => canSeeTest || !m.IsTest)
            .OrderBy(m => m.Name)
            .ToListAsync();

        var approvedIds = await GetApprovedMerchantIdsForCallerAsync(callerClerkUserId, merchants.Select(m => m.Id));

        return merchants.Select(m => m.ToDto(m.IsPublic || approvedIds.Contains(m.Id))).ToList();
    }

    public async Task<MerchantResponse> ToggleTestAsync(Guid id)
    {
        var merchant = await db.Merchants.FindAsync(id)
            ?? throw new NotFoundException("Merchant not found.");
        merchant.IsTest = !merchant.IsTest;
        await db.SaveChangesAsync();
        return merchant.ToDto();
    }

    // --- Prospects (admin-built sales-demo stores — see Merchant.IsProspect) ---

    // Returns the entity (not a DTO) so the caller can immediately create the paired Store
    // using merchant.Id — MerchantService intentionally doesn't depend on StoreService, so
    // that orchestration lives in the admin controller.
    public async Task<Merchant> CreateProspectMerchantAsync(string name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.");

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug is required.");

        if (!SlugRegex.IsMatch(slug))
            throw new ArgumentException("Slug must be lowercase alphanumeric with optional hyphens and cannot start with a hyphen.");

        if (await db.Merchants.AnyAsync(m => m.Slug == slug))
            throw new ConflictException($"Slug '{slug}' is already taken.");

        var merchant = new Merchant
        {
            Id = Guid.NewGuid(),
            // No real Clerk account exists yet — a placeholder that can never collide with an
            // actual Clerk user ID (their IDs look like "user_xxx") until PromoteProspectAsync
            // reassigns it.
            ClerkUserId = $"prospect_{Guid.NewGuid():N}",
            Name = name.Trim(),
            Slug = slug,
            CommissionPercent = 0,
            ApiKey = Guid.NewGuid().ToString("N"),
            Balance = 0m,
            IsActive = true,
            IsPublic = false,
            IsProspect = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Merchants.Add(merchant);
        await db.SaveChangesAsync();

        return merchant;
    }

    public async Task<PaginatedResult<MerchantResponse>> GetAllProspectsAdminAsync(int page, int pageSize)
    {
        var result = await db.Merchants
            .Where(m => m.IsProspect)
            .OrderByDescending(m => m.CreatedAt)
            .ToPaginatedResultAsync(page, pageSize);
        return result.Map(m => m.ToDto());
    }

    // Flips a prospect over to a real merchant. Passing a new Clerk user ID reassigns ownership
    // to the actual customer's account (their first login then lands them straight in their
    // now-live merchant dashboard, with everything the admin already built still in place);
    // omit it to just unflag the prospect and reassign ownership separately later.
    public async Task<MerchantResponse> PromoteProspectAsync(Guid id, string? newClerkUserId)
    {
        var merchant = await db.Merchants.FindAsync(id)
            ?? throw new NotFoundException("Merchant not found.");

        if (!merchant.IsProspect)
            throw new ConflictException("This merchant is not a prospect.");

        if (!string.IsNullOrWhiteSpace(newClerkUserId))
        {
            if (await db.Merchants.AnyAsync(m => m.Id != id && m.ClerkUserId == newClerkUserId))
                throw new ConflictException("That Clerk user is already linked to a different merchant account.");
            merchant.ClerkUserId = newClerkUserId;
        }

        merchant.IsProspect = false;
        merchant.IsPublic = true;
        await db.SaveChangesAsync();

        return merchant.ToDto();
    }

    // Hard-deletes a never-promoted demo prospect (and its Store/Categories/Products/etc. via
    // cascade) so Prospect Studio doesn't pile up abandoned demos. Refuses anything already
    // promoted to a real merchant — those must go through actual account deletion, not this.
    public async Task DeleteProspectAsync(Guid id)
    {
        var merchant = await db.Merchants.FindAsync(id)
            ?? throw new NotFoundException("Merchant not found.");

        if (!merchant.IsProspect)
            throw new ConflictException("This merchant is not a prospect — refusing to delete a real merchant here.");

        db.Merchants.Remove(merchant);
        await db.SaveChangesAsync();
    }

    public async Task<MerchantResponse> ToggleHighlightAsync(Guid id)
    {
        var merchant = await db.Merchants.FindAsync(id)
            ?? throw new NotFoundException("Merchant not found.");
        merchant.IsHighlighted = !merchant.IsHighlighted;
        await db.SaveChangesAsync();
        return merchant.ToDto();
    }

    public async Task<MerchantResponse> GetBySlugAsync(string slug, string? callerClerkUserId)
    {
        var merchant = await db.Merchants
            .FirstOrDefaultAsync(m => m.Slug == slug && m.IsActive && !m.IsProspect
                && !db.Stores.Any(s => s.MerchantId == m.Id && !s.AffiliateEnabled))
            ?? throw new NotFoundException($"Merchant '{slug}' not found.");

        if (merchant.IsPublic) return merchant.ToDto();

        var approvedIds = await GetApprovedMerchantIdsForCallerAsync(callerClerkUserId, [merchant.Id]);
        return merchant.ToDto(approvedIds.Contains(merchant.Id));
    }

    public async Task<MerchantResponse> UpdateAsync(Guid id, string clerkUserId, UpdateMerchantRequest request)
    {
        var merchant = await db.Merchants.FindAsync(id)
            ?? throw new NotFoundException("Merchant not found.");

        if (merchant.ClerkUserId != clerkUserId)
            throw new ForbiddenException("You can only update your own merchant profile.");

        var wasPrivate = !merchant.IsPublic;

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Name cannot be empty.");
            merchant.Name = request.Name.Trim();
        }

        if (request.CommissionPercent.HasValue)
        {
            if (request.CommissionPercent.Value < 0 || request.CommissionPercent.Value > 100)
                throw new ArgumentException("Commission percent must be between 0 and 100.");
            merchant.CommissionPercent = request.CommissionPercent.Value;
        }

        if (request.WebsiteUrl is not null) merchant.WebsiteUrl = request.WebsiteUrl;
        if (request.InstagramHandle is not null) merchant.InstagramHandle = request.InstagramHandle;
        if (request.Description is not null) merchant.Description = request.Description;
        if (request.LogoUrl is not null) merchant.LogoUrl = request.LogoUrl;
        if (request.NotificationEmail is not null) merchant.NotificationEmail = request.NotificationEmail;
        if (request.IsPublic.HasValue) merchant.IsPublic = request.IsPublic.Value;

        if (wasPrivate && merchant.IsPublic) await ApproveAllPendingRequestsAsync(merchant.Id);

        await db.SaveChangesAsync();

        return merchant.ToDto();
    }

    // --- Access requests (private-store allowlist) ---

    private async Task ApproveAllPendingRequestsAsync(Guid merchantId)
    {
        var pending = await db.MerchantAccessRequests
            .Where(a => a.MerchantId == merchantId && a.Status == MerchantAccessRequestStatus.Pending)
            .ToListAsync();

        foreach (var request in pending)
        {
            request.Status = MerchantAccessRequestStatus.Approved;
            request.RespondedAt = DateTimeOffset.UtcNow;
        }
    }

    public async Task<List<MerchantAccessRequestResponse>> GetAccessRequestsAsync(string clerkUserId)
    {
        var merchant = await db.Merchants.FirstOrDefaultAsync(m => m.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a merchant account.");

        var requests = await db.MerchantAccessRequests
            .Where(a => a.MerchantId == merchant.Id)
            .Include(a => a.Creator)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return requests.Select(a => ToAccessRequestResponse(a)).ToList();
    }

    // Merchant proactively adding a creator they already trust — approved immediately, no request
    // step needed since the merchant themselves is the one calling this.
    public async Task<MerchantAccessRequestResponse> AddApprovedCreatorAsync(string clerkUserId, AddApprovedCreatorRequest request)
    {
        var merchant = await db.Merchants.FirstOrDefaultAsync(m => m.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a merchant account.");

        var creator = await db.Creators.FindAsync(request.CreatorId)
            ?? throw new NotFoundException("Creator not found.");

        if (await db.MerchantAccessRequests.AnyAsync(a => a.MerchantId == merchant.Id && a.CreatorId == creator.Id))
            throw new ConflictException("This creator already has a pending or existing request.");

        var accessRequest = new MerchantAccessRequest
        {
            Id = Guid.NewGuid(),
            MerchantId = merchant.Id,
            CreatorId = creator.Id,
            Status = MerchantAccessRequestStatus.Approved,
            CreatedAt = DateTimeOffset.UtcNow,
            RespondedAt = DateTimeOffset.UtcNow,
        };
        db.MerchantAccessRequests.Add(accessRequest);
        await db.SaveChangesAsync();

        return ToAccessRequestResponse(accessRequest, creator);
    }

    public async Task<MerchantAccessRequestResponse> RespondToAccessRequestAsync(string clerkUserId, Guid requestId, bool approve)
    {
        var accessRequest = await db.MerchantAccessRequests
            .Include(a => a.Creator)
            .Include(a => a.Merchant)
            .FirstOrDefaultAsync(a => a.Id == requestId)
            ?? throw new NotFoundException("Access request not found.");

        if (accessRequest.Merchant.ClerkUserId != clerkUserId)
            throw new ForbiddenException("You can only respond to your own store's access requests.");

        accessRequest.Status = approve ? MerchantAccessRequestStatus.Approved : MerchantAccessRequestStatus.Rejected;
        accessRequest.RespondedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return ToAccessRequestResponse(accessRequest);
    }

    private static MerchantAccessRequestResponse ToAccessRequestResponse(MerchantAccessRequest a, Creator? creator = null)
    {
        var c = creator ?? a.Creator;
        return new MerchantAccessRequestResponse(
            a.Id, c.Id, c.Name, c.Slug, c.AvatarUrl, a.Status.ToString(), a.CreatedAt, a.RespondedAt);
    }

    public async Task<MerchantSnippetResponse> GetSnippetAsync(string clerkUserId, string apiBaseUrl)
    {
        var merchant = await db.Merchants
            .FirstOrDefaultAsync(m => m.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a merchant account.");

        return new MerchantSnippetResponse(merchant.ApiKey, Snippet1, BuildSnippet2(merchant.ApiKey, apiBaseUrl));
    }

    // Snippet 1: paste on every page — captures the ?ref= URL param into a first-party cookie.
    // No API key needed; safe to expose publicly.
    private static readonly string Snippet1 = """
        <script>
        (function () {
          var p = new URLSearchParams(window.location.search).get('ref');
          if (!p) return;
          document.cookie = '_nn_ref=' + encodeURIComponent(p) + '; path=/; max-age=2592000; SameSite=Lax';
        })();
        </script>
        """;

    // Snippet 2: paste only on the order confirmation page — reads the cookie and reports the conversion.
    private static string BuildSnippet2(string apiKey, string apiBaseUrl) => $$"""
        <script>
        (function () {
          var KEY = '{{apiKey}}';
          var API = '{{apiBaseUrl}}';
          var m = document.cookie.match('(^|;)\\s*_nn_ref\\s*=\\s*([^;]+)');
          if (!m) return;
          var ref = decodeURIComponent(m[2]);

          /* Replace ORDER_ID and AMOUNT with your actual order values */
          fetch(API + '/api/conversions/track', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'X-Merchant-Key': KEY },
            body: JSON.stringify({ ref: ref, orderId: 'ORDER_ID', amount: 0.00, currency: 'GEL' })
          });
        })();
        </script>
        """;

    // --- AI product-photo generation quota ---

    private const int DefaultAiImageGenerationMonthlyLimit = 40;

    private int AiImageGenerationMonthlyLimit =>
        configuration.GetValue("AiImageGeneration:MonthlyLimit", DefaultAiImageGenerationMonthlyLimit);

    // Resets the counter in-memory (caller still needs to SaveChangesAsync) whenever the
    // stored period no longer matches the current calendar month.
    private void ResetAiImageUsageIfNewPeriod(Merchant merchant, string currentPeriod)
    {
        if (merchant.AiImageGenerationsPeriod == currentPeriod) return;
        merchant.AiImageGenerationsPeriod = currentPeriod;
        merchant.AiImageGenerationsUsed = 0;
    }

    public async Task<AiImageUsageResponse> GetAiImageUsageAsync(string clerkUserId)
    {
        var merchant = await db.Merchants.FirstOrDefaultAsync(m => m.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a merchant account.");

        var currentPeriod = DateTimeOffset.UtcNow.ToString("yyyy-MM");
        var used = merchant.AiImageGenerationsPeriod == currentPeriod ? merchant.AiImageGenerationsUsed : 0;
        return new AiImageUsageResponse(used, AiImageGenerationMonthlyLimit, currentPeriod);
    }

    // Atomically checks-and-increments — called by the frontend right before it pays for an
    // actual Gemini generation, so a merchant can never generate past their monthly cap.
    public async Task<AiImageUsageResponse> ConsumeAiImageGenerationAsync(string clerkUserId)
    {
        var merchant = await db.Merchants.FirstOrDefaultAsync(m => m.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a merchant account.");

        var currentPeriod = DateTimeOffset.UtcNow.ToString("yyyy-MM");
        ResetAiImageUsageIfNewPeriod(merchant, currentPeriod);

        var limit = AiImageGenerationMonthlyLimit;
        if (merchant.AiImageGenerationsUsed >= limit)
            throw new QuotaExceededException($"You've used all {limit} AI photo generations for this month.");

        merchant.AiImageGenerationsUsed++;
        await db.SaveChangesAsync();

        return new AiImageUsageResponse(merchant.AiImageGenerationsUsed, limit, currentPeriod);
    }

    public async Task<MerchantDashboardResponse> GetDashboardAsync(
        string clerkUserId,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        var merchant = await db.Merchants
            .FirstOrDefaultAsync(m => m.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a merchant account.");

        var clicksQuery = db.Clicks.Where(c => c.MerchantId == merchant.Id);
        var conversionsQuery = db.Conversions.Where(c => c.MerchantId == merchant.Id);

        if (from.HasValue)
        {
            clicksQuery = clicksQuery.Where(c => c.ClickedAt >= from.Value);
            conversionsQuery = conversionsQuery.Where(c => c.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            clicksQuery = clicksQuery.Where(c => c.ClickedAt <= to.Value);
            conversionsQuery = conversionsQuery.Where(c => c.CreatedAt <= to.Value);
        }

        var totalClicks = await clicksQuery.CountAsync();
        var totalConversions = await conversionsQuery.CountAsync();

        var totalOwed = await conversionsQuery
            .Where(c => c.Status == ConversionStatus.Confirmed || c.Status == ConversionStatus.Paid)
            .SumAsync(c => (decimal?)(c.CommissionAmount + c.MerchantFeeAmount)) ?? 0m;

        var topCreatorStats = await conversionsQuery
            .GroupBy(c => c.CreatorId)
            .Select(g => new
            {
                CreatorId = g.Key,
                Conversions = g.Count(),
                TotalOwed = g.Sum(c => c.CommissionAmount + c.MerchantFeeAmount),
            })
            .OrderByDescending(x => x.TotalOwed)
            .Take(5)
            .ToListAsync();

        var creatorIds = topCreatorStats.Select(x => x.CreatorId).ToList();

        var creators = await db.Creators
            .Where(c => creatorIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id);

        var clicksByCreator = await clicksQuery
            .Where(c => creatorIds.Contains(c.CreatorId))
            .GroupBy(c => c.CreatorId)
            .Select(g => new { CreatorId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CreatorId, x => x.Count);

        var topCreators = topCreatorStats
            .Where(x => creators.ContainsKey(x.CreatorId))
            .Select(x => new TopCreatorEntry(
                x.CreatorId,
                creators[x.CreatorId].Name,
                creators[x.CreatorId].Slug,
                clicksByCreator.GetValueOrDefault(x.CreatorId, 0),
                x.Conversions,
                x.TotalOwed
            ))
            .ToList();

        return new MerchantDashboardResponse(
            totalClicks,
            totalConversions,
            totalOwed,
            topCreators,
            from,
            to
        );
    }
}
