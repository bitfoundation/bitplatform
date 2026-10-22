namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Errors;

public partial class AiErrorsScenarioPage
{
    private readonly string example1RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    <BitAiChat Session="session"
               EmptyTitle="Send anything"
               EmptyBody="The first request fails on purpose. Press Try again."
               Suggestions="suggestions" />
</div>
""";

    private readonly string example1CsharpCode = """
private readonly BitAiChatSession session = new(AiDemoInteractiveTransports.FailsThenRecovers());

private readonly string[] suggestions = ["Summarise the release notes"];

public ValueTask DisposeAsync() => session.DisposeAsync();
""";

    private const string example1TransportCode = """
public static IBitAiTransport FailsThenRecovers()
{
    var attempts = 0;

    return new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
    {
        await yielder(new BitAiMessageStartedEvent());

        if (Interlocked.Increment(ref attempts) == 1)
        {
            // Some of the answer streams, then the request fails - and what streamed is kept.
            await foreach (var evt in AiScript.Write("Looking that up", 4, 25, token)) await yielder(evt);

            await yielder(new BitAiErrorEvent(new BitAiError("The model is rate limited right now.")
            {
                Code = "rate_limit",
                Kind = BitAiErrorKind.RateLimit,
                IsRetryable = true,
                RetryAfter = TimeSpan.FromSeconds(3),
                Detail = "HTTP 429\nx-ratelimit-remaining: 0\nretry-after: 3",
            }));

            return;
        }

        await foreach (var evt in AiScript.Write("That worked on the second attempt.", 4, 12, token)) await yielder(evt);
        await yielder(new BitAiMessageCompletedEvent());
    }));
}
""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("AiDemoInteractiveTransports.cs", example1TransportCode),
    ];
}
