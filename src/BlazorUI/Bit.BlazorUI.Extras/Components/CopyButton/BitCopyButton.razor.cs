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
/// command of the page. A copy that still fails confirms nothing - a confirmation of an earlier copy still showing is
/// taken off - and raises <see cref="OnCopyFailed"/>.
/// </remarks>
public partial class BitCopyButton : BitComponentBase
{
    private bool _copied;
    private int _copyCount;
    private string? _copiedValue;
    private CancellationTokenSource? _resetCts;
    private BitButtonClassStyles? _buttonClasses;
    private BitButtonClassStyles? _buttonClassesSource;
    private BitButtonClassStyles? _buttonClassesSnapshot;



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
    /// what it was. Another copy while it shows starts it over. A zero or negative duration confirms nothing: the copy is
    /// made and <see cref="OnCopy"/> raised, but the button never shows the copied state and nothing is announced.
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
    /// command refused), with the error it gave. The button confirms nothing, and a confirmation of an earlier copy still
    /// showing is taken off.
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
    /// The text to put on the clipboard, read when the button is pressed. A null value disables the button, the way
    /// <see cref="BitComponentBase.Disabled"/> does; an empty text is copied like any other. A confirmation still showing
    /// is taken off once the value changes, since it was about the one before.
    /// </summary>
    [Parameter, ResetClassBuilder] public string? Value { get; set; }

    /// <summary>
    /// The visual variant of the button.
    /// </summary>
    [Parameter] public BitVariant? Variant { get; set; }



    protected override string RootElementClass => "bit-cpb";

    // A button with no value is as unavailable as a disabled one, so the root says so the same way (the base registers
    // bit-dis for Disabled itself) and drops the page's activation handlers.
    private protected override bool IsActivationDisabled => Disabled || Value is null;

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Disabled is false && Value is null ? "bit-dis" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        // The confirmation is about the value that was copied: a button handed another value (a reused row of a list, a
        // text still being typed), or made unavailable, no longer has a copy to show for itself.
        if (_copied && (Disabled || Value is null || string.Equals(Value, _copiedValue, StringComparison.Ordinal) is false))
        {
            ClearCopied();
        }

        base.OnParametersSet();
    }



    // The Copy glyph is not one of the core font's, so the class of the subset font this package ships goes on the icon,
    // ahead of whatever the page gave it, the way BitNavPanel draws its toggle. The glyph classes are a literal: the
    // publish-time trimming keeps a glyph by the literals it finds in the code. The copy is made again only when the page
    // hands in another instance or changes a class of the one it gave, since the button resets its class builder for
    // every new one.
    private BitButtonClassStyles ButtonClasses
    {
        get
        {
            var classes = Classes?.Button;

            if (_buttonClasses is not null &&
                ReferenceEquals(_buttonClassesSource, classes) &&
                HaveSameClasses(_buttonClassesSnapshot, classes)) return _buttonClasses;

            const string iconClasses = "bit-icon-ex bit-icon-ex--Copy";

            _buttonClassesSource = classes;
            _buttonClassesSnapshot = classes is null ? null : new()
            {
                Root = classes.Root,
                Icon = classes.Icon,
                Container = classes.Container,
                Primary = classes.Primary,
                Secondary = classes.Secondary,
                HiddenContent = classes.HiddenContent,
                LoadingContainer = classes.LoadingContainer,
                Spinner = classes.Spinner,
                LoadingLabel = classes.LoadingLabel,
            };

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

    private static bool HaveSameClasses(BitButtonClassStyles? a, BitButtonClassStyles? b)
    {
        if (a is null || b is null) return a is null && b is null;

        return a.Root == b.Root &&
               a.Icon == b.Icon &&
               a.Container == b.Container &&
               a.Primary == b.Primary &&
               a.Secondary == b.Secondary &&
               a.HiddenContent == b.HiddenContent &&
               a.LoadingContainer == b.LoadingContainer &&
               a.Spinner == b.Spinner &&
               a.LoadingLabel == b.LoadingLabel;
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
            // is the truth - a confirmation of an earlier copy still showing would claim this one worked, so it goes; the
            // page is told, so it can say more than that.
            if (IsDisposed) return;

            if (_copied)
            {
                ClearCopied();

                StateHasChanged();
            }

            await OnCopyFailed.InvokeAsync(ex);

            return;
        }

        if (IsDisposed) return;

        if (CopiedDuration > TimeSpan.Zero)
        {
            _copied = true;
            _copiedValue = value;
            // A new key for every copy puts a new node in the live region, so a copy made while the previous one is still
            // confirmed is announced too, rather than landing as the same text the region already holds.
            _copyCount++;

            // Started before the callback of the app is awaited, so a button removed by it stops the reset as it goes;
            // another copy starts it over.
            StartReset();

            StateHasChanged();
        }
        else if (_copied)
        {
            // No confirmation is asked for (a duration that may have been set to zero since the previous copy).
            ClearCopied();

            StateHasChanged();
        }

        await OnCopy.InvokeAsync(value);
    }

    private void ClearCopied()
    {
        _copied = false;
        _copiedValue = null;

        _resetCts?.Cancel();
        _resetCts?.Dispose();
        _resetCts = null;
    }

    private void StartReset()
    {
        _resetCts?.Cancel();
        _resetCts?.Dispose();

        var cts = _resetCts = new CancellationTokenSource();

        // Task.Delay takes none longer than int.MaxValue milliseconds (a non-positive one never gets here).
        var delay = TimeSpan.FromMilliseconds(Math.Min(CopiedDuration.TotalMilliseconds, int.MaxValue - 1));

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
            _copiedValue = null;

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
