using Microsoft.Extensions.DependencyInjection;

namespace NipNip.Modules.Codes.Extensions;

public static class CodeServiceExtensions
{
    public static IServiceCollection AddCodeModule(this IServiceCollection services)
    {
        services.AddScoped<CodeService>();
        return services;
    }
}
