namespace NipNip.Data.Entities;

public class Category
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? IconUrl { get; set; }
    public string? IconKey { get; set; }
    public string? IconEmoji { get; set; }

    public Store Store { get; set; } = null!;
    public Category? ParentCategory { get; set; }
    public ICollection<Category> ChildCategories { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
}
