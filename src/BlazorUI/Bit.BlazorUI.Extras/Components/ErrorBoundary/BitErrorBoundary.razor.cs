using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI;

/// <summary>
/// BitErrorBoundary catches the exceptions thrown by the components it wraps and renders an error UI
/// in their place instead of letting the exception tear down the whole app.
/// </summary>
/// <remarks>
/// It builds on Blazor's own <see cref="ErrorBoundaryBase"/>, so everything that applies to the
/// framework's <c>ErrorBoundary</c> applies here: it catches what is thrown while the components inside
/// it render, run their lifecycle methods, or handle an event, and it does NOT catch what never reaches
/// the renderer - a fire-and-forget task, a timer, a JS callback. <see cref="Capture(Exception)"/> is
/// the way in for those.
/// <br />
/// On top of that it renders a complete, themed error UI (icon, title, message, optional exception
/// details and a Refresh / Home / Recover footer), logs through the same
/// <see cref="IErrorBoundaryLogger"/> the framework's boundary logs through, can clear itself when the
/// reader navigates (<see cref="RecoverOnNavigation"/>) or when the state behind the error changes
/// (<see cref="RecoverKeys"/>), and cascades itself to its content so that any descendant can hand it
/// an exception.
/// </remarks>
public partial class BitErrorBoundary : ErrorBoundaryBase, IDisposable
{
    private BitDir? _dir;
    private bool _isDisposed;
    private object?[]? _recoverKeys;
    private Exception? _capturedException;
    private BitErrorBoundaryContext? _context;

    [Inject] private NavigationManager _navigationManager { get; set; } = default!;

    [Inject] private IServiceProvider _serviceProvider { get; set; } = default!;



    /// <summary>
    /// Gets or sets the component direction to be cascaded from an ancestor component.
    /// </summary>
    [CascadingParameter] protected BitDir? CascadingDir { get; set; }



    /// <summary>
    /// The extra content of the footer of the boundary's default error UI, rendered after the
    /// Refresh, Home and Recover buttons.
    /// </summary>
    /// <remarks>
    /// This is what adds a button to the default footer; <see cref="Footer"/> is what replaces the
    /// footer altogether, and a boundary that sets it never renders these.
    /// </remarks>
    [Parameter] public RenderFragment? AdditionalButtons { get; set; }

    /// <summary>
    /// Moves the browser focus to the error UI as it appears.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The error UI already announces itself to a screen reader as an assertive live region, so this is
    /// for the case where the reader should also be carried to it - a boundary around a whole page,
    /// where whatever had the focus is gone.
    /// </remarks>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// Alias of the ChildContent: the content the boundary renders and watches over while it has caught
    /// nothing. Takes precedence over ChildContent where both are set.
    /// </summary>
    [Parameter] public RenderFragment? Body { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the boundary's default error UI.
    /// </summary>
    [Parameter] public BitErrorBoundaryClassStyles? Classes { get; set; }

    /// <summary>
    /// The CSS class of the root element of the boundary's error UI.
    /// </summary>
    /// <remarks>
    /// A boundary that has caught nothing renders its content and no element of its own, so this - like
    /// <see cref="Style"/>, <see cref="Id"/> and the splatted HTML attributes - lands on the error UI only.
    /// </remarks>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// Gets or sets the text directionality of the boundary's error UI.
    /// </summary>
    [Parameter]
    public BitDir? Dir
    {
        get => _dir ?? CascadingDir;
        set => _dir = value;
    }

    /// <summary>
    /// The template of the error UI, receiving the caught exception along with the boundary's own
    /// Recover, Refresh and GoHome actions.
    /// </summary>
    /// <remarks>
    /// This is the inherited <c>ErrorContent</c> template with a way out of the error handed to it, and
    /// it takes precedence over <c>ErrorContent</c> where both are set.
    /// </remarks>
    [Parameter] public RenderFragment<BitErrorBoundaryContext>? ErrorTemplate { get; set; }

    /// <summary>
    /// The footer content of the boundary, replacing the default Refresh, Home and Recover buttons.
    /// </summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>
    /// Prevents rendering the Home button of the default error UI.
    /// </summary>
    [Parameter] public bool HideHomeButton { get; set; }

    /// <summary>
    /// Prevents rendering the icon of the default error UI.
    /// </summary>
    [Parameter] public bool HideIcon { get; set; }

    /// <summary>
    /// Prevents rendering the Recover button of the default error UI.
    /// </summary>
    [Parameter] public bool HideRecoverButton { get; set; }

    /// <summary>
    /// Prevents rendering the Refresh button of the default error UI.
    /// </summary>
    [Parameter] public bool HideRefreshButton { get; set; }

    /// <summary>
    /// The text of the Home button.
    /// </summary>
    [Parameter] public string? HomeText { get; set; }

    /// <summary>
    /// The url of the home page for the Home button.
    /// <br />
    /// The default value is <strong>"/"</strong>.
    /// </summary>
    [Parameter] public string? HomeUrl { get; set; }

    /// <summary>
    /// Captures the HTML attributes to be applied to the root element of the boundary's error UI.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? HtmlAttributes { get; set; }

    /// <summary>
    /// The icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The name of the icon to render in place of the built-in illustration.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.ErrorBadge</c>) of the
    /// <c>Bit.BlazorUI.Icons</c> nuget package. For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// The template of the icon, replacing both the built-in illustration and <see cref="IconName"/>.
    /// </summary>
    [Parameter] public RenderFragment? IconTemplate { get; set; }

    /// <summary>
    /// The id of the root element of the boundary's error UI.
    /// </summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>
    /// The message rendered under the title of the default error UI.
    /// </summary>
    /// <remarks>
    /// The title says that something broke; this is where the reader is told what it means for them and
    /// what to do about it. Nothing is rendered while it has no value.
    /// </remarks>
    [Parameter] public string? Message { get; set; }

    /// <summary>
    /// Prevents the boundary from logging the caught exception through the app's
    /// <see cref="IErrorBoundaryLogger"/>.
    /// </summary>
    /// <remarks>
    /// The boundary logs like the framework's own <c>ErrorBoundary</c> does, which is what puts the
    /// exception in front of a developer at all - and, in a Blazor Server app in development, in the
    /// browser console. Turn it off only where <see cref="OnError"/> already reports the exception
    /// somewhere else.
    /// </remarks>
    [Parameter] public bool NoLogging { get; set; }

    /// <summary>
    /// The callback for when an error gets caught by the boundary.
    /// </summary>
    /// <remarks>
    /// Called before the error UI is rendered, so this is where the exception is reported to whatever
    /// collects them. An exception thrown out of this callback is fatal to the boundary, exactly as one
    /// thrown out of the error UI is.
    /// </remarks>
    [Parameter] public EventCallback<Exception> OnError { get; set; }

    /// <summary>
    /// The callback for when the boundary leaves its errored state.
    /// </summary>
    /// <remarks>
    /// Called by every route back: the Recover button, <see cref="Recover"/>,
    /// <see cref="RecoverOnNavigation"/> and <see cref="RecoverKeys"/>. It is where the state that made
    /// the content throw is put right, so that recovering does not simply throw again.
    /// </remarks>
    [Parameter] public EventCallback OnRecover { get; set; }

    /// <summary>
    /// The values that recover the boundary as they change.
    /// </summary>
    /// <remarks>
    /// An errored boundary keeps showing its error UI until something clears it, and the thing that
    /// makes the content worth rendering again is usually a change somewhere else - a different record
    /// selected, a filter reset, a retry counter bumped. List those values here and the boundary
    /// recovers itself the moment any of them differs from what it last saw.
    /// </remarks>
    [Parameter] public IEnumerable<object?>? RecoverKeys { get; set; }

    /// <summary>
    /// Recovers the boundary when the reader navigates to another location.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// A boundary wrapping the body of a layout outlives the page that threw, so without this the error
    /// UI stays on screen no matter where the reader goes next. A narrowly scoped boundary has no use
    /// for it: it is torn down with the page it belongs to.
    /// </remarks>
    [Parameter] public bool RecoverOnNavigation { get; set; }

    /// <summary>
    /// The text of the Recover button.
    /// </summary>
    [Parameter] public string? RecoverText { get; set; }

    /// <summary>
    /// The text of the Refresh button.
    /// </summary>
    [Parameter] public string? RefreshText { get; set; }

    /// <summary>
    /// Whether the actual exception information should be shown or not.
    /// </summary>
    /// <remarks>
    /// This renders the exception's full text, stack trace and all. It is what a developer needs to see
    /// while building; it is also internal detail in front of whoever is looking at the screen, so it is
    /// off by default and belongs behind an environment check in a deployed app.
    /// </remarks>
    [Parameter] public bool ShowException { get; set; }

    /// <summary>
    /// The CSS style of the root element of the boundary's error UI.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the boundary's default error UI.
    /// </summary>
    [Parameter] public BitErrorBoundaryClassStyles? Styles { get; set; }

    /// <summary>
    /// The header title of the boundary.
    /// <br />
    /// The default value is <strong>"Oops, Something went wrong..."</strong>.
    /// </summary>
    [Parameter] public string? Title { get; set; }



    /// <summary>
    /// The exception the boundary is currently showing, or null while it has caught nothing.
    /// </summary>
    /// <remarks>
    /// The public reading of the inherited <c>CurrentException</c>, so that a page holding a reference to
    /// the boundary can tell whether it is errored without having to derive from it.
    /// </remarks>
    public Exception? CaughtException => CurrentException;

    /// <summary>
    /// Hands the boundary an exception that never passed through the renderer, putting it into exactly
    /// the state a caught one would.
    /// </summary>
    /// <remarks>
    /// A boundary only ever sees what the renderer routes to it, which leaves out everything thrown
    /// where nothing is awaiting it: a fire-and-forget task, a timer callback, a JS interop callback, an
    /// exception a page swallowed in a try/catch and wants shown. Pass any of those here and the
    /// boundary handles it as its own - logging it, raising <see cref="OnError"/>, counting it against
    /// <see cref="ErrorBoundaryBase.MaximumErrorCount"/> and rendering the error UI.
    /// <br />
    /// This has to be called on the renderer's synchronization context, which is where a component's own
    /// code already runs. Call <see cref="CaptureAsync(Exception)"/> from anywhere else.
    /// </remarks>
    /// <param name="exception">The exception the boundary should show.</param>
    public void Capture(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (_isDisposed) return;

        // An errored boundary handed another exception keeps the one the reader is looking at: the base
        // class treats a second error as fatal, and swapping it in would lose the first. The same goes
        // for one that is already holding a capture it has not thrown yet.
        if (CurrentException is not null || _capturedException is not null) return;

        _capturedException = exception;

        StateHasChanged();
    }

    /// <summary>
    /// <see cref="Capture(Exception)"/> from a thread that is not the renderer's.
    /// </summary>
    /// <param name="exception">The exception the boundary should show.</param>
    public Task CaptureAsync(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return _isDisposed ? Task.CompletedTask : InvokeAsync(() => Capture(exception));
    }

    /// <summary>
    /// Navigates to <see cref="HomeUrl"/>, which is what the default UI's Home button does.
    /// </summary>
    public void GoHome()
    {
        _navigationManager.NavigateTo(HomeUrl ?? "/", forceLoad: true);
    }

    /// <summary>
    /// Clears the error and renders the boundary's content again, raising <see cref="OnRecover"/>.
    /// </summary>
    /// <remarks>
    /// This hides <see cref="ErrorBoundaryBase.Recover"/> so that everything the boundary added on top of
    /// it - a pending <see cref="Capture(Exception)"/>, the <see cref="OnRecover"/> callback - is cleared
    /// with it. Never call it from rendering logic: the content is rendered again from scratch, so a
    /// recovery that does not first put right whatever threw simply throws again.
    /// </remarks>
    public new void Recover()
    {
        if (RecoverCore() is false) return;

        _ = OnRecover.InvokeAsync();
    }

    /// <summary>
    /// Reloads the current page in the browser, which is what the default UI's Refresh button does.
    /// </summary>
    public void Refresh()
    {
        _navigationManager.Refresh(forceReload: true);
    }



    protected override void OnInitialized()
    {
        _navigationManager.LocationChanged += HandleLocationChanged;

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        var keys = RecoverKeys?.ToArray();

        // The first pass has nothing to compare against: a boundary has caught nothing yet when its
        // parameters are first set, so taking the initial keys for a change would recover nothing anyway.
        if (_recoverKeys is not null && keys is not null && keys.SequenceEqual(_recoverKeys) is false)
        {
            RecoverOnKeysChanged();
        }

        _recoverKeys = keys;

        base.OnParametersSet();
    }

    protected override async Task OnErrorAsync(Exception exception)
    {
        if (NoLogging is false)
        {
            // Registered by every Blazor host, and by nothing at all in a bare test renderer, so it is
            // resolved rather than injected: a boundary that cannot log is still a boundary.
            var logger = (IErrorBoundaryLogger?)_serviceProvider.GetService(typeof(IErrorBoundaryLogger));

            if (logger is not null)
            {
                try
                {
                    await logger.LogErrorAsync(exception);
                }
                catch (Exception)
                {
                    // A logger that throws is not a reason to lose the error UI, which is the one thing
                    // standing between the reader and a torn-down app.
                }
            }
        }

        await OnError.InvokeAsync(exception);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed || disposing is false) return;

        _navigationManager.LocationChanged -= HandleLocationChanged;

        _isDisposed = true;
    }



    /// <summary>The class list of the error UI's root element.</summary>
    private string _RootClass => string.Join(' ', new[]
    {
        "bit-erb",
        Dir == BitDir.Rtl ? "bit-rtl" : null,
        Classes?.Root,
        Class
    }.Where(c => c.HasValue()));

    /// <summary>The style declarations of the error UI's root element.</summary>
    private string? _RootStyle => JoinStyles(Styles?.Root, Style);

    /// <summary>
    /// The value of an attribute the page wrote as a plain HTML attribute rather than as a parameter.
    /// </summary>
    /// <remarks>
    /// HTML attribute names are case insensitive, and so is the deduplication the render tree does
    /// between a splatted attribute and one the component writes itself, so a differently cased spelling
    /// has to be found here too. This is what lets the error UI resolve its own value against the
    /// splatted one instead of writing a null over it - a null written over a splatted attribute does not
    /// leave that attribute alone, it removes it.
    /// </remarks>
    private string? _Splat(string name)
    {
        if (HtmlAttributes is null || HtmlAttributes.Count == 0) return null;

        if (HtmlAttributes.TryGetValue(name, out var value)) return Stringify(value);

        foreach (var attribute in HtmlAttributes)
        {
            if (string.Equals(attribute.Key, name, StringComparison.OrdinalIgnoreCase)) return Stringify(attribute.Value);
        }

        return null;

        // A boolean is the one attribute value the renderer does not write as its text: an attribute is
        // written with no value at all while it is true and left out altogether while it is false.
        static string? Stringify(object? value) => value is bool boolean ? (boolean ? string.Empty : null) : value?.ToString();
    }

    /// <summary>The heading the error UI carries, which an explicitly empty Title drops.</summary>
    private string? _Title => Title ?? "Oops, Something went wrong...";

    /// <summary>The glyph the error UI draws in place of its own illustration, or null where it draws it.</summary>
    private BitIconInfo? _Icon => BitIconInfo.From(Icon, IconName);

    /// <summary>Whether anything is left for the default footer to hold.</summary>
    private bool _HasFooter => AdditionalButtons is not null
                            || HideRefreshButton is false
                            || HideHomeButton is false
                            || HideRecoverButton is false;

    /// <summary>
    /// The context an <see cref="ErrorTemplate"/> is handed, rebuilt only as the exception it carries
    /// changes so that a template bound to it is not handed a new object on every render.
    /// </summary>
    private BitErrorBoundaryContext _Context
    {
        get
        {
            if (_context is null || ReferenceEquals(_context.Exception, CurrentException) is false)
            {
                _context = new BitErrorBoundaryContext(CurrentException!, Recover, Refresh, GoHome);
            }

            return _context;
        }
    }

    /// <summary>
    /// The exception <see cref="Capture(Exception)"/> left behind, taken rather than read: the render it
    /// is returned to is the one that hands it to the thrower, and leaving it in place would throw it
    /// again on the first render after a recovery.
    /// </summary>
    private Exception? TakeCapturedException()
    {
        var exception = _capturedException;

        _capturedException = null;

        return exception;
    }

    /// <summary>
    /// The thrower, built by hand rather than written as a tag: the Razor compiler only matches a tag
    /// against the component types an assembly makes public, and this one is nobody's business but the
    /// boundary's.
    /// </summary>
    private static RenderFragment RenderThrower(Exception exception) => builder =>
    {
        builder.OpenComponent<_BitErrorBoundaryThrower>(0);
        builder.AddComponentParameter(1, nameof(_BitErrorBoundaryThrower.Exception), exception);
        builder.CloseComponent();
    };

    private static string? JoinStyles(string? style, string? extraStyle)
    {
        if (style.HasNoValue()) return extraStyle;

        if (extraStyle.HasNoValue()) return style;

        return style!.TrimEnd().EndsWith(';') ? $"{style} {extraStyle}" : $"{style};{extraStyle}";
    }

    /// <summary>
    /// Clears the errored state, reporting whether there was one to clear.
    /// </summary>
    private bool RecoverCore()
    {
        var wasErrored = CurrentException is not null;
        var wasCaptured = _capturedException is not null;

        _capturedException = null;

        // Renders on its own only when it had an exception to clear, which is why the captured-but-not-
        // yet-thrown case has to ask for the render itself.
        base.Recover();

        if (wasErrored is false && wasCaptured)
        {
            StateHasChanged();
        }

        return wasErrored || wasCaptured;
    }

    private async Task HandleRecover()
    {
        if (RecoverCore() is false) return;

        await OnRecover.InvokeAsync();
    }

    private void RecoverOnKeysChanged()
    {
        if (CurrentException is null && _capturedException is null) return;

        _ = HandleRecover();
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        if (RecoverOnNavigation is false) return;

        if (CurrentException is null && _capturedException is null) return;

        // LocationChanged is raised off the renderer's synchronization context in a Blazor Server app.
        _ = InvokeAsync(HandleRecover);
    }
}
