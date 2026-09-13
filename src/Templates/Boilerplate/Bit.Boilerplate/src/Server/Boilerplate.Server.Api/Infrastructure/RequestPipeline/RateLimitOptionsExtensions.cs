//+:cnd:noEmit
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Boilerplate.Server.Api.Infrastructure.RequestPipeline;

public static class RateLimitOptionsExtensions
{
    /// <summary>
    /// Applied with <c>[EnableRateLimiting(RateLimitOptionsExtensions.IDENTITY)]</c> to the anonymous credential
    /// endpoints - the ones that verify a secret or send a message to a stranger.
    /// </summary>
    public const string IDENTITY = "identity";

    //#if (signalR == true)
    /// <summary>
    /// Applied with <c>[EnableRateLimiting(RateLimitOptionsExtensions.SPEECH)]</c> to the two endpoints of the AI chat
    /// panel that speak and listen: transcribing a recording and synthesising an answer. Only those - a chat message
    /// and an attached image cost this app's own resources, while these two spend money at a third party on nothing
    /// more than a button press, so the window is far tighter than <see cref="IDENTITY"/>'s. It partitions per
    /// signed-in user rather than per ip, since both are authenticated and a shared nat would otherwise throttle a
    /// whole office as one caller.
    /// </summary>
    public const string SPEECH = "speech";
    //#endif

    extension(RateLimiterOptions options)
    {
        /// <summary>
        /// Registers every rate limit policy this app defines, the global limiter that backs them and the rejection
        /// status code they all answer with.
        /// </summary>
        public void AddAppRateLimitPolicies()
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(IDENTITY, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            //#if (signalR == true)
            // These endpoints are authenticated, so a user id is what a caller really is here. The ip fallback in
            // GetPartitionKey only exists for the request that arrives before authorization has rejected it.
            options.AddPolicy(SPEECH, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            // An endpoint is matched to a single policy, so the per-ip limit that holds alongside the per-user window
            // above goes on the global limiter instead, keyed off that same policy name - every other request passes
            // straight through, and the attribute stays the one place that says an endpoint is throttled. It is an
            // order of magnitude looser on purpose: an office behind one nat is a single address, so it must only
            // bite at volume no room of people could produce. What it catches is a hundred accounts driven from one
            // host, which a per-user limit is blind to.
            //
            // Both counters live in this process, so on more than one instance each limit is per instance. Neither
            // bounds the total bill either - that is a spend limit on the speech api keys, outside this app.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                IsSpeechEndpoint(context) is false
                    ? RateLimitPartition.GetNoLimiter(string.Empty)
                    : RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: $"ip:{context.Connection.RemoteIpAddress}",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1)
                        }));

            static bool IsSpeechEndpoint(HttpContext context)
                => context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName is SPEECH;
            //#endif
        }
    }

    /// <summary>
    /// A signed-in caller is counted as that user, so neither a shared nat nor a changing ip decides what they are
    /// allowed. Anyone else is counted as the address the request arrived from, which is all a stranger really is.
    /// </summary>
    private static string GetPartitionKey(HttpContext context)
        => context.User.IsAuthenticated()
            ? $"user:{context.User.GetUserId()}"
            : $"ip:{context.Connection.RemoteIpAddress}";
}
