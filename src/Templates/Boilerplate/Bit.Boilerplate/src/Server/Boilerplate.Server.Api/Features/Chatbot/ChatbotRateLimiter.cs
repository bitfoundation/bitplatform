using System.Threading.RateLimiting;

namespace Boilerplate.Server.Api.Features.Chatbot;

public sealed class ChatbotRateLimiter : IDisposable
{
    public const int ChatsPerMinute = 10;

    public const int MessagesPerMinute = 20;

    private readonly PartitionedRateLimiter<HttpContext> chats = CreateLimiter(ChatsPerMinute);
    private readonly PartitionedRateLimiter<HttpContext> messages = CreateLimiter(MessagesPerMinute);

    public bool TryStartChat(HttpContext context) => TryAcquire(chats, context);

    public bool TryProcessMessage(HttpContext context) => TryAcquire(messages, context);

    private static bool TryAcquire(PartitionedRateLimiter<HttpContext> limiter, HttpContext context)
    {
        using var lease = limiter.AttemptAcquire(context);

        return lease.IsAcquired;
    }

    private static PartitionedRateLimiter<HttpContext> CreateLimiter(int permitLimit)
    {
        return PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: RateLimitOptionsExtensions.GetPartitionKey(context),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromMinutes(1)
                }));
    }

    public void Dispose()
    {
        chats.Dispose();
        messages.Dispose();
    }
}
