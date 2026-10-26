namespace Bit.BlazorUI;

/// <summary>
/// BitAiCopyButton copies a text to the clipboard and confirms it: the glyph turns into a check mark, the name of the
/// button says so, and the confirmation is announced to screen readers, because a copy button that gives no feedback
/// leaves the user guessing whether it worked.
/// </summary>
public partial class BitAiCopyButton : BitAiComponentBase
{
    private bool _copied;
    private CancellationTokenSource? _resetCts;
    private BitAiIconClassesCache<BitButtonClassStyles> _copyClasses;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the copy button component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple copy button components
    /// through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiCopyButtonParams.ParamName)]
    public BitAiCopyButtonParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the copy button.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiCopyButtonClassStyles? Classes { get; set; }

    /// <summary>
    /// How long the confirmation stays on the button after a copy.
    /// <br />
    /// The default value is <strong>2 seconds</strong>.
    /// </summary>
    [Parameter] public TimeSpan ConfirmationDuration { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The text to copy. A null value disables the button.
    /// </summary>
    [Parameter] public string? Content { get; set; }

    /// <summary>
    /// The name of the button, shown as its tooltip and as its text when <see cref="ShowLabel"/> is set.
    /// <br />
    /// The default value is <strong>null</strong>, which uses the Copy text of the string table in force
    /// (<see cref="BitAiStrings.Copy"/>).
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// The callback raised after the text has been put on the clipboard.
    /// </summary>
    [Parameter] public EventCallback OnCopied { get; set; }

    /// <summary>
    /// Shows the label beside the icon.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    [Parameter] public bool ShowLabel { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the copy button.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiCopyButtonClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aicpb";

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

        base.OnParametersSet();
    }



    // The Copy glyph is not one of the core font's, so the class of the subset font this package ships goes on the icon,
    // beside whatever the page gave it (see BitAiIconClasses). The check mark (Accept) is a glyph of the core font and
    // needs nothing more. The copy is made again only when the page hands in another instance, so a re-render of the
    // button - every frame of a streaming message - does not reset its class builder.
    private BitButtonClassStyles? ButtonClasses =>
        _copied ? Classes?.Button : _copyClasses.Get("bit-icon-ai bit-icon-ai--Copy", Classes?.Button, BitAiIconClasses.With);

    private async Task HandleOnClick()
    {
        // A press that reaches the button while it is unavailable - a screen reader's click, a script - copies nothing.
        if (Disabled || Content is null) return;

        var copied = await _js.BitAiCopyButtonCopy(Content);

        if (copied is false || IsDisposed) return;

        _copied = true;

        await OnCopied.InvokeAsync();

        StateHasChanged();

        _resetCts?.Cancel();
        _resetCts?.Dispose();
        _resetCts = new CancellationTokenSource();
        var token = _resetCts.Token;

        try
        {
            await Task.Delay(ConfirmationDuration, token);

            _copied = false;

            await InvokeAsync(StateHasChanged);
        }
        catch (OperationCanceledException)
        {
            // Another copy started, or the button went away: whoever cancelled owns the reset.
        }
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        _resetCts?.Cancel();
        _resetCts?.Dispose();
        _resetCts = null;

        await base.DisposeAsync(disposing);
    }
}
