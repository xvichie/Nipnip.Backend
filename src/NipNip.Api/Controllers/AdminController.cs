using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Modules.Creators;
using NipNip.Modules.Creators.DTOs;
using NipNip.Modules.Merchants;
using NipNip.Modules.Merchants.DTOs;
using NipNip.Modules.Merchants.Extensions;
using NipNip.Modules.Payouts;
using NipNip.Modules.Payouts.DTOs;
using NipNip.Modules.Storefronts;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Modules.Tracking;
using NipNip.Modules.Tracking.DTOs;
using NipNip.Shared.Exceptions;
using NipNip.Shared.Pagination;

namespace NipNip.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminController(
    MerchantService merchantService,
    CreatorService creatorService,
    TrackingService trackingService,
    PayoutService payoutService,
    StoreService storeService,
    ProductService productService,
    CategoryService categoryService,
    ProductImageService productImageService,
    AppDbContext db) : ControllerBase
{
    // --- Stats ---

    [HttpGet("stats")]
    public async Task<ActionResult<AdminStatsResponse>> GetStats()
    {
        var totalMerchants = await db.Merchants.CountAsync();
        var totalCreators = await db.Creators.CountAsync();
        var totalConversions = await db.Conversions.CountAsync();
        var totalCommissionVolume = await db.Conversions
            .SumAsync(c => (decimal?)c.CommissionAmount) ?? 0m;
        var totalCreatorFees = await db.Conversions
            .SumAsync(c => (decimal?)c.CreatorFeeAmount) ?? 0m;
        var totalMerchantFees = await db.Conversions
            .SumAsync(c => (decimal?)c.MerchantFeeAmount) ?? 0m;
        var pendingPayouts = await db.Conversions
            .Where(c => c.Status == NipNip.Data.Enums.ConversionStatus.Confirmed)
            .SumAsync(c => (decimal?)c.CreatorEarnings) ?? 0m;

        return Ok(new AdminStatsResponse(
            totalMerchants,
            totalCreators,
            totalConversions,
            totalCommissionVolume,
            totalCreatorFees + totalMerchantFees,
            pendingPayouts));
    }

    // --- Payout Summary ---

    [HttpGet("payout-summary")]
    public async Task<ActionResult<AdminPayoutSummaryResponse>> GetPayoutSummary(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null)
    {
        var query = db.Conversions
            .Where(c => c.Status == NipNip.Data.Enums.ConversionStatus.Confirmed)
            .AsQueryable();

        if (from.HasValue) query = query.Where(c => c.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(c => c.CreatedAt <= to.Value);

        var totalOwedToCreators = await query.SumAsync(c => (decimal?)c.CreatorEarnings) ?? 0m;

        var byCreator = await query
            .GroupBy(c => c.CreatorId)
            .Select(g => new
            {
                CreatorId = g.Key,
                Conversions = g.Count(),
                TotalEarnings = g.Sum(c => c.CreatorEarnings),
                Currency = g.Select(c => c.Currency).FirstOrDefault() ?? "GEL",
            })
            .OrderByDescending(x => x.TotalEarnings)
            .ToListAsync();

        var creatorIds = byCreator.Select(x => x.CreatorId).ToList();
        var creators = await db.Creators
            .Where(c => creatorIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id);

        var entries = byCreator
            .Where(x => creators.ContainsKey(x.CreatorId))
            .Select(x => new CreatorPayoutEntry(
                x.CreatorId,
                creators[x.CreatorId].Name,
                creators[x.CreatorId].Slug,
                x.Conversions,
                x.TotalEarnings,
                x.Currency))
            .ToList();

        return Ok(new AdminPayoutSummaryResponse(totalOwedToCreators, entries));
    }

    // --- Merchants ---

    [HttpPost("merchants")]
    public async Task<ActionResult<MerchantResponse>> CreateMerchant(
        [FromBody] AdminCreateMerchantRequest request)
    {
        var merchant = await merchantService.RegisterAsync(
            request.ClerkUserId,
            new RegisterMerchantRequest(
                request.Name,
                request.Slug,
                request.CommissionPercent,
                request.WebsiteUrl,
                request.InstagramHandle,
                request.Description,
                request.LogoUrl));

        return Ok(merchant);
    }

    [HttpGet("merchants")]
    public async Task<ActionResult<PaginatedResult<MerchantResponse>>> GetAllMerchants(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        return Ok(await merchantService.GetAllAdminAsync(page, pageSize));
    }

    [HttpGet("merchants/{id:guid}")]
    public async Task<ActionResult<MerchantResponse>> GetMerchant(Guid id)
    {
        var merchant = await db.Merchants.FindAsync(id)
            ?? throw new NotFoundException("Merchant not found.");
        return Ok(merchant.ToDto());
    }

    [HttpPut("merchants/{id:guid}")]
    public async Task<ActionResult<MerchantResponse>> UpdateMerchant(
        Guid id, [FromBody] UpdateMerchantRequest request)
    {
        return Ok(await merchantService.UpdateAdminAsync(id, request));
    }

    [HttpDelete("merchants/{id:guid}")]
    public async Task<ActionResult<MerchantResponse>> DeactivateMerchant(Guid id)
    {
        return Ok(await merchantService.DeactivateAsync(id));
    }

    [HttpPut("merchants/{id:guid}/highlight")]
    public async Task<ActionResult<MerchantResponse>> ToggleMerchantHighlight(Guid id)
    {
        return Ok(await merchantService.ToggleHighlightAsync(id));
    }

    [HttpPut("merchants/{id:guid}/test-flag")]
    public async Task<ActionResult<MerchantResponse>> ToggleMerchantTest(Guid id)
    {
        return Ok(await merchantService.ToggleTestAsync(id));
    }

    [HttpGet("merchants/{merchantId:guid}/store")]
    public async Task<ActionResult<StoreResponse?>> GetMerchantStore(Guid merchantId)
    {
        return Ok(await storeService.GetByMerchantIdAdminAsync(merchantId));
    }

    [HttpPost("merchants/{merchantId:guid}/store")]
    public async Task<ActionResult<StoreResponse>> CreateMerchantStore(
        Guid merchantId, [FromBody] CreateStoreRequest request)
    {
        return Ok(await storeService.CreateAdminAsync(merchantId, request));
    }

    [HttpPut("merchants/{merchantId:guid}/store/theme-override")]
    public async Task<ActionResult<StoreResponse>> SetStoreThemeOverride(
        Guid merchantId, [FromBody] SetStoreThemeOverrideRequest request)
    {
        return Ok(await storeService.SetThemeOverrideAdminAsync(merchantId, request));
    }

    [HttpPut("merchants/{merchantId:guid}/store")]
    public async Task<ActionResult<StoreResponse>> UpdateMerchantStore(
        Guid merchantId, [FromBody] UpdateStoreRequest request)
    {
        return Ok(await storeService.UpdateAdminAsync(merchantId, request));
    }

    // --- Prospects (admin-built sales-demo stores — see Merchant.IsProspect) ---

    [HttpPost("prospects")]
    public async Task<ActionResult<ProspectResponse>> CreateProspect([FromBody] CreateProspectRequest request)
    {
        var merchant = await merchantService.CreateProspectMerchantAsync(request.Name, request.Slug);
        var store = await storeService.CreateAdminAsync(
            merchant.Id,
            new CreateStoreRequest(request.Slug, request.Name, ThemeId: "minimal", ThemeConfig: null));

        return Ok(new ProspectResponse(merchant.ToDto(), store));
    }

    [HttpGet("prospects")]
    public async Task<ActionResult<PaginatedResult<MerchantResponse>>> GetAllProspects(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        return Ok(await merchantService.GetAllProspectsAdminAsync(page, pageSize));
    }

    [HttpPut("merchants/{id:guid}/promote")]
    public async Task<ActionResult<MerchantResponse>> PromoteProspect(Guid id, [FromBody] PromoteProspectRequest request)
    {
        return Ok(await merchantService.PromoteProspectAsync(id, request.ClerkUserId));
    }

    // --- Prospect products/categories (admin manages these on the prospect's behalf, since a
    // prospect has no real Clerk account of its own to sign in and use the normal merchant
    // dashboard with) ---

    [HttpGet("merchants/{merchantId:guid}/products")]
    public async Task<ActionResult<PaginatedResult<ProductSummaryResponse>>> GetMerchantProducts(
        Guid merchantId, [FromQuery] PaginatedRequest pagination)
    {
        return Ok(await productService.GetAllAdminForMerchantAsync(merchantId, pagination));
    }

    [HttpPost("merchants/{merchantId:guid}/products")]
    public async Task<ActionResult<ProductDetailResponse>> CreateMerchantProduct(
        Guid merchantId, [FromBody] CreateProductRequest request)
    {
        return Ok(await productService.CreateAdminAsync(merchantId, request));
    }

    [HttpPut("merchants/{merchantId:guid}/products/{productId:guid}")]
    public async Task<ActionResult<ProductDetailResponse>> UpdateMerchantProduct(
        Guid merchantId, Guid productId, [FromBody] UpdateProductRequest request)
    {
        return Ok(await productService.UpdateAdminAsync(merchantId, productId, request));
    }

    [HttpDelete("merchants/{merchantId:guid}/products/{productId:guid}")]
    public async Task<IActionResult> DeleteMerchantProduct(Guid merchantId, Guid productId)
    {
        await productService.DeleteAdminAsync(merchantId, productId);
        return NoContent();
    }

    [HttpPost("merchants/{merchantId:guid}/products/{productId:guid}/images")]
    public async Task<ActionResult<ProductImageResponse>> AddMerchantProductImage(
        Guid merchantId, Guid productId, [FromBody] CreateProductImageRequest request)
    {
        return Ok(await productImageService.CreateAdminAsync(merchantId, productId, request));
    }

    [HttpDelete("merchants/{merchantId:guid}/products/{productId:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> DeleteMerchantProductImage(Guid merchantId, Guid productId, Guid imageId)
    {
        await productImageService.DeleteAdminAsync(merchantId, productId, imageId);
        return NoContent();
    }

    [HttpGet("merchants/{merchantId:guid}/categories")]
    public async Task<ActionResult<List<CategoryResponse>>> GetMerchantCategories(Guid merchantId)
    {
        return Ok(await categoryService.GetAllAdminForMerchantAsync(merchantId));
    }

    [HttpPost("merchants/{merchantId:guid}/categories")]
    public async Task<ActionResult<CategoryResponse>> CreateMerchantCategory(
        Guid merchantId, [FromBody] CreateCategoryRequest request)
    {
        return Ok(await categoryService.CreateAdminAsync(merchantId, request));
    }

    [HttpPut("merchants/{merchantId:guid}/categories/{categoryId:guid}")]
    public async Task<ActionResult<CategoryResponse>> UpdateMerchantCategory(
        Guid merchantId, Guid categoryId, [FromBody] UpdateCategoryRequest request)
    {
        return Ok(await categoryService.UpdateAdminAsync(merchantId, categoryId, request));
    }

    [HttpDelete("merchants/{merchantId:guid}/categories/{categoryId:guid}")]
    public async Task<IActionResult> DeleteMerchantCategory(Guid merchantId, Guid categoryId)
    {
        await categoryService.DeleteAdminAsync(merchantId, categoryId);
        return NoContent();
    }

    // --- Creators ---

    [HttpGet("creators")]
    public async Task<ActionResult<PaginatedResult<CreatorResponse>>> GetAllCreators(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        return Ok(await creatorService.GetAllAdminAsync(page, pageSize));
    }

    [HttpPut("creators/{id:guid}/highlight")]
    public async Task<ActionResult<CreatorResponse>> ToggleCreatorHighlight(Guid id)
    {
        return Ok(await creatorService.ToggleHighlightAsync(id));
    }

    [HttpPut("creators/{id:guid}/test-flag")]
    public async Task<ActionResult<CreatorResponse>> ToggleCreatorTest(Guid id)
    {
        return Ok(await creatorService.ToggleTestAsync(id));
    }

    [HttpDelete("creators/{id:guid}")]
    public async Task<ActionResult<CreatorResponse>> DeactivateCreator(Guid id)
    {
        return Ok(await creatorService.DeactivateAsync(id));
    }

    // --- Conversions ---

    [HttpGet("conversions")]
    public async Task<ActionResult<PaginatedResult<AdminConversionEntry>>> GetAllConversions(
        [FromQuery] Guid? merchantId = null,
        [FromQuery] Guid? creatorId = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        return Ok(await trackingService.GetAllConversionsAdminAsync(
            merchantId, creatorId, from, to, page, pageSize));
    }

    // --- Payouts ---

    [HttpGet("payouts")]
    public async Task<ActionResult<PaginatedResult<PayoutResponse>>> GetAllPayouts(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        return Ok(await payoutService.GetAllPayoutsAdminAsync(status, page, pageSize));
    }

    [HttpPut("payouts/{id:guid}/mark-sent")]
    public async Task<ActionResult<PayoutResponse>> MarkPayoutSent(
        Guid id, [FromBody] MarkPayoutSentRequest request)
    {
        return Ok(await payoutService.MarkPayoutSentAsync(id, request));
    }

    [HttpPut("payouts/{id:guid}/reject")]
    public async Task<ActionResult<PayoutResponse>> RejectPayout(
        Guid id, [FromQuery] string? notes = null)
    {
        return Ok(await payoutService.RejectPayoutAsync(id, notes));
    }
}

public record AdminStatsResponse(
    int TotalMerchants,
    int TotalCreators,
    int TotalConversions,
    decimal TotalCommissionVolume,
    decimal TotalPlatformEarnings,
    decimal PendingCreatorPayouts
);

public record AdminPayoutSummaryResponse(
    decimal TotalOwedToCreators,
    IReadOnlyList<CreatorPayoutEntry> Creators
);

public record CreatorPayoutEntry(
    Guid CreatorId,
    string CreatorName,
    string CreatorSlug,
    int Conversions,
    decimal TotalEarnings,
    string Currency
);

public record AdminCreateMerchantRequest(
    string ClerkUserId,
    string Name,
    string Slug,
    decimal CommissionPercent,
    string? WebsiteUrl,
    string? InstagramHandle,
    string? Description,
    string? LogoUrl
);

public record CreateProspectRequest(string Name, string Slug);

public record ProspectResponse(MerchantResponse Merchant, StoreResponse Store);

/// <summary>Omit ClerkUserId to just unflag the prospect; pass it to also hand ownership to the real customer's Clerk account.</summary>
public record PromoteProspectRequest(string? ClerkUserId);
