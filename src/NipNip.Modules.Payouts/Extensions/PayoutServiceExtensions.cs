using Microsoft.Extensions.DependencyInjection;

namespace NipNip.Modules.Payouts.Extensions;

public static class PayoutServiceExtensions
{
    public static IServiceCollection AddPayoutModule(this IServiceCollection services)
    {
        services.AddScoped<PayoutService>();
        return services;
    }
}
