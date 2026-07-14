using Microsoft.EntityFrameworkCore;
using NipNip.Data.Entities;

namespace NipNip.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<Creator> Creators => Set<Creator>();
    public DbSet<Click> Clicks => Set<Click>();
    public DbSet<Conversion> Conversions => Set<Conversion>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<DiscountCode> DiscountCodes => Set<DiscountCode>();

    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductOption> ProductOptions => Set<ProductOption>();
    public DbSet<ProductOptionValue> ProductOptionValues => Set<ProductOptionValue>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductVariantOptionValue> ProductVariantOptionValues => Set<ProductVariantOptionValue>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderNote> OrderNotes => Set<OrderNote>();
    public DbSet<StorePage> StorePages => Set<StorePage>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
