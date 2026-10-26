namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.SourceCard;

public partial class BitAiSourceCardDemo
{
    private readonly string example1RazorCode = """
<BitAiSourceCard Source="renderModes" />
""";
    private readonly string example1CsharpCode = """
private static readonly BitAiCitationSource renderModes = new("render-modes", "Blazor render modes")
{
    Kind = BitAiSourceKind.Web,
    Url = "https://learn.microsoft.com/aspnet/core/blazor/components/render-modes",
    SiteName = "Microsoft Learn",
    Description = "How interactivity is chosen per component or per page.",
    Author = "Microsoft",
    PublishedAt = new DateTimeOffset(2025, 11, 11, 0, 0, 0, TimeSpan.Zero),
    Score = 0.94,
};
""";

    private readonly string example2RazorCode = """
<BitAiSourceCard Source="renderModes" Number="1" />
<BitAiSourceCard Source="circuits" Number="2" />
""";
    private readonly string example2CsharpCode = """
private static readonly BitAiCitationSource circuits = new("circuits", "SignalR circuit lifetime")
{
    Kind = BitAiSourceKind.Document,
    Url = "https://learn.microsoft.com/aspnet/core/blazor/fundamentals/signalr",
    SiteName = "Microsoft Learn",
    Description = "What happens to state when a circuit drops.",
    Author = "Microsoft",
    PublishedAt = new DateTimeOffset(2025, 11, 11, 0, 0, 0, TimeSpan.Zero),
    Score = 0.81,
};
""";

    private readonly string example3RazorCode = """
<BitToggle @bind-Value="blockImages" Text="Images = BitAiImagePolicy.Block" />

<BitAiOptionsProvider UiOptions="@(blockImages ? blockedImageOptions : BitAiUiOptions.Default)">
    <BitAiSourceCard Source="withFavicon" Number="1" />
    <BitAiSourceCard Source="hostOnly" Number="2" />
    <BitAiSourceCard Source="attachedFile" Number="3" />
</BitAiOptionsProvider>
""";
    private readonly string example3CsharpCode = """
private bool blockImages;

private static readonly BitAiUiOptions blockedImageOptions = BitAiUiOptions.Default with { Images = BitAiImagePolicy.Block };

private static readonly BitAiCitationSource withFavicon = new("bitplatform", "bitfoundation/bitplatform")
{
    Kind = BitAiSourceKind.Web,
    Url = "https://github.com/bitfoundation/bitplatform",
    SiteName = "GitHub",
    IconUrl = "https://github.com/favicon.ico",
};

private static readonly BitAiCitationSource hostOnly = new("signalr", "ASP.NET Core SignalR overview")
{
    Kind = BitAiSourceKind.Web,
    Url = "https://learn.microsoft.com/aspnet/core/signalr/introduction",
};

private static readonly BitAiCitationSource attachedFile = new("q3-report", "Q3 report.pdf")
{
    Kind = BitAiSourceKind.File,
    Description = "The quarterly report you attached to the conversation.",
    Author = "Finance team",
    Score = 0.72,
};
""";

    private readonly string example4RazorCode = """
<BitAiSourceCard Source="renderModes" Number="1" OnActivate="s => activated = s" />
<BitAiSourceCard Source="circuits" Number="2" OnActivate="s => activated = s" />
<BitAiSourceCard Source="attachedFile" Number="3" OnActivate="s => activated = s" Disabled />

@if (activated is not null)
{
    <BitCard>
        <BitText Typography="BitTypography.Subtitle1">@activated.Title</BitText>
        <BitText>@activated.Description</BitText>
        <BitText Typography="BitTypography.Caption1">
            @activated.Kind - @activated.Author - @activated.PublishedAt?.ToString("d") - score @activated.Score?.ToString("0.00")
        </BitText>
    </BitCard>
}
""";
    private readonly string example4CsharpCode = """
// renderModes, circuits and attachedFile are the sources of the examples above.
private BitAiCitationSource? activated;
""";

    private readonly string example5RazorCode = """
<BitAiSourceCard Source="untrusted" Number="1" />
""";
    private readonly string example5CsharpCode = """
private static readonly BitAiCitationSource untrusted = new("untrusted", "A page that says it is the docs")
{
    Kind = BitAiSourceKind.Web,
    Url = "javascript:alert('Gotcha')",
    SiteName = "docs.example.com",
    IconUrl = "data:image/svg+xml;base64,PHN2ZyB4bWxucz0naHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmcnLz4=",
};
""";

    private readonly string example6RazorCode = """
<BitParams Parameters="cardParams">
    <BitAiSourceCard Source="renderModes" Number="1" />
    <BitAiSourceCard Source="circuits" Number="2" />
    <BitAiSourceCard Source="attachedFile" Number="3" Styles="@(new() { Title = "font-weight: normal" })" />
</BitParams>
""";
    private readonly string example6CsharpCode = """
// renderModes, circuits and attachedFile are the sources of the examples above.
private readonly BitAiSourceCardParams[] cardParams =
[
    new() { Styles = new() { Title = "font-weight: 600", Index = "color: var(--bit-clr-pri)" } }
];
""";

    private readonly string example7RazorCode = """
<style>
    .custom-class {
        max-width: 16rem;
        box-shadow: 0 0 0.75rem #0ea5e955;
    }

    .custom-index {
        color: white;
        padding-inline: 0.375rem;
        border-radius: 1rem;
        background-color: #0ea5e9;
    }

    .custom-title {
        color: light-dark(#0369a1, #7dd3fc);
        text-decoration: underline;
    }
</style>

<BitAiSourceCard Source="renderModes" Number="1" Style="max-width: 16rem; outline: 1px dashed #0ea5e9; outline-offset: 0.25rem" />
<BitAiSourceCard Source="circuits"
                 Number="2"
                 Class="custom-class"
                 Classes="@(new() { Index = "custom-index", Title = "custom-title" })"
                 Styles="@(new() { Card = new() { Root = "border-radius: 1rem" } })" />
<div style="--bit-AiSourceCard-index-color: #7c3aed;
            --bit-AiSourceCard-title-color: #4c1d95;
            --bit-AiSourceCard-title-font-size: 1rem;
            --bit-AiSourceCard-site-color: #8b5cf6;
            --bit-Card-background: #f5f3ff;
            --bit-Card-border-color: #c4b5fd;
            --bit-Card-radius: 0.75rem">
    <BitAiSourceCard Source="withFavicon" Number="3" />
</div>
""";

    private readonly string example8RazorCode = """
<div dir="rtl">
    <BitAiSourceCard Source="rtlSource" Number="1" />
    <BitAiSourceCard Dir="BitDir.Rtl" Source="rtlSource" Number="2" />
</div>
""";
    private readonly string example8CsharpCode = """
private static readonly BitAiCitationSource rtlSource = new("render-modes", "حالت‌های رندر بلیزر")
{
    Kind = BitAiSourceKind.Web,
    Url = "https://learn.microsoft.com/fa-ir/aspnet/core/blazor/components/render-modes",
    SiteName = "مایکروسافت لرن",
};
""";
}
