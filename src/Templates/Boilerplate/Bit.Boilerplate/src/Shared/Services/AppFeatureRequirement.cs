using Microsoft.AspNetCore.Authorization;

namespace Boilerplate.Shared.Services;

public record AppFeatureRequirement(
    string FeatureName,
    string FeatureValue
) : IAuthorizationRequirement;

public class FeatureRequirementHandler : AuthorizationHandler<AppFeatureRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AppFeatureRequirement requirement)
    {
        if (context.User.HasClaim(AppClaimTypes.FEATURES, requirement.FeatureValue) is false)
            return;

        context.Succeed(requirement);
    }
}
