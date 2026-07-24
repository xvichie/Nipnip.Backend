using Microsoft.Extensions.DependencyInjection;

namespace NipNip.Modules.Merchants.Extensions;

public static class MerchantServiceExtensions
{
    public static IServiceCollection AddMerchantModule(this IServiceCollection services)
    {
        services.AddScoped<MerchantService>();
        services.AddScoped<FacebookImportService>();
        services.AddScoped<WebsiteInquiryService>();
        return services;
    }
}
