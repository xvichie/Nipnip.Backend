namespace NipNip.Data.Entities;

// A merchant-defined grouping of products (e.g. "საზაფხულო", "ორიგინალი") shown as its own
// horizontally-scrolling row on the storefront home page. Unlike Category, a product can belong
// to any number of collections at once — see ProductCollection.
public class Collection
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";

    public Store Store { get; set; } = null!;
    public ICollection<ProductCollection> ProductCollections { get; set; } = [];
}
