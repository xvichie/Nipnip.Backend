using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Extensions;
using NipNip.Shared.Pagination;

namespace NipNip.Modules.Storefronts;

[ApiController]
[Route("api/stores/me/products")]
[Authorize]
public class ProductController(ProductService productService) : ControllerBase
{
    [HttpGet("/api/stores/{slug}/products")]
    [AllowAnonymous]
    [EnableCors("Public")]
    public async Task<ActionResult<PaginatedResult<ProductSummaryResponse>>> GetAllPublic(
        string slug,
        [FromQuery] PaginatedRequest pagination,
        [FromQuery] string? categorySlug,
        [FromQuery] string? collectionSlug,
        [FromQuery] string? search,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDir,
        [FromQuery] string? optionFilters)
    {
        return Ok(await productService.GetAllForStoreSlugAsync(slug, pagination, categorySlug, collectionSlug, search, minPrice, maxPrice, sortBy, sortDir, optionFilters));
    }

    [HttpGet("/api/stores/{slug}/products/price-range")]
    [AllowAnonymous]
    [EnableCors("Public")]
    public async Task<ActionResult<ProductPriceRangeResponse>> GetPriceRangePublic(string slug, [FromQuery] string? categorySlug)
    {
        return Ok(await productService.GetPriceRangeForStoreSlugAsync(slug, categorySlug));
    }

    [HttpGet("/api/stores/{slug}/products/facets")]
    [AllowAnonymous]
    [EnableCors("Public")]
    public async Task<ActionResult<List<ProductFacetResponse>>> GetFacetsPublic(string slug, [FromQuery] string? categorySlug)
    {
        return Ok(await productService.GetFacetsForStoreSlugAsync(slug, categorySlug));
    }

    [HttpGet("/api/stores/{slug}/products/{productSlug}")]
    [AllowAnonymous]
    [EnableCors("Public")]
    public async Task<ActionResult<ProductDetailResponse>> GetBySlugPublic(string slug, string productSlug)
    {
        return Ok(await productService.GetBySlugForStoreSlugAsync(slug, productSlug));
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<ProductSummaryResponse>>> GetAll(
        [FromQuery] PaginatedRequest pagination,
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? collectionId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await productService.GetAllForOwnStoreAsync(clerkUserId, pagination, search, categoryId, collectionId, isActive, sortBy, sortDir));
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportCsv()
    {
        var clerkUserId = User.GetClerkUserId();
        var csv = await productService.ExportCsvAsync(clerkUserId);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "products.csv");
    }

    [HttpPost("import")]
    public async Task<ActionResult<ProductImportResult>> ImportCsv(IFormFile file)
    {
        var clerkUserId = User.GetClerkUserId();
        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync();
        return Ok(await productService.ImportCsvAsync(clerkUserId, content));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDetailResponse>> GetById(Guid id)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await productService.GetOwnByIdAsync(clerkUserId, id));
    }

    [HttpPost]
    public async Task<ActionResult<ProductDetailResponse>> Create([FromBody] CreateProductRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        var product = await productService.CreateAsync(clerkUserId, request);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductDetailResponse>> Update(Guid id, [FromBody] UpdateProductRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await productService.UpdateAsync(clerkUserId, id, request));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var clerkUserId = User.GetClerkUserId();
        await productService.DeleteAsync(clerkUserId, id);
        return NoContent();
    }

    [HttpPost("{id:guid}/duplicate")]
    public async Task<ActionResult<ProductDetailResponse>> Duplicate(Guid id)
    {
        var clerkUserId = User.GetClerkUserId();
        var duplicate = await productService.DuplicateAsync(clerkUserId, id);
        return CreatedAtAction(nameof(GetById), new { id = duplicate.Id }, duplicate);
    }
}
