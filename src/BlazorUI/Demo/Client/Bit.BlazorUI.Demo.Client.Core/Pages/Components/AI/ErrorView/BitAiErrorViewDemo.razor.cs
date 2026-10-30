using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ErrorView;

public partial class BitAiErrorViewDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAiErrorViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the error view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Compact",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the error for a place inside another card that already says what failed (a tool call, a tool result): it is titled by its own message instead of the generic heading, and the message is not repeated under it.",
        },
        new()
        {
            Name = "Error",
            Type = "BitAiError",
            DefaultValue = "",
            Description = "The failure to show (required). Its Message is the text, IsRetryable decides whether the retry is offered, RetryAfter adds how long to wait, and Detail goes behind the disclosure.",
            LinkType = LinkType.Link,
            Href = "#error",
        },
        new()
        {
            Name = "OnRetry",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Raised when the user asks to retry - typically BitAiChatSession.RetryAsync. The retry button is hidden while it is unset and while the error says retrying would not help (IsRetryable is false), and does nothing while the view is disabled.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiErrorViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the error view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiErrorViewClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiErrorView.",
                },
                new()
                {
                    Name = "Message",
                    Type = "BitMessageClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitMessage the BitAiErrorView draws the error with: its surface, icon, title and content.",
                    LinkType = LinkType.Link,
                    Href = "/components/message#message-class-styles",
                },
                new()
                {
                    Name = "Text",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the text of the error of the BitAiErrorView (not rendered when Compact, whose title is that text).",
                },
                new()
                {
                    Name = "RetryAfter",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the line saying how long to wait before retrying, of the BitAiErrorView.",
                },
                new()
                {
                    Name = "Detail",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the technical detail of the BitAiErrorView.",
                },
                new()
                {
                    Name = "Actions",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the row of the action buttons of the BitAiErrorView, rendered when there is a button to show.",
                },
                new()
                {
                    Name = "RetryButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the retry button of the BitAiErrorView.",
                    LinkType = LinkType.Link,
                    Href = "/components/button#button-class-styles",
                },
                new()
                {
                    Name = "DetailButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the button disclosing the technical detail of the BitAiErrorView.",
                    LinkType = LinkType.Link,
                    Href = "/components/button#button-class-styles",
                },
            ],
        },
        new()
        {
            Id = "error",
            Title = "BitAiError",
            Description = "A transport- and provider-independent description of a failure, fit for display and for driving the retry (a sealed record). Transports translate their own exceptions into it, so no component branches on the exception type of a provider's SDK.",
            Parameters =
            [
                new()
                {
                    Name = "BitAiError(message)",
                    Type = "constructor",
                    DefaultValue = "",
                    Description = "Creates an error with the message to show to the user.",
                },
                new()
                {
                    Name = "Message",
                    Type = "string",
                    DefaultValue = "",
                    Description = "A human-readable message, safe to show to the end user.",
                },
                new()
                {
                    Name = "Code",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "A stable machine-readable code, for example rate_limit.",
                },
                new()
                {
                    Name = "Kind",
                    Type = "BitAiErrorKind",
                    DefaultValue = "BitAiErrorKind.Unknown",
                    Description = "The kind of failure, which the app can drive its own recovery from. The view draws every kind the same way; an agent run ended with an error of the Cancelled kind (BitAiAgentRun.SetError) ends as cancelled rather than failed.",
                    LinkType = LinkType.Link,
                    Href = "#error-kind-enum",
                },
                new()
                {
                    Name = "IsRetryable",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether retrying the same request could plausibly succeed. The view offers the retry only when it is true.",
                },
                new()
                {
                    Name = "RetryAfter",
                    Type = "TimeSpan?",
                    DefaultValue = "null",
                    Description = "How long to wait before retrying, when the provider says so. The view shows it under the message from when it is handed the error until the wait is over, then takes the line away (it is not counted down: the message is an alert, which would announce every second).",
                },
                new()
                {
                    Name = "Detail",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Additional technical detail, shown behind a disclosure rather than inline - to the user, so never a stack trace or anything else they must not read.",
                },
                new()
                {
                    Name = "Exception",
                    Type = "Exception?",
                    DefaultValue = "null",
                    Description = "The originating exception, if any. Never serialized and never shown.",
                },
                new()
                {
                    Name = "FromException(exception)",
                    Type = "BitAiError",
                    DefaultValue = "",
                    Description = "Translates an exception: an OperationCanceledException into a Cancelled error (code cancelled, not retryable), a TimeoutException into a retryable Timeout one (code timeout), anything else into a retryable Unknown one carrying the exception's message - which is shown, so a transport whose exceptions say what users must not read translates them itself. The stack trace is never put in the Detail, which is shown too; the exception stays on Exception, for the app's logs.",
                },
            ],
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "error-kind-enum",
            Name = "BitAiErrorKind",
            Description = "The broad category of a failure.",
            Items =
            [
                new() { Name = "Unknown", Description = "Unclassified failure.", Value = "0" },
                new() { Name = "Network", Description = "The network or the transport failed.", Value = "1" },
                new() { Name = "Authentication", Description = "Credentials are missing, invalid or expired.", Value = "2" },
                new() { Name = "Authorization", Description = "The caller is authenticated but not permitted.", Value = "3" },
                new() { Name = "RateLimit", Description = "The provider is rate limiting the caller.", Value = "4" },
                new() { Name = "ContextLength", Description = "The request exceeded the context window of the model.", Value = "5" },
                new() { Name = "ContentFilter", Description = "A safety system of the provider refused the request.", Value = "6" },
                new() { Name = "InvalidRequest", Description = "The provider rejected the request as malformed.", Value = "7" },
                new() { Name = "Provider", Description = "The provider failed internally.", Value = "8" },
                new() { Name = "Tool", Description = "A tool threw or returned a failure while running.", Value = "9" },
                new() { Name = "Timeout", Description = "The request timed out.", Value = "10" },
                new() { Name = "Cancelled", Description = "The caller cancelled the request.", Value = "11" },
            ],
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiErrorView-detail-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Color of the technical detail." },
        new() { Name = "--bit-AiErrorView-detail-max-height", DefaultValue = "spacing(24)", Description = "Height past which the technical detail scrolls." },
    ];



    private readonly BitAiError filterError = new("This request was blocked by the content filter of the provider.")
    {
        Code = "content_filter",
        Kind = BitAiErrorKind.ContentFilter,
    };



    private BitAiRenderScheduler sessionRenders = default!;
    private BitAiChatSession retrySession = default!;

    protected override Task OnInitAsync()
    {
        sessionRenders = new BitAiRenderScheduler(() => InvokeAsync(StateHasChanged));
        retrySession = CreateRetrySession();

        return base.OnInitAsync();
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

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (retrySession is not null) await retrySession.DisposeAsync();
        if (sessionRenders is not null) await sessionRenders.DisposeAsync();

        await base.DisposeAsync(disposing);
    }



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



    private int rtlRetries;
    private readonly BitAiUiOptions persianOptions = BitAiUiOptions.Default with { Strings = new PersianAiStrings() };
    private readonly BitAiError rtlError = new("سرویس گزارش پاسخ نداد.")
    {
        IsRetryable = true,
        RetryAfter = TimeSpan.FromSeconds(30),
        Detail = "HTTP 503 Service Unavailable",
    };

    private sealed class PersianAiStrings : BitAiStrings
    {
        public override string SomethingWentWrong => "مشکلی پیش آمد";
        public override string Retry => "تلاش دوباره";
        public override string ShowDetails => "نمایش جزئیات";
        public override string HideDetails => "پنهان کردن جزئیات";
        public override string RetryIn(TimeSpan wait) => $"{wait.TotalSeconds:F0} ثانیه‌ی دیگر دوباره تلاش کنید.";
    }



    private readonly BitAiErrorViewParams[] errorViewParams =
    [
        new() { Styles = new() { RetryButton = new() { Root = "border-radius: 1rem" }, Detail = "font-size: 0.875rem" } },
    ];
}
