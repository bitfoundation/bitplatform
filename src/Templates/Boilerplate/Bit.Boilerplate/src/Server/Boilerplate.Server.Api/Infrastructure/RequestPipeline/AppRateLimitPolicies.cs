namespace Boilerplate.Server.Api.Infrastructure.RequestPipeline;

public static class AppRateLimitPolicies
{
    /// <summary>
    /// Applied with <c>[EnableRateLimiting(AppRateLimitPolicies.IDENTITY)]</c> to the anonymous credential
    /// endpoints - the ones that verify a secret or send a message to a stranger.
    /// </summary>
    public const string IDENTITY = "identity";
}
