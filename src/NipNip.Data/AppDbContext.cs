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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
