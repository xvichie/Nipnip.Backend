using Microsoft.Extensions.DependencyInjection;

namespace NipNip.Modules.Storefronts.Extensions;

public static class StorefrontServiceExtensions
{
    public static IServiceCollection AddStorefrontModule(this IServiceCollection services)
    {
        services.AddOptions<VercelOptions>().BindConfiguration("Vercel");
        services.AddScoped<VercelDomainService>();
        services.AddScoped<StoreService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<StorePageService>();
        services.AddScoped<ContactMessageService>();
        services.AddScoped<ProductService>();
        services.AddScoped<ProductOptionService>();
        services.AddScoped<ProductVariantService>();
        services.AddScoped<ProductImageService>();
        services.AddScoped<CartService>();
        services.AddScoped<OrderService>();
        return services;
    }
}
