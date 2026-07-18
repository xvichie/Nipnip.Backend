namespace NipNip.Data.Entities;

// A merchant's manual "show this below that product" pick. Directed: a row here only
// affects RelatedProductId's appearance under ProductId's page, not the reverse.
public class ProductRelation
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid RelatedProductId { get; set; }
    public int SortOrder { get; set; }

    public Product Product { get; set; } = null!;
    public Product RelatedProduct { get; set; } = null!;
}
