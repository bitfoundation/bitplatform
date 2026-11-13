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

    public bool TryProcessMessage(HttpContext context)
    {
        using var lease = messages.AttemptAcquire(context);

        return lease.IsAcquired;
    }

    public void Dispose() => messages.Dispose();
}
