namespace NipNip.Data.Entities;

public class ProductVariantOptionValue
{
    public Guid VariantId { get; set; }
    public Guid OptionValueId { get; set; }

    public ProductVariant Variant { get; set; } = null!;
    public ProductOptionValue OptionValue { get; set; } = null!;
}
