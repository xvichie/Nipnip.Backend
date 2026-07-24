namespace NipNip.Data.Entities;

// Many-to-many join between Product and Collection. SortOrder is the merchant's curated
// position of this product within this specific collection (mirrors ProductRelation).
public class ProductCollection
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid CollectionId { get; set; }
    public int SortOrder { get; set; }

    public Product Product { get; set; } = null!;
    public Collection Collection { get; set; } = null!;
}
