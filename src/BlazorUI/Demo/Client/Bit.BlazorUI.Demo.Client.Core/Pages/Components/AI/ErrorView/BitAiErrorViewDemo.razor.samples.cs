namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ErrorView;

public partial class BitAiErrorViewDemo
{
    private readonly string example1RazorCode = """
<BitAiErrorView Error="filterError" />
""";
    private readonly string example1CsharpCode = """
private readonly BitAiError filterError = new("This request was blocked by the content filter of the provider.")
{
    Code = "content_filter",
    Kind = BitAiErrorKind.ContentFilter,
};
""";

    private readonly string example2RazorCode = """
@implements IAsyncDisposable

<BitButton OnClick="AskAsync" Disabled="retrySession.IsGenerating">Ask</BitButton>
<BitButton Variant="BitVariant.Outline" OnClick="StartOverAsync" Disabled="retrySession.IsGenerating">Start over</BitButton>

@if (retrySession.LastError is { } lastError)
{
    <BitAiErrorView Error="lastError" OnRetry="RetryAsync" />
}
else if (retrySession.Conversation.Messages.LastOrDefault(m => m.Role.IsAssistant) is { } answer)
{
    <div>@answer.GetText()</div>
}
""";
    private readonly string example2CsharpCode = """
private BitAiRenderScheduler sessionRenders = default!;
private BitAiChatSession retrySession = default!;

protected override void OnInitialized()
{
    // At most one render of the page per frame interval, however often the session changes.
    sessionRenders = new BitAiRenderScheduler(() => InvokeAsync(StateHasChanged));
    retrySession = CreateRetrySession();
}

// A scripted model that is rate limited on its first try and answers on the second.
private BitAiChatSession CreateRetrySession()
{
    var session = new BitAiChatSession(AiDemoInteractiveTransports.FailsThenRecovers());
    session.StateChanged += sessionRenders.Request;
    return session;
}

private Task AskAsync() => retrySession.SubmitAsync("Summarise the release notes");

private Task RetryAsync() => retrySession.RetryAsync();

private async Task StartOverAsync()
{
    await retrySession.DisposeAsync();
    retrySession = CreateRetrySession();
}

public async ValueTask DisposeAsync()
{
    await retrySession.DisposeAsync();
    await sessionRenders.DisposeAsync();
}
""";

    private readonly string example3RazorCode = """
<BitAiErrorView Error="detailError" OnRetry="() => detailRetries++" />

<div>Retried <b>@detailRetries</b> times.</div>
""";
    private readonly string example3CsharpCode = """"
private int detailRetries;
private readonly BitAiError detailError = new("The report service did not answer.")
{
    Code = "upstream_unavailable",
    Kind = BitAiErrorKind.Provider,
    IsRetryable = true,
    RetryAfter = TimeSpan.FromSeconds(30),
    Detail = """
        HTTP 503 Service Unavailable
        x-request-id: 7f3c2a91-4e0b-4f6e-9b1d-2c8a5e0d3b44
        retry-after: 30

        {
          "error": {
            "type": "overloaded",
            "message": "The report service is overloaded. Try again later."
          }
        }
        """,
};
"""";

    private readonly string example4RazorCode = """
<BitCard>
    <BitText Typography="BitTypography.Subtitle2">Monthly report</BitText>
    <BitAiErrorView Compact Error="detailError" />
</BitCard>
""";
    private readonly string example4CsharpCode = """"
private readonly BitAiError detailError = new("The report service did not answer.")
{
    Code = "upstream_unavailable",
    Kind = BitAiErrorKind.Provider,
    IsRetryable = true,
    RetryAfter = TimeSpan.FromSeconds(30),
    Detail = """
        HTTP 503 Service Unavailable
        x-request-id: 7f3c2a91-4e0b-4f6e-9b1d-2c8a5e0d3b44
        retry-after: 30

        {
          "error": {
            "type": "overloaded",
            "message": "The report service is overloaded. Try again later."
          }
        }
        """,
};
"""";

    private readonly string example5RazorCode = """
<BitAiErrorView Error="cancelledError" OnRetry="() => exceptionRetries++" />
<BitAiErrorView Error="timeoutError" OnRetry="() => exceptionRetries++" />
<BitAiErrorView Error="unknownError" OnRetry="() => exceptionRetries++" />

<div>Retried <b>@exceptionRetries</b> times.</div>
""";
    private readonly string example5CsharpCode = """
private int exceptionRetries;
private readonly BitAiError cancelledError = BitAiError.FromException(new OperationCanceledException());
private readonly BitAiError timeoutError = BitAiError.FromException(new TimeoutException());
private readonly BitAiError unknownError = FromThrownException();

private static BitAiError FromThrownException()
{
    try
    {
        throw new InvalidOperationException("The search index is out of date.");
    }
    catch (Exception exception)
    {
        return BitAiError.FromException(exception);
    }
}
""";

    private readonly string example6RazorCode = """
<BitAiErrorView Disabled Error="detailError" OnRetry="() => detailRetries++" />
""";

    private readonly string example7RazorCode = """
<BitParams Parameters="errorViewParams">
    <BitAiErrorView Error="detailError" OnRetry="() => detailRetries++" />
    <BitAiErrorView Error="timeoutError" OnRetry="() => detailRetries++" Disabled />
</BitParams>
""";
    private readonly string example7CsharpCode = """
private readonly BitAiErrorViewParams[] errorViewParams =
[
    new() { Styles = new() { RetryButton = new() { Root = "border-radius: 1rem" }, Detail = "font-size: 0.875rem" } },
];
""";

    private readonly string example8RazorCode = """
<style>
    .custom-error {
        padding: 0.5rem;
        border-radius: 0.75rem;
        background: repeating-linear-gradient(45deg, var(--bit-clr-err-tint), var(--bit-clr-err-tint) 0.5rem, transparent 0.5rem, transparent 1rem);
    }

    .custom-error-message {
        border-radius: 1rem;
        border-inline-start-width: 0.375rem;
    }

    .custom-error-actions {
        width: 100%;
        justify-content: flex-end;
    }
</style>

<BitAiErrorView Error="filterError" Style="box-shadow: 0 0.25rem 1rem #b91c1c40" />

<BitAiErrorView Error="detailError" Class="custom-error" OnRetry="() => detailRetries++" />

<BitAiErrorView Error="detailError"
                OnRetry="() => detailRetries++"
                Styles="@(new() { Text = "font-weight: bold",
                                  RetryAfter = "font-style: italic",
                                  RetryButton = new() { Root = "border-radius: 1rem" } })" />

<BitAiErrorView Error="detailError"
                OnRetry="() => detailRetries++"
                Classes="@(new() { Message = new() { Root = "custom-error-message" },
                                   Actions = "custom-error-actions" })" />

<div style="--bit-AiErrorView-detail-color: var(--bit-clr-err-fg); --bit-AiErrorView-detail-max-height: 4rem;">
    <BitAiErrorView Error="detailError" />
</div>
""";

    private readonly string example9RazorCode = """
<BitAiOptionsProvider UiOptions="persianOptions">
    <BitAiErrorView Dir="BitDir.Rtl" Error="rtlError" OnRetry="() => rtlRetries++" />
</BitAiOptionsProvider>

<div>Retried <b>@rtlRetries</b> times.</div>
""";
    private readonly string example9CsharpCode = """
private int rtlRetries;
private readonly BitAiUiOptions persianOptions = BitAiUiOptions.Default with { Strings = new PersianAiStrings() };
private readonly BitAiError rtlError = new("سرویس گزارش پاسخ نداد.")
{
    IsRetryable = true,
    RetryAfter = TimeSpan.FromSeconds(30),
    Detail = "HTTP 503 Service Unavailable",
};

// The words of the view, in Persian.
private sealed class PersianAiStrings : BitAiStrings
{
    public override string SomethingWentWrong => "مشکلی پیش آمد";
    public override string Retry => "تلاش دوباره";
    public override string ShowDetails => "نمایش جزئیات";
    public override string HideDetails => "پنهان کردن جزئیات";
    public override string RetryIn(TimeSpan wait) => $"{wait.TotalSeconds:F0} ثانیه‌ی دیگر دوباره تلاش کنید.";
}
""";
}
