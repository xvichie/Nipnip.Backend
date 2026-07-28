namespace NipNip.Modules.Storefronts.DTOs;

/// <summary>Value is the canonical string used for filter/facet matching and never changes via
/// translation edits; ValueKa/ValueEn/ValueRu are optional display-only overlays.</summary>
public record ProductOptionValueResponse(Guid Id, string Value, string? ValueKa, string? ValueEn, string? ValueRu);

/// <summary>Name is the resolved ka->en->ru fallback.</summary>
public record ProductOptionResponse(Guid Id, string Name, string? NameKa, string? NameEn, string? NameRu, List<ProductOptionValueResponse> Values);

/// <summary>At least one of NameKa/NameEn/NameRu is required (validated in ProductOptionService).</summary>
public record CreateProductOptionRequest(string? NameKa, string? NameEn, string? NameRu);

/// <summary>Unconditionally overwrites all three name fields, same convention as categories/products.</summary>
public record UpdateProductOptionRequest(string? NameKa, string? NameEn, string? NameRu);

public record CreateProductOptionValueRequest(string Value, string? ValueKa = null, string? ValueEn = null, string? ValueRu = null);

/// <summary>Translation-only update — the canonical Value is immutable once created (it's what
/// filters/facets and existing selections key off), so this never touches it.</summary>
public record UpdateProductOptionValueRequest(string? ValueKa, string? ValueEn, string? ValueRu);
