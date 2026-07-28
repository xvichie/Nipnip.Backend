namespace NipNip.Data.Entities;

public class Category
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public Guid? ParentCategoryId { get; set; }

    // At least one of these three must be set (enforced in CategoryService, not a DB
    // constraint) — a merchant can enter just one language and add the others later.
    public string? NameKa { get; set; }
    public string? NameEn { get; set; }
    public string? NameRu { get; set; }

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
