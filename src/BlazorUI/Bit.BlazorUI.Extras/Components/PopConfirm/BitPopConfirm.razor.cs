namespace Bit.BlazorUI;

/// <summary>
/// A confirmation asked right where an action is triggered: a small popup anchored to the trigger, holding a question
/// and the two buttons that answer it, for the actions worth a second thought (a delete, a discard) that are not worth
/// a modal dialog.
/// </summary>
/// <remarks>
/// The trigger is the <see cref="ChildContent"/> - a button, most of the time - and a click on it opens the popup. The
/// popup is an alertdialog that keeps the focus while it is open, starting on the Cancel button. Every opening ends in
/// exactly one of <see cref="OnConfirm"/> and <see cref="OnCancel"/>: the Confirm button raises the first, and the Cancel
/// button, Escape and a click outside of the popup the second. The focus goes back to the trigger either way.
/// <br />
/// Closing the popup through <see cref="IsOpen"/> is the page's own doing, so it raises neither. A
/// <see cref="BitComponentBase.Disabled"/> pop confirm never opens, and leaves the trigger as it is.
/// </remarks>
public partial class BitPopConfirm : BitComponentBase
{
    private const string DefaultConfirmText = "Ok";
    private const string DefaultCancelText = "Cancel";



    [Inject] private IJSRuntime _js { get; set; } = default!;

    private BitCallout _callout = default!;
    private string _triggerId = default!;
    private string _titleId = default!;
    private string _messageId = default!;
    private BitCalloutClassStyles? _calloutClasses;
    private BitCalloutClassStyles? _calloutStyles;

    // Set while one of the two buttons closes the popup, so that the close it causes is not taken for a dismissal -
    // which is how the popup tells a Cancel or a Confirm apart from an Escape or a click outside of it.
    private bool _answering;



    /// <summary>
    /// The default values of the parameters of the pop confirm, cascaded through a <see cref="BitParams"/>. A value set
    /// on the pop confirm itself wins over it.
    /// </summary>
    [CascadingParameter(Name = BitPopConfirmParams.ParamName)]
    public BitPopConfirmParams? CascadingParameters { get; set; }



    /// <summary>
    /// The text of the Cancel button. Default: Cancel.
    /// </summary>
    [Parameter] public string? CancelText { get; set; }

    /// <summary>
    /// The trigger the popup is anchored to and opened from, a button most of the time.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for the different parts of the pop confirm.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitPopConfirmClassStyles? Classes { get; set; }

    /// <summary>
    /// The color of the Confirm button, Error for an action that destroys something. Default: the color of a button
    /// left without one (Primary).
    /// </summary>
    [Parameter] public BitColor? ConfirmColor { get; set; }

    /// <summary>
    /// The text of the Confirm button. Default: Ok.
    /// </summary>
    [Parameter] public string? ConfirmText { get; set; }

    /// <summary>
    /// Whether the popup is open.
    /// </summary>
    [Parameter, TwoWayBound]
    public bool IsOpen { get; set; }

    /// <summary>
    /// The question the popup asks, or what it explains under the title. The popup is described by it, or named by it
    /// where there is no <see cref="Title"/>.
    /// </summary>
    [Parameter] public string? Message { get; set; }

    /// <summary>
    /// The callback for when the question is answered with no: the Cancel button, Escape, or a click outside of the
    /// popup.
    /// </summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    /// <summary>
    /// The callback for when the question is answered with yes, through the Confirm button. It is raised once the popup
    /// has closed.
    /// </summary>
    [Parameter] public EventCallback OnConfirm { get; set; }

    /// <summary>
    /// The side of the trigger the popup is placed on. By default it takes the side with the most room.
    /// </summary>
    [Parameter] public BitPlacement? Placement { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the pop confirm.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitPopConfirmClassStyles? Styles { get; set; }

    /// <summary>
    /// The title of the popup, which names it.
    /// </summary>
    [Parameter] public string? Title { get; set; }



    protected override string RootElementClass => "bit-pcf";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        _triggerId = $"{_Id}-trg";
        _titleId = $"{_Id}-ttl";
        _messageId = $"{_Id}-msg";

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        // The popup is the callout's own element, which the Callout class and style are given to.
        _calloutClasses = Classes?.Callout is null ? null : new() { Content = Classes.Callout };
        _calloutStyles = Styles?.Callout is null ? null : new() { Content = Styles.Callout };

        base.OnParametersSet();
    }



    private string? GetAriaLabelledBy()
    {
        if (Title.HasValue()) return _titleId;

        return Message.HasValue() ? _messageId : null;
    }

    private string? GetAriaDescribedBy() => Title.HasValue() && Message.HasValue() ? _messageId : null;

    // The callout closes on its own for Escape and for a click outside of it, which is a no to the question; the close
    // the two buttons cause is answered by their own handlers.
    private async Task HandleIsOpenChanged(bool isOpen)
    {
        await AssignIsOpen(isOpen);

        if (isOpen || _answering) return;

        await FocusTrigger();

        await OnCancel.InvokeAsync();
    }

    // The callout hands the focus back to the trigger only when it still has it as it closes, which Escape leaves it,
    // while the press of a click outside of the popup has already dropped it to the body: the overlay it lands on takes
    // no focus. The question was the trigger's, so that is where the focus goes back to however it was answered - the
    // popup is still open here, and the trigger is outside of it, so the callout leaves it be.
    private async Task FocusTrigger()
    {
        if (IsDisposed) return;

        try
        {
            await _js.BitUtilsFocusFirstElement(_triggerId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private Task HandleOnCancelClick() => Answer(OnCancel);

    private Task HandleOnConfirmClick() => Answer(OnConfirm);

    // The popup is closed first - which hands the focus back to the trigger - so that what the answer does next (a
    // delete taking its time, a page navigated away from) happens with the popup out of the way.
    private async Task Answer(EventCallback callback)
    {
        _answering = true;
        try
        {
            await _callout.Close();
        }
        finally
        {
            _answering = false;
        }

        await callback.InvokeAsync();
    }
}
