using HomeSphere.Application.Features.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace HomeSphere.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
