using Microsoft.Extensions.DependencyInjection;

namespace NipNip.Modules.Merchants.Extensions;

public static class MerchantServiceExtensions
{
    public static IServiceCollection AddMerchantModule(this IServiceCollection services)
    {
        services.AddScoped<MerchantService>();
        return services;
    }
}
