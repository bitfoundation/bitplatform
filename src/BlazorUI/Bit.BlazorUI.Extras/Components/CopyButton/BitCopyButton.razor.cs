namespace Bit.BlazorUI;

/// <summary>
/// BitCopyButton puts a value on the clipboard and confirms it: for a moment its glyph turns into a check mark, its text
/// says so, and the copy is announced to screen readers, because a copy button that gives no feedback leaves the user
/// guessing whether it worked.
/// </summary>
/// <remarks>
/// It is a <see cref="BitButton"/>, so it takes the button's <see cref="Variant"/>, <see cref="Size"/>,
/// <see cref="Color"/> and icon, and its look is restyled through the <c>--bit-Button-*</c> variables. With no
/// <see cref="Text"/> it is an icon button named Copy. It writes through the Clipboard API of the browser, which only
/// works in a secure context (https, or localhost); elsewhere, and where the browser refuses it, it falls back to the copy
/// command of the page. A copy that still fails leaves the button as it was and raises <see cref="OnCopyFailed"/>.
/// </remarks>
public partial class BitCopyButton : BitComponentBase
{
    private bool _copied;
    private CancellationTokenSource? _resetCts;
    private BitButtonClassStyles? _buttonClasses;
    private BitButtonClassStyles? _buttonClassesSource;



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
    [CascadingParameter(Name = BitCopyButtonParams.ParamName)]
    public BitCopyButtonParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the copy button.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitCopyButtonClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the button.
    /// </summary>
    [Parameter] public BitColor? Color { get; set; }

    /// <summary>
    /// How long the copied state - the copied glyph and text - stays on the button after a copy, before it goes back to
    /// what it was. Another copy while it shows starts it over.
    /// <br />
    /// The default value is <strong>2 seconds</strong>.
    /// </summary>
    [Parameter] public TimeSpan CopiedDuration { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The icon the button shows while it is in the copied state, using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="CopiedIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// With neither set, the button shows a check mark (<c>Accept</c>).
    /// </remarks>
    [Parameter, Outranks(nameof(Icon))] public BitIconInfo? CopiedIcon { get; set; }

    /// <summary>
    /// The name of the built-in Fluent UI icon the button shows while it is in the copied state.
    /// </summary>
    /// <remarks>
    /// With neither this nor <see cref="CopiedIcon"/> set, the button shows a check mark (<c>Accept</c>).
    /// </remarks>
    [Parameter] public string? CopiedIconName { get; set; }

    /// <summary>
    /// The text the button carries, and the live region announces, while it is in the copied state.
    /// <br />
    /// The default value is <strong>null</strong>, which uses the Copied text of the strings in force
    /// (<see cref="BitStrings.Copied"/>, "Copied").
    /// </summary>
    [Parameter] public string? CopiedText { get; set; }

    /// <summary>
    /// The icon of the button, using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// With neither set, the button shows the Copy glyph, drawn from the icon subset of this package.
    /// </remarks>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The name of the built-in Fluent UI icon of the button.
    /// </summary>
    /// <remarks>
    /// With neither this nor <see cref="Icon"/> set, the button shows the Copy glyph, drawn from the icon subset of this
    /// package.
    /// </remarks>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// The callback raised once the value is on the clipboard, with the value that was copied.
    /// </summary>
    [Parameter] public EventCallback<string> OnCopy { get; set; }

    /// <summary>
    /// The callback raised when the browser could not put the value on the clipboard (a permission denied, a copy
    /// command refused), with the error it gave. The button stays as it was.
    /// </summary>
    [Parameter] public EventCallback<Exception> OnCopyFailed { get; set; }

    /// <summary>
    /// The size of the button.
    /// </summary>
    [Parameter] public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the copy button.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitCopyButtonClassStyles? Styles { get; set; }

    /// <summary>
    /// The text of the button. While it is in the copied state, <see cref="CopiedText"/> takes its place.
    /// <br />
    /// The default value is <strong>null</strong>, which renders an icon button named by the Copy text of the strings in
    /// force (<see cref="BitStrings.Copy"/>, "Copy") - or by <see cref="BitComponentBase.AriaLabel"/> - and shows that
    /// name as its tooltip.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// The text to put on the clipboard, read when the button is pressed. A null value disables the button; an empty text
    /// is copied like any other.
    /// </summary>
    [Parameter] public string? Value { get; set; }

    /// <summary>
    /// The visual variant of the button.
    /// </summary>
    [Parameter] public BitVariant? Variant { get; set; }



    protected override string RootElementClass => "bit-cpb";

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
    // ahead of whatever the page gave it, the way BitNavPanel draws its toggle. The glyph classes are a literal: the
    // publish-time trimming keeps a glyph by the literals it finds in the code. The copy is made again only when the page
    // hands in another instance, since the button resets its class builder for every new one.
    private BitButtonClassStyles ButtonClasses
    {
        get
        {
            var classes = Classes?.Button;

            if (_buttonClasses is not null && ReferenceEquals(_buttonClassesSource, classes)) return _buttonClasses;

            const string iconClasses = "bit-icon-ex bit-icon-ex--Copy";

            _buttonClassesSource = classes;

            return _buttonClasses = new()
            {
                Root = classes?.Root,
                Icon = classes?.Icon is { Length: > 0 } icon ? $"{iconClasses} {icon}" : iconClasses,
                Container = classes?.Container,
                Primary = classes?.Primary,
                Secondary = classes?.Secondary,
                HiddenContent = classes?.HiddenContent,
                LoadingContainer = classes?.LoadingContainer,
                Spinner = classes?.Spinner,
                LoadingLabel = classes?.LoadingLabel,
            };
        }
    }

    private async Task HandleOnClick()
    {
        // A press that reaches the button while it is unavailable - a screen reader's click, a script - copies nothing.
        if (Disabled || Value is null) return;

        var value = Value;

        try
        {
            await _js.BitExtrasCopyToClipboard(value);
        }
        catch (JSDisconnectedException)
        {
            return; // the circuit is gone, and nobody is left to tell
        }
        catch (Exception ex)
        {
            // A clipboard that is not there to be written to leaves the button saying what it still offers to do, which
            // is the truth; the page is told, so it can say more than that.
            if (IsDisposed) return;

            await OnCopyFailed.InvokeAsync(ex);

            return;
        }

        if (IsDisposed) return;

        _copied = true;

        // Started before the callback of the app is awaited, so a button removed by it stops the reset as it goes; another
        // copy starts it over.
        StartReset();

        StateHasChanged();

        await OnCopy.InvokeAsync(value);
    }

    private void StartReset()
    {
        _resetCts?.Cancel();
        _resetCts?.Dispose();

        var cts = _resetCts = new CancellationTokenSource();

        // Task.Delay takes no negative span and none longer than int.MaxValue milliseconds.
        var delay = TimeSpan.FromMilliseconds(Math.Clamp(CopiedDuration.TotalMilliseconds, 0, int.MaxValue - 1));

        _ = ResetAfter(delay, cts.Token);
    }

    private async Task ResetAfter(TimeSpan delay, CancellationToken token)
    {
        try
        {
            await Task.Delay(delay, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested || IsDisposed || IsDisposing) return;

        await InvokeAsync(() =>
        {
            if (token.IsCancellationRequested || IsDisposed || IsDisposing) return;

            _copied = false;

            StateHasChanged();
        });
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
