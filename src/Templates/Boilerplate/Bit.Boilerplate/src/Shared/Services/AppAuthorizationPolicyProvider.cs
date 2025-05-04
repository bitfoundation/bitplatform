using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;

namespace Boilerplate.Shared.Services;

public class AppAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options), IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var result = await base.GetPolicyAsync(policyName);

        if (result is not null)
            return result;

        try
        {
            await _semaphore.WaitAsync();

            result = await base.GetPolicyAsync(policyName); // After lock double-check.

            if (result is not null)
                return result;

            // Consider every missing policy as a claim requirement.
            var claimType = policyName;
            var policy = new AuthorizationPolicyBuilder()
                .RequireAssertion(context => context.User.HasClaim(claimType, ""))
                .Build();

            options.Value.AddPolicy(policyName, policy);

            return policy;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}
