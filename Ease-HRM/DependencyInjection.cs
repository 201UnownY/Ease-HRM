using Ease_HRM.Api.Authorization;
using Ease_HRM.Api.Services;
using Ease_HRM.Application;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace Ease_HRM.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApp(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication()
            .AddInfrastructure(configuration);

        services.AddMemoryCache();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddScoped<IAuthorizationHandler, PermissionHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, CustomPolicyProvider>();

        return services;
    }
}
