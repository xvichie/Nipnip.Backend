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

    // Option groups (e.g. [{"name":"Size","values":["S","M","L","XL"]}]) merchants configure
    // once per category, pre-filled onto new products created in that category — purely a
    // creation-time convenience, never enforced or referenced afterward.
    public string DefaultOptions { get; set; } = "[]";

    public Store Store { get; set; } = null!;
    public Category? ParentCategory { get; set; }
    public ICollection<Category> ChildCategories { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
}
