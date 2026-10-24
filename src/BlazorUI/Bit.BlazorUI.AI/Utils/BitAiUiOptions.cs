namespace Bit.BlazorUI;

/// <summary>
/// Presentation settings shared by every component through a cascading value.
/// </summary>
/// <remarks>
/// Everything here is a default, not a constraint: individual components take parameters that
/// override these. The point is that a host sets its preferences once rather than on every usage.
/// <br />
/// How dense the AI components are is not one of these settings: they are spaced in the units of the bit theme, so
/// they follow its density like every other component of the library. A compact chat - in a sidebar or an embedded
/// panel - is a region with a lower <c>--bit-layout-density-scale</c>, for example a <see cref="BitThemeProvider"/>
/// around it whose theme takes the <see cref="BitThemeLayout.DensityScale"/> of
/// <see cref="BitThemeDensityPresets.CreateCompactOverlay"/>.
/// </remarks>
public sealed record BitAiUiOptions
{
    /// <summary>The default settings.</summary>
    public static readonly BitAiUiOptions Default = new();

    /// <summary>Which Markdown extensions to enable.</summary>
    public BitAiMarkdownOptions Markdown { get; init; } = BitAiMarkdownOptions.Default;

    /// <summary>
    /// Which images are fetched and shown: those of Markdown, image parts, image artifacts, and the favicons of sources.
    /// </summary>
    /// <remarks>
    /// Images are allowed by default. A host handling sensitive conversations can restrict them to
    /// listed hosts, load them only on request, or block them; see <see cref="BitAiImagePolicy"/>.
    /// Script-capable URL schemes are neutralised whatever the policy.
    /// </remarks>
    public BitAiImagePolicy Images { get; init; } = BitAiImagePolicy.Allow;

    /// <summary>The minimum time between renders while streaming.</summary>
    /// <remarks>
    /// The single most important performance dial. Raising it on Blazor Server reduces circuit
    /// traffic; lowering it makes WebAssembly feel marginally smoother at a real CPU cost.
    /// </remarks>
    public TimeSpan RenderInterval { get; init; } = BitAiRenderScheduler.DefaultInterval;

    /// <summary>Show a timestamp under each message.</summary>
    public bool ShowTimestamps { get; init; }

    /// <summary>Show token usage under assistant messages.</summary>
    public bool ShowUsage { get; init; }

    /// <summary>Show the model name under assistant messages.</summary>
    public bool ShowModelName { get; init; }

    /// <summary>Show the copy, regenerate, and feedback actions under assistant messages.</summary>
    public bool ShowMessageActions { get; init; } = true;

    /// <summary>Show a copy button on code blocks.</summary>
    public bool ShowCodeCopy { get; init; } = true;

    /// <summary>Show line numbers in code blocks.</summary>
    public bool ShowLineNumbers { get; init; }

    /// <summary>Collapse the reasoning panel until the user opens it.</summary>
    public bool CollapseReasoning { get; init; } = true;

    /// <summary>Collapse tool call cards until the user opens them.</summary>
    public bool CollapseToolCalls { get; init; } = true;

    /// <summary>
    /// Follow the newest content while streaming, until the user scrolls away.
    /// </summary>
    public bool AutoScroll { get; init; } = true;

    /// <summary>
    /// Switch the message list to virtualized rendering once it holds this many messages.
    /// </summary>
    /// <remarks>
    /// Virtualization costs correctness in small lists, where variable message heights make the
    /// scrollbar jump. Below the threshold every message is rendered, which is both simpler and
    /// faster; above it, only what fits on screen is.
    /// </remarks>
    public int VirtualizeThreshold { get; init; } = 60;

    /// <summary>Even out bursty text so it renders at a steady rate.</summary>
    public bool SmoothStreaming { get; init; } = true;

    /// <summary>Show the caret that blinks at the end of streaming text.</summary>
    public bool ShowStreamingCaret { get; init; } = true;

    /// <summary>Turn bracketed numbers in the text into interactive citation chips.</summary>
    public bool InlineCitations { get; init; } = true;

    /// <summary>Show the source list under a message that cites anything.</summary>
    public bool ShowSourceList { get; init; } = true;

    /// <summary>Send the prompt when Enter is pressed without a modifier.</summary>
    public bool SubmitOnEnter { get; init; } = true;

    /// <summary>The largest number of rows the prompt textarea grows to before scrolling.</summary>
    public int MaxComposerRows { get; init; } = 12;

    /// <summary>The user-facing text, for localisation.</summary>
    public BitAiStrings Strings { get; init; } = BitAiStrings.Default;
}
