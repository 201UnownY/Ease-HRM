using Ease_HRM.Application;
using Ease_HRM.Infrastructure;

namespace Ease_HRM.Api
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddAppDI(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddApplicationDI()
                .AddInfrastructureDI(configuration);
            return services;
        }
    }
}
