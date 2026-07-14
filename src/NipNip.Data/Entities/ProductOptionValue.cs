namespace NipNip.Data.Entities;

public class ProductOptionValue
{
    public Guid Id { get; set; }
    public Guid ProductOptionId { get; set; }
    public string Value { get; set; } = "";

    public ProductOption ProductOption { get; set; } = null!;
}
