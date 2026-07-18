using Anthropic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NipNip.Modules.Storefronts.AiAgent;
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
        services.AddSingleton(sp => new AesStringProtector(sp.GetRequiredService<IOptions<FacebookOptions>>().Value.TokenEncryptionKey));
        services.AddSingleton(sp => new AnthropicClient { ApiKey = sp.GetRequiredService<IOptions<AnthropicOptions>>().Value.ApiKey });
        services.AddScoped<VercelDomainService>();
        services.AddScoped<GraphApiClient>();
        services.AddScoped<VoyageClient>();
        services.AddScoped<FacebookConnectionService>();
        services.AddScoped<InstagramConnectionService>();
        services.AddScoped<StoreService>();
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
