using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ease_HRM.Domain
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDomainDI(this IServiceCollection services)
        {
            return services;
        }
    }
}
