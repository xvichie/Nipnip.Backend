using Microsoft.Extensions.DependencyInjection;

namespace NipNip.Modules.Tracking.Extensions;

public static class TrackingServiceExtensions
{
    public static IServiceCollection AddTrackingModule(this IServiceCollection services)
    {
        services.AddOptions<PlatformFeesOptions>()
            .BindConfiguration("PlatformFees");
        services.AddScoped<TrackingService>();
        return services;
    }
}
