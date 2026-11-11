namespace Bit.BlazorUI;

/// <summary>
/// BitAiContextMeter shows how much of the context window of the model the conversation is using.
/// </summary>
/// <remarks>
/// Worth showing because running out of context is the failure users understand least: the model simply starts
/// forgetting. A meter that turns to a warning before it happens is far kinder than an error afterwards - so it turns
/// to the warning color at <see cref="Warn"/> and to the error color at <see cref="Critical"/>, and says so once
/// through a polite live region as it crosses each of them.
/// <br />
/// The tokens used and the size of the window are read off the <see cref="Conversation"/> when it carries them (see
/// <see cref="Measure"/>), else taken from <see cref="Used"/> and <see cref="Window"/>, with the window of the
/// <see cref="Model"/> as the last resort. Nothing is rendered while no window is known.
/// </remarks>
public partial class BitAiContextMeter : BitAiComponentBase
{
    // Derived values live in fields rather than being written back into the parameters: a parameter the parent does
    // not pass is never reset, so a window filled in once would stick even after the model or the conversation changed.
    private long _used;
    private long? _window;
    private Severity _severity;



    /// <summary>
    /// Gets or sets the cascading parameters for the context meter component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings (the two thresholds, and the look) to be applied
    /// to multiple context meter components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiContextMeterParams.ParamName)]
    public BitAiContextMeterParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the context meter.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitAiContextMeterClassStyles? Classes { get; set; }

    /// <summary>
    /// Reads the used tokens and the window from a conversation instead of taking them directly.
    /// </summary>
    [Parameter] public BitAiConversation? Conversation { get; set; }

    /// <summary>
    /// The fraction of the window at which the meter turns to its critical (error) styling.
    /// <br />
    /// The default value is <strong>0.95</strong>.
    /// </summary>
    [Parameter] public double Critical { get; set; } = 0.95;

    /// <summary>
    /// Supplies the window when neither <see cref="Window"/> nor the <see cref="Conversation"/> carries one.
    /// </summary>
    [Parameter] public BitAiModel? Model { get; set; }

    /// <summary>
    /// An estimate of the tokens about to be added, such as the prompt being typed, counted on top of what the
    /// conversation has used.
    /// </summary>
    [Parameter] public long PendingTokens { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the context meter.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitAiContextMeterClassStyles? Styles { get; set; }

    /// <summary>
    /// The tokens used so far. Overridden by the <see cref="Conversation"/> when it carries usage.
    /// </summary>
    [Parameter] public long Used { get; set; }

    /// <summary>
    /// The fraction of the window at which the meter turns to its warning styling.
    /// <br />
    /// The default value is <strong>0.8</strong>.
    /// </summary>
    [Parameter] public double Warn { get; set; } = 0.8;

    /// <summary>
    /// The size of the context window.
    /// </summary>
    [Parameter] public long? Window { get; set; }



    /// <summary>
    /// Works out how full the context is from a conversation.
    /// </summary>
    /// <param name="conversation">The conversation.</param>
    /// <returns>
    /// The tokens the next request will carry and the window size, either of which is <see langword="null"/> when the
    /// conversation does not report it.
    /// </returns>
    /// <remarks>
    /// The right figure is the latest assistant message's prompt plus its output, because the next request sends the
    /// whole thread again and that message's input tokens already cover it. Summing every turn's usage, as a total-cost
    /// readout does, counts the early turns once per later turn and runs the meter to the top long before the model
    /// forgets anything.
    /// </remarks>
    public static (long? Used, long? Window) Measure(BitAiConversation conversation)
    {
        ArgumentNullException.ThrowIfNull(conversation);

        long? used = null;
        long? window = null;
        var messages = conversation.Messages;

        for (var i = messages.Count - 1; i >= 0; i--)
        {
            var metadata = messages[i].Metadata;
            if (metadata is null) continue;

            window ??= metadata.ContextWindow;

            if (used is null && messages[i].Role == BitAiRole.Assistant && metadata.Usage.TotalTokens is { } total)
            {
                used = total;
            }

            if (used is not null && window is not null) break;
        }

        return (used, window);
    }



    protected override string RootElementClass => "bit-aictm";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        // The state the reading is in, worked out from the parameters on every pass (see OnParametersSet).
        ClassBuilder.Register(() => _severity switch
        {
            Severity.Warning => "bit-aictm-wrn",
            Severity.Critical => "bit-aictm-crt",
            _ => string.Empty
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        _used = Used;
        _window = Window;

        if (Conversation is { } conversation)
        {
            var (used, window) = Measure(conversation);
            if (used is { } value) _used = value;
            _window ??= window;
        }

        _used += PendingTokens;
        _window ??= Model?.ContextWindow;

        // The severity is derived from several parameters at once rather than set by one of them, so the classes are
        // built again only when it actually moves.
        var severity = _Fraction switch
        {
            var f when f >= Critical => Severity.Critical,
            var f when f >= Warn => Severity.Warning,
            _ => Severity.None,
        };

        if (severity != _severity)
        {
            _severity = severity;
            ClassBuilder.Reset();
        }

        base.OnParametersSet();
    }



    private double _Fraction => _window is { } window && window > 0 ? (double)_used / window : 0;

    // The share of the window used, cut to a whole percent. Worked out in whole numbers rather than off the fraction,
    // whose binary rounding would cut 29 of 100 tokens to 28%.
    private long _Percent => _window is { } window && window > 0 ? Math.Max(0, _used) * 100 / window : 0;

    private BitColor? _MeterColor => _severity switch
    {
        Severity.Critical => BitColor.Error,
        Severity.Warning => BitColor.Warning,
        _ => null,
    };

    private string? _Announcement => _severity switch
    {
        Severity.Critical => Strings.ContextCritical,
        Severity.Warning => Strings.ContextWarning,
        _ => null,
    };



    private enum Severity
    {
        None,
        Warning,
        Critical,
    }
}
