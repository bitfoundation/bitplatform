using System.Threading.RateLimiting;

namespace Boilerplate.Server.Api.Features.Chatbot;

/// <summary>
/// How many questions the AI chat panel answers a caller a minute (See AppChatbot.Refusal). Each answer is a stream of its
/// own, which ends with it, so the questions are all there is to count.
/// </summary>
public sealed class ChatbotRateLimiter : IDisposable
{
    public const int MessagesPerMinute = 20;

    private readonly PartitionedRateLimiter<HttpContext> messages = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: RateLimitOptionsExtensions.GetPartitionKey(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = MessagesPerMinute,
                Window = TimeSpan.FromMinutes(1)
            }));

    /// <summary>Counts one more question of the caller's, unless it's one too many this minute.</summary>
    /// <param name="retryAfter">When refused, how long until the caller's next question is answered, if the limiter knows.</param>
    public bool TryProcessMessage(HttpContext context, out TimeSpan? retryAfter)
    {
        using var lease = messages.AttemptAcquire(context);

        retryAfter = lease.IsAcquired is false && lease.TryGetMetadata(MetadataName.RetryAfter, out var wait) ? wait : null;

        return lease.IsAcquired;
    }

    public void Dispose() => messages.Dispose();
}
