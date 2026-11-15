namespace Bit.BlazorUI;

/// <summary>
/// BitAiReasoningPartView is the collapsible panel that holds the reasoning of a model, rendered as Markdown.
/// </summary>
/// <remarks>
/// The panel is open while the model is still thinking, so the user sees progress rather than a spinner, and collapses
/// once the reasoning is complete (when <see cref="BitAiUiOptions.CollapseReasoning"/> is on), so it does not compete
/// with the answer. A user who opened or closed the panel by hand wins over both.
/// <br />
/// The panel follows the part itself, re-rendering at most once per <see cref="BitAiUiOptions.RenderInterval"/> while
/// the reasoning streams in, without waiting for the message around it to render again.
/// </remarks>
public partial class BitAiReasoningPartView : BitAiComponentBase
{
    private bool _open;
    private bool _userDecided;
    private bool _wasStreaming;
    private BitAiReasoningPart? _subscribed;



    /// <summary>
    /// Gets or sets the cascading parameters for the reasoning panel component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings (the classes and styles of its parts) to be
    /// applied to multiple reasoning panel components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiReasoningPartViewParams.ParamName)]
    public BitAiReasoningPartViewParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the reasoning panel.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiReasoningPartViewClassStyles? Classes { get; set; }

    /// <summary>
    /// The reasoning to render.
    /// </summary>
    [Parameter, EditorRequired] public BitAiReasoningPart Part { get; set; } = default!;

    /// <summary>
    /// Custom CSS styles for different parts of the reasoning panel.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiReasoningPartViewClassStyles? Styles { get; set; }



    // "Reasoning" while the model thinks, "Thought for 12s" once it is done.
    private string Title => Part.Duration is { } duration && Part.IsStreaming is false
        ? Strings.ReasonedFor(duration)
        : Strings.Reasoning;



    protected override string RootElementClass => "bit-airpv";

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

        if (ReferenceEquals(_subscribed, Part) is false)
        {
            if (_subscribed is not null)
            {
                _subscribed.Changed -= OnPartChanged;
            }

            _subscribed = Part;
            _subscribed.Changed += OnPartChanged;
            _userDecided = false;
            _wasStreaming = Part.IsStreaming;
            _open = Part.IsStreaming || Options.CollapseReasoning is false;
        }

        ApplyAutoState();

        base.OnParametersSet();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        if (_subscribed is not null)
        {
            _subscribed.Changed -= OnPartChanged;
            _subscribed = null;
        }

        await base.DisposeAsync(disposing);
    }



    private void OnPartChanged(BitAiMessagePart part)
    {
        ApplyAutoState();
        RequestRender();
    }

    private void ApplyAutoState()
    {
        if (_wasStreaming == Part.IsStreaming) return;

        _wasStreaming = Part.IsStreaming;

        // Streaming just finished: collapse unless the host wants reasoning visible or the user already made a choice.
        if (Part.IsStreaming is false && _userDecided is false && Options.CollapseReasoning)
        {
            _open = false;
        }
    }

    private void OnUserToggled(bool open)
    {
        _open = open;
        _userDecided = true;
    }
}
