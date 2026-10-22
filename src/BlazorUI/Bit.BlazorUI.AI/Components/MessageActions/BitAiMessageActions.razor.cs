using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiMessageActions is the row under a finished assistant message: copy, regenerate and feedback, plus whatever
/// metadata the options in force choose to show (the model, the token usage, the time).
/// </summary>
/// <remarks>
/// The actions are revealed when the message they belong to is hovered or holds the focus, on a device that can hover;
/// on a touch screen, under a preference for more contrast, and when <see cref="Pinned"/> is set, they are always shown.
/// The metadata is always shown.
/// </remarks>
public partial class BitAiMessageActions : BitAiComponentBase
{
    private BitAiIconClassesCache<BitButtonClassStyles> _regenerateButtonClasses;
    private BitAiIconClassesCache<BitButtonClassStyles> _positiveButtonClasses;
    private BitAiIconClassesCache<BitButtonClassStyles> _negativeButtonClasses;



    /// <summary>
    /// Gets or sets the cascading parameters for the message actions component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple message actions
    /// components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiMessageActionsParams.ParamName)]
    public BitAiMessageActionsParams? CascadingParameters { get; set; }



    /// <summary>
    /// Offers regeneration of the message. Only the newest assistant message should offer it, since regenerating an older
    /// one discards everything after it.
    /// <br />
    /// The default value is <strong>true</strong>.
    /// </summary>
    [Parameter] public bool CanRegenerate { get; set; } = true;

    /// <summary>
    /// Custom CSS classes for different parts of the message actions.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiMessageActionsClassStyles? Classes { get; set; }

    /// <summary>
    /// The extra buttons appended to the row of actions.
    /// </summary>
    [Parameter] public RenderFragment? ExtraActionsTemplate { get; set; }

    /// <summary>
    /// The message these actions belong to.
    /// </summary>
    [Parameter, EditorRequired] public BitAiMessage Message { get; set; } = default!;

    /// <summary>
    /// The callback for when the user records feedback, so a host can persist it.
    /// </summary>
    [Parameter] public EventCallback<BitAiFeedbackKind> OnFeedback { get; set; }

    /// <summary>
    /// Keeps the actions visible rather than revealing them on hover.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    [Parameter, ResetClassBuilder] public bool Pinned { get; set; }

    /// <summary>
    /// The session, which enables regeneration.
    /// </summary>
    [Parameter] public BitAiChatSession? Session { get; set; }

    /// <summary>
    /// Shows the thumbs up and down buttons.
    /// <br />
    /// The default value is <strong>true</strong>.
    /// </summary>
    [Parameter] public bool ShowFeedback { get; set; } = true;

    /// <summary>
    /// Custom CSS styles for different parts of the message actions.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiMessageActionsClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aimac";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root)
                    .Register(() => Pinned ? "bit-aimac-pin" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiMessageActionsParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }



    // None of the glyphs is one of the core font's, so they are drawn from this package's own subset (see
    // BitAiIconClasses), and each button is handed the same classes while the app's own and its glyph stay the same.
    private BitButtonClassStyles _RegenerateButtonClasses =>
        _regenerateButtonClasses.Get("bit-icon-ai bit-icon-ai--Refresh", Classes?.RegenerateButton, BitAiIconClasses.With);

    private BitButtonClassStyles _PositiveButtonClasses =>
        _positiveButtonClasses.Get(Message.Feedback is BitAiFeedbackKind.Positive ? "bit-icon-ai bit-icon-ai--LikeSolid" : "bit-icon-ai bit-icon-ai--Like",
                                   Classes?.FeedbackButton,
                                   BitAiIconClasses.With);

    private BitButtonClassStyles _NegativeButtonClasses =>
        _negativeButtonClasses.Get(Message.Feedback is BitAiFeedbackKind.Negative ? "bit-icon-ai bit-icon-ai--DislikeSolid" : "bit-icon-ai bit-icon-ai--Dislike",
                                   Classes?.FeedbackButton,
                                   BitAiIconClasses.With);

    private async Task RegenerateAsync()
    {
        if (Disabled || Session is null) return;

        await Session.RegenerateAsync(Message.MessageId);
    }

    // Pressing the button of the feedback already recorded withdraws it.
    private async Task SetFeedbackAsync(BitAiFeedbackKind feedback)
    {
        if (Disabled) return;

        var next = Message.Feedback == feedback ? BitAiFeedbackKind.None : feedback;

        Message.SetFeedback(next);

        await OnFeedback.InvokeAsync(next);
    }
}
