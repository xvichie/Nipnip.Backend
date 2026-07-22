using Anthropic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NipNip.Modules.Storefronts.AiAgent;
using NipNip.Modules.Storefronts.Extra;
using NipNip.Modules.Storefronts.Flitt;
using NipNip.Modules.Storefronts.MyMarket;
using NipNip.Modules.Storefronts.Phubber;
using NipNip.Modules.Storefronts.QuickShipper;
using NipNip.Modules.Storefronts.TikTok;
using NipNip.Shared.Crypto;

namespace NipNip.Modules.Storefronts.Extensions;

public static class StorefrontServiceExtensions
{
    public static IServiceCollection AddStorefrontModule(this IServiceCollection services)
    {
        services.AddOptions<VercelOptions>().BindConfiguration("Vercel");
        services.AddOptions<FacebookOptions>().BindConfiguration("Facebook");
        services.AddOptions<AnthropicOptions>().BindConfiguration("Anthropic");
        services.AddOptions<VoyageOptions>().BindConfiguration("Voyage");
        services.AddOptions<QuickShipperOptions>().BindConfiguration("QuickShipper");
        services.AddOptions<FlittOptions>().BindConfiguration("Flitt");
        services.AddOptions<TikTokOptions>().BindConfiguration("TikTok");
        services.AddSingleton(sp => new AesStringProtector(sp.GetRequiredService<IOptions<FacebookOptions>>().Value.TokenEncryptionKey));
        services.AddSingleton(sp => new AnthropicClient { ApiKey = sp.GetRequiredService<IOptions<AnthropicOptions>>().Value.ApiKey });
        services.AddScoped<VercelDomainService>();
        services.AddScoped<GraphApiClient>();
        services.AddScoped<VoyageClient>();
        services.AddScoped<FacebookConnectionService>();
        services.AddScoped<InstagramConnectionService>();
        services.AddScoped<QuickShipperAuthClient>();
        services.AddScoped<QuickShipperOrderClient>();
        services.AddScoped<QuickShipperService>();
        services.AddScoped<FlittApiClient>();
        services.AddScoped<FlittService>();
        services.AddScoped<MyMarketService>();
        services.AddScoped<PhubberService>();
        services.AddScoped<ExtraService>();
        services.AddScoped<TikTokApiClient>();
        services.AddScoped<TikTokConnectionService>();
        services.AddScoped<StoreService>();
        services.AddScoped<StoreAnalyticsService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<StorePageService>();
        services.AddScoped<ContactMessageService>();
        services.AddScoped<ProductService>();
        services.AddScoped<ProductOptionService>();
        services.AddScoped<RelatedProductService>();
        services.AddScoped<ProductVariantService>();
        services.AddScoped<ProductImageService>();
        services.AddScoped<CartService>();
        services.AddScoped<OrderService>();
        services.AddScoped<AiAgentToolService>();
        services.AddScoped<AiAgentSettingsService>();
        services.AddScoped<ConversationService>();
        services.AddScoped<AiAgentOrchestrator>();
        services.AddScoped<KnowledgeBaseService>();
        return services;
    }
}
