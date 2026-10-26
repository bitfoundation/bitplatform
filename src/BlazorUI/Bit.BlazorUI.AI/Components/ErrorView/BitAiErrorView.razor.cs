using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiErrorView shows an error the user can act on: what went wrong, a retry when retrying could help, and the
/// technical detail behind a disclosure rather than in their face.
/// </summary>
/// <remarks>
/// The error is drawn as an Error <see cref="BitMessage"/>, which is what announces it to a screen reader as an
/// alert, and restyled through the <c>--bit-Message-*</c> variables (or <see cref="BitAiErrorViewClassStyles.Message"/>).
/// The retry is offered only when the error says retrying could help and a handler is there to do it, and the wait the
/// error asks for (<see cref="BitAiError.RetryAfter"/>) is shown until it is over. The detail is disclosed under the
/// actions, after the button that shows it.
/// </remarks>
public partial class BitAiErrorView : BitAiComponentBase
{
    private bool _showDetail;
    private BitAiIconClassesCache<BitButtonClassStyles> _retryButtonClasses;

    // The wait before a retry (see SyncWait): the error it was timed for, the one-shot timer ending it, and whether it is
    // over - set on the timer's thread, read as the view renders.
    private readonly object _gate = new();
    private BitAiError? _waitError;
    private System.Threading.Timer? _waitTimer;
    private volatile bool _waitOver;



    /// <summary>
    /// Gets or sets the cascading parameters for the error view component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple error view components
    /// through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiErrorViewParams.ParamName)]
    public BitAiErrorViewParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the error view.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitAiErrorViewClassStyles? Classes { get; set; }

    /// <summary>
    /// Renders the error inside another card, titled by its own message instead of a generic heading.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    [Parameter] public bool Compact { get; set; }

    /// <summary>
    /// The failure to show.
    /// </summary>
    [Parameter, EditorRequired]
    public BitAiError Error { get; set; } = default!;

    /// <summary>
    /// Raised when the user asks to retry. The retry button is hidden while it is unset, and while the error says
    /// retrying would not help.
    /// </summary>
    [Parameter] public EventCallback OnRetry { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the error view.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitAiErrorViewClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aierv";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiErrorViewParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        SyncWait();

        base.OnParametersSet();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        lock (_gate)
        {
            _waitTimer?.Dispose();
            _waitTimer = null;
        }

        await base.DisposeAsync(disposing);
    }



    private string _DetailId => $"{_Id}-dtl";

    /// <summary>
    /// Times the wait the error asks for before a retry, from when the view is handed the error, and takes the line saying
    /// it away once it is over: "Try again in 30s" left on screen a minute later says nothing true. It is not counted
    /// down, since the line is part of the alert the message is, which would announce every second of it.
    /// </summary>
    private void SyncWait()
    {
        // An error equal to the one being timed is the same error handed over again (a record built anew on a render of
        // the parent), so its wait goes on rather than starting over.
        if (Equals(_waitError, Error)) return;

        lock (_gate)
        {
            _waitTimer?.Dispose();
            _waitTimer = null;

            var error = Error;
            var wait = error?.RetryAfter ?? TimeSpan.Zero;
            _waitError = error;
            _waitOver = wait <= TimeSpan.Zero;

            // A wait longer than a timer can be set for (about 49 days) is left on screen as it is.
            if (_waitOver is false && wait.TotalMilliseconds < uint.MaxValue - 1 && IsDisposing is false)
            {
                _waitTimer = new System.Threading.Timer(_ => EndWait(error!), null, wait, Timeout.InfiniteTimeSpan);
            }
        }
    }

    // Raised on the timer's thread, so it only asks for a render. A wait timed for an error the view has since been
    // handed another one in place of ends nothing.
    private void EndWait(BitAiError error)
    {
        lock (_gate)
        {
            if (ReferenceEquals(error, _waitError) is false) return;

            _waitOver = true;
            _waitTimer?.Dispose();
            _waitTimer = null;
        }

        RequestRender();
    }

    private async Task HandleOnRetry()
    {
        if (Disabled) return;

        await OnRetry.InvokeAsync();
    }

    // Refresh is not one of the glyphs of the core font, so the retry button draws it from the subset font of this
    // package (see BitAiIconClasses), and is handed the same classes while the app's own stay the same.
    private BitButtonClassStyles _RetryButtonClasses =>
        _retryButtonClasses.Get("bit-icon-ai bit-icon-ai--Refresh", Classes?.RetryButton, BitAiIconClasses.With);

    private void ToggleDetail()
    {
        if (Disabled) return;

        _showDetail = _showDetail is false;
    }
}
