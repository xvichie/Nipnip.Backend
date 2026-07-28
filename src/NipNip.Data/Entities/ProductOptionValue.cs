namespace NipNip.Data.Entities;

public class ProductOptionValue
{
    public Guid Id { get; set; }
    public Guid ProductOptionId { get; set; }
    // Canonical value — unchanged behavior: used for filter/facet matching and variant
    // linkage semantics. The 3 fields below are pure display overlays, entirely optional,
    // and never affect matching/filtering.
    public string Value { get; set; } = "";
    public string? ValueKa { get; set; }
    public string? ValueEn { get; set; }
    public string? ValueRu { get; set; }

    public ProductOption ProductOption { get; set; } = null!;
}
