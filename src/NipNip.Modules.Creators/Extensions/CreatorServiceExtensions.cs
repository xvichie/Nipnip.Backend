using Microsoft.Extensions.DependencyInjection;

namespace NipNip.Modules.Creators.Extensions;

public static class CreatorServiceExtensions
{
    public static IServiceCollection AddCreatorModule(this IServiceCollection services)
    {
        services.AddScoped<CreatorService>();
        services.AddScoped<LinkTreeService>();
        return services;
    }
}
