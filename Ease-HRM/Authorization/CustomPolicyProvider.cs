using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Ease_HRM.Api.Authorization;

public class CustomPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public CustomPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var existingPolicy = await base.GetPolicyAsync(policyName);
        if (existingPolicy is not null)
        {
            return existingPolicy;
        }

        return new AuthorizationPolicyBuilder()
            .AddRequirements(new PermissionRequirement(policyName))
            .Build();
    }
}