using HomeSphere.Application.Features.Addresses;
using HomeSphere.Application.Features.Auth;
using HomeSphere.Application.Features.Users;
using Microsoft.Extensions.DependencyInjection;

namespace HomeSphere.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IAddressService, AddressService>();
        return services;
    }
}
