namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.SourceList;

public partial class BitAiSourceListDemo
{
    private readonly string example1RazorCode = """
<BitAiSourceList Citations="citations" />
""";
    private readonly string example1CsharpCode = """
private static readonly BitAiCitationSource renderModesSource = new("render-modes", "Blazor render modes")
{
    Kind = BitAiSourceKind.Web,
    Url = "https://learn.microsoft.com/aspnet/core/blazor/components/render-modes",
    SiteName = "Microsoft Learn",
    Description = "How interactivity is chosen per component or per page.",
};

private static readonly BitAiCitationSource circuitsSource = new("circuits", "SignalR circuit lifetime")
{
    Kind = BitAiSourceKind.Document,
    Url = "https://learn.microsoft.com/aspnet/core/blazor/fundamentals/signalr",
    SiteName = "Microsoft Learn",
    Description = "What happens to state when a circuit drops.",
};

private static readonly BitAiCitation[] citations =
[
    new("c1", renderModesSource) { Number = 1 },
    new("c2", circuitsSource) { Number = 2 },
];
""";

    private readonly string example2RazorCode = """
<BitAiSourceList Citations="mixedCitations" />
""";
    private readonly string example2CsharpCode = """
// renderModesSource and circuitsSource are the sources of the Basic example.
private static readonly BitAiCitationSource reportSource = new("q3-report", "Q3 report.pdf") { Kind = BitAiSourceKind.File };

private static readonly BitAiCitation[] mixedCitations =
[
    new("c1", circuitsSource) { Number = 2 },
    new("c2", renderModesSource) { Number = 1 },
    new("c3", circuitsSource) { Number = 3, Quote = "Another passage of the same document." },
    new("c4", reportSource),
];
""";

    private readonly string example3RazorCode = """
<BitAiSourceList Citations="citations" ShowHeading="false" />
""";

    private readonly string example4RazorCode = """
<BitAiSourceList Citations="citations" OnSourceClick="s => clicked = s" />

<div>Clicked: <b>@(clicked?.Title ?? "-")</b> @clicked?.Description</div>
""";
    private readonly string example4CsharpCode = """
private BitAiCitationSource? clicked;
""";

    private readonly string example5RazorCode = """
@implements IAsyncDisposable

<BitButton OnClick="AskCited" Disabled="isAsking">Ask</BitButton>

@if (citedMessage is not null)
{
    <div>@citedMessage.GetText()</div>

    <BitAiSourceList Citations="citedMessage.Citations" />
}
""";
    private readonly string example5CsharpCode = """
// AiDemoContentTransports.Cited is a scripted transport of this site whose answer cites two sources:
// https://github.com/bitfoundation/bitplatform/blob/develop/src/BlazorUI/Demo/Client/Bit.BlazorUI.Demo.Client.Core/Services/AI/AiDemoContentTransports.cs
private BitAiChatSession? citedSession;
private BitAiMessage? citedMessage;
private bool isAsking;

private async Task AskCited()
{
    isAsking = true;

    if (citedSession is not null) await citedSession.DisposeAsync();

    citedSession = new BitAiChatSession(AiDemoContentTransports.Cited());
    citedMessage = await citedSession.SubmitAsync("What does Interactive Server trade away?");

    isAsking = false;
}

public async ValueTask DisposeAsync()
{
    if (citedSession is not null) await citedSession.DisposeAsync();
}
""";

    private readonly string example6RazorCode = """
<BitParams Parameters="listParams">
    <BitAiSourceList Citations="citations" />

    <BitAiSourceList Citations="citations" ShowHeading="true" />
</BitParams>
""";
    private readonly string example6CsharpCode = """
private readonly BitAiSourceListParams[] listParams =
[
    new() { ShowHeading = false, Styles = new() { SourceCard = new() { Title = "font-weight: 600" } } }
];
""";

    private readonly string example7RazorCode = """
<style>
    .custom-class {
        padding: 0.75rem;
        border-radius: 0.5rem;
        background-color: #ecfeff;
    }

    .custom-header {
        color: #0e7490;
        font-size: 1rem;
        letter-spacing: normal;
        text-transform: none;
    }

    .custom-title {
        color: #155e75;
    }
</style>

<BitAiSourceList Citations="citations" Style="padding: 0.75rem; border: 1px dashed #0ea5e9; border-radius: 0.5rem" />

<BitAiSourceList Citations="citations"
                 Class="custom-class"
                 Classes="@(new() { Header = "custom-header", SourceCard = new() { Title = "custom-title" } })"
                 Styles="@(new() { List = "gap: 0.25rem" })" />

<BitAiSourceList Citations="citations"
                 Style="--bit-AiSourceList-column-min-width: 100%;
                        --bit-AiSourceList-gap: 0.25rem;
                        --bit-AiSourceList-header-color: #7c3aed;
                        --bit-AiSourceList-header-font-size: 0.875rem;
                        --bit-AiSourceCard-index-color: #7c3aed" />
""";

    private readonly string example8RazorCode = """
<BitAiSourceList Dir="BitDir.Rtl" Citations="rtlCitations" />
""";
    private readonly string example8CsharpCode = """
private static readonly BitAiCitation[] rtlCitations =
[
    new("c1", new BitAiCitationSource("render-modes", "حالت‌های رندر بلیزر")
    {
        Kind = BitAiSourceKind.Web,
        Url = "https://learn.microsoft.com/fa-ir/aspnet/core/blazor/components/render-modes",
        SiteName = "مایکروسافت لرن",
    }) { Number = 1 },
    new("c2", new BitAiCitationSource("circuits", "طول عمر اتصال SignalR")
    {
        Kind = BitAiSourceKind.Document,
        Url = "https://learn.microsoft.com/fa-ir/aspnet/core/blazor/fundamentals/signalr",
        SiteName = "مایکروسافت لرن",
    }) { Number = 2 },
];
""";
}
