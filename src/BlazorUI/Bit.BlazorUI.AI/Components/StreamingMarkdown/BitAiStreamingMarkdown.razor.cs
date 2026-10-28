namespace Bit.BlazorUI;

/// <summary>
/// BitAiStreamingMarkdown renders Markdown that is still being written, as a <see cref="BitAiMarkdownView"/> that
/// follows the text while it grows.
/// </summary>
/// <remarks>
/// It holds one incremental parser for its lifetime, so text already seen is parsed once no matter how many deltas
/// arrive. That is the difference between a long streamed answer costing linear work and quadratic work.
/// <br />
/// The parser is built from the Markdown options in force (<see cref="BitAiUiOptions.Markdown"/>), and built again if
/// a different options instance is cascaded, so the parser and the renderer always agree on what is enabled.
/// </remarks>
public partial class BitAiStreamingMarkdown : BitAiComponentBase
{
    private BitAiIncrementalMarkdownParser? _parser;
    private BitAiMarkdownOptions? _parserOptions;
    private BitAiMarkdownDocument? _document;
    private string? _lastText;
    private bool _lastStreaming = true;



    /// <summary>
    /// Gets or sets the cascading parameters for the streaming Markdown component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings (the classes and styles of its parts) to be
    /// applied to multiple streaming Markdown components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiStreamingMarkdownParams.ParamName)]
    public BitAiStreamingMarkdownParams? CascadingParameters { get; set; }



    /// <summary>
    /// The citations that the inline citation markers of the text resolve against.
    /// </summary>
    [Parameter] public IReadOnlyList<BitAiCitation>? Citations { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the streaming Markdown.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiStreamingMarkdownClassStyles? Classes { get; set; }

    /// <summary>
    /// Whether more text is expected, which draws the streaming caret and keeps the last block open.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    [Parameter] public bool IsStreaming { get; set; }

    /// <summary>
    /// The callback for when the user activates an inline citation marker.
    /// </summary>
    [Parameter] public EventCallback<BitAiCitation> OnCitationClick { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the streaming Markdown.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiStreamingMarkdownClassStyles? Styles { get; set; }

    /// <summary>
    /// The Markdown received so far.
    /// </summary>
    [Parameter] public string? Text { get; set; }



    protected override string RootElementClass => "bit-aismd";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        var markdownOptions = Options.Markdown;
        if (_parser is null || ReferenceEquals(_parserOptions, markdownOptions) is false)
        {
            _parser = new BitAiIncrementalMarkdownParser(markdownOptions);
            _parserOptions = markdownOptions;
            _lastText = null;
        }

        if (_lastText != Text || _lastStreaming != IsStreaming)
        {
            _lastText = Text;
            _lastStreaming = IsStreaming;
            _document = _parser.Parse(Text, IsStreaming is false);
        }

        base.OnParametersSet();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        _parser?.Reset();
        _parser = null;
        _document = null;

        await base.DisposeAsync(disposing);
    }
}
