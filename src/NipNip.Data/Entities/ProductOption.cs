namespace NipNip.Data.Entities;

public class ProductOption
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Name { get; set; } = "";

    public Product Product { get; set; } = null!;
    public ICollection<ProductOptionValue> Values { get; set; } = [];
}
