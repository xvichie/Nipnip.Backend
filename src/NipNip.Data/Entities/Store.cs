namespace NipNip.Data.Entities;

public class Store
{
    public Guid Id { get; set; }
    public Guid MerchantId { get; set; }
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string ThemeId { get; set; } = "default";
    public string ThemeConfig { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
    public string? CustomDomain { get; set; }
    public DateTimeOffset? CustomDomainVerifiedAt { get; set; }
    public bool AffiliateEnabled { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; }

    public Merchant Merchant { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
    public ICollection<Cart> Carts { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
}
