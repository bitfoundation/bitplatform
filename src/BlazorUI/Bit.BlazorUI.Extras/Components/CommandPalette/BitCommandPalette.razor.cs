using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bit.BlazorUI;

/// <summary>
/// An accessible, keyboard-first command palette - the <c>Ctrl+K</c> / <c>Cmd+K</c> box that searches every page, action
/// and setting of an app and runs the one picked. Place it once in a layout. Commands come from
/// <see cref="IBitCommandPaletteService"/>, from declarative <see cref="BitCommandPaletteOption"/> children, from
/// <see cref="Commands"/> and from async sources.
/// </summary>
/// <remarks>
/// Matching, ranking, paging, history and every other decision are made in C#; the browser layer only opens the native
/// modal <c>&lt;dialog&gt;</c>, moves the focus in and out of it, scrolls the active row into view and listens for keys,
/// so typing anywhere on the page never costs a round trip. <see cref="Inline"/> renders it in the page instead.
/// </remarks>
public partial class BitCommandPalette : BitComponentBase
{
    private const string ModeHintTag = "__bit_cpl_mode_hint";
    private const string SourceErrorTag = "__bit_cpl_source_error";
    private const string SourceMoreTag = "__bit_cpl_source_more";

    /// <summary>How long a handler runs before the palette shows it is busy: long enough that a quick one never flickers.</summary>
    private static readonly TimeSpan BusyDelay = TimeSpan.FromMilliseconds(150);

    /// <summary>
    /// How long a toast shows before the palette it closes goes away: long enough to read a short confirmation. Tests
    /// lengthen it, so that nothing they do lands after the close by chance.
    /// </summary>
    internal TimeSpan ToastCloseDelay { get; set; } = TimeSpan.FromMilliseconds(900);

    private readonly List<BitCommandPaletteCommand> _declarative = [];
    private readonly List<BitCommandPalettePageFrame> _pages = [];
    private readonly Dictionary<string, IReadOnlyList<BitCommandPaletteCommand>> _sourceResults = new(StringComparer.Ordinal);
    private readonly HashSet<string> _pendingSources = new(StringComparer.Ordinal);
    private readonly HashSet<string> _sourceErrors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (string Cursor, int? Total)> _sourceCursors = new(StringComparer.Ordinal);

    private ElementReference _inputEl;
    private ElementReference _listEl;
    private IJSObjectReference? _handle;
    private DotNetObjectReference<BitCommandPalette>? _selfRef;
    // Set first thing by DisposeAsync: the first render's calls into the browser layer check it as each one returns.
    private bool _disposed;
    // Whether the page behind the modal dialog is held still (BitUtilsLockScroll), which closing or disposing gives back.
    private bool _scrollLocked;
    // The id the scroll lock was taken under, so it is released under the same one even if Id changed meanwhile.
    private string? _scrollLockKey;

    private bool _isOpen;
    private bool _pendingOpen;
    // The Open parameter as the parent last passed it. The palette acts on a change of it, never on its value alone: a
    // parent rendering again while the palette opens or closes itself - from its shortcut, a trigger, the service -
    // passes the value it held before OpenChanged reached it, which must neither close what just opened nor reopen
    // what the user just dismissed.
    private bool? _openParameter;
    private bool _selectOnOpen;
    private bool _pendingFocus;
    private bool _pendingScroll;
    private bool _isMac;
    private string _query = string.Empty;
    private int _activeIndex;
    private bool _sourcesLoading;
    private string? _confirmingId;
    private string? _toast;
    private bool _toastIsError;
    private bool _announcePage;
    private bool _inlineFocused;
    private string? _activateId;
    private BitCommandPaletteKeyboardShortcut? _pendingSequence;
    private ILogger? _logger;
    private readonly HashSet<string> _reportedShortcutProblems = new(StringComparer.Ordinal);
    private string _announcement = string.Empty;
    private CancellationTokenSource? _debounceCts;
    private CancellationTokenSource? _executeCts;
    private CancellationTokenSource? _toastCts;
    // The close a toast scheduled (see ApplyResultAsync). Whatever the user does in the palette meanwhile calls it off:
    // opening or closing it, typing, a key, a command - from a click, a shortcut or code - and a page change.
    private CancellationTokenSource? _autoCloseCts;
    // The guards deciding about what the user chose in the palette (see MayRunAsync): it turns a second Enter away and
    // shows busy, as a running handler does, but is not one - navigating still closes the palette, and code and shortcuts
    // still run. Closing the palette cancels it.
    private CancellationTokenSource? _decisionCts;
    // Cancelled on disposal: what guards decide about a shortcut or code, which closing the palette leaves alone.
    private readonly CancellationTokenSource _lifetimeCts = new();
    // The registered IBitCommandPaletteCommandGuards, resolved once: they are scoped, as the palette is.
    private IBitCommandPaletteCommandGuard[]? _registeredGuards;
    private bool _executing;
    private bool _busy;
    private string? _runningId;
    private string? _promptError;
    private bool _pointerSelectionSuspended;
    private int _hoverSuspension;
    private bool _selectionReset;
    private bool _syncedVim;
    private string _syncedToggles = string.Empty;
    private string _syncedShortcuts = string.Empty;
    private bool _inputPending;
    private int _inputVersion;
    private TimeSpan _lastSearchCost;

    private List<BitCommandPaletteCommand> _all = [];
    // The snapshot the shortcut problems were last looked for in.
    private List<BitCommandPaletteCommand>? _warnedSnapshot;
    private int _declarativeVersion;
    private (int Registry, int Declarative, IEnumerable<BitCommandPaletteCommand>? Parameter)? _snapshotKey;
    private Dictionary<string, BitCommandPaletteCommand> _allById = new(StringComparer.Ordinal);
    private HashSet<string> _parentIds = new(StringComparer.Ordinal);
    private bool _approximate;
    private IEnumerable<BitCommandPaletteCommand>? _lastCommandsParameter;
    private List<BitCommandPaletteRow> _rows = [];
    private List<BitCommandPaletteRow> _itemRows = [];
    private BitCommandPaletteCommandMode? _mode;
    private string _effectiveQuery = string.Empty;
    private List<BitCommandPaletteMatch> _registryMatches = [];
    private (BitCommandPalettePageFrame? Page, BitCommandPaletteCommandMode? Mode, string? Query) _registryMatchKey;

    [Inject] private IJSRuntime _js { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IServiceProvider Services { get; set; } = default!;

    /// <summary>
    /// The default values of the parameters of the command palette, cascaded through a <see cref="BitParams"/>. A value set
    /// on the command palette itself wins over it.
    /// </summary>
    [CascadingParameter(Name = BitCommandPaletteParams.ParamName)]
    public BitCommandPaletteParams? CascadingParameters { get; set; }

    // Resolved rather than injected, so the palette also works when the app never called AddBitCommandPaletteServices. Null
    // until first used: DisposeAsync leaves alone what was never resolved.
    private BitCommandPaletteRuntime? _runtime;
    private BitCommandPaletteRuntime Runtime => _runtime ??= BitCommandPaletteRuntime.Of(Services);
    private BitCommandPaletteService Service => Runtime.Service;
    private BitCommandPaletteOptions Options => Runtime.Options;
    private BitCommandPaletteRanker Ranker => Runtime.Ranker;
    private BitCommandPaletteHistory History => Runtime.History;

    /// <summary>
    /// The declarative <see cref="BitCommandPaletteGroup"/> and <see cref="BitCommandPaletteOption"/> children of the palette.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for the different parts of the command palette.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitCommandPaletteClassStyles? Classes { get; set; }

    /// <summary>
    /// Commands supplied directly to this palette, in addition to the ones registered through
    /// <see cref="IBitCommandPaletteService"/> and declared in its markup.
    /// </summary>
    [Parameter] public IEnumerable<BitCommandPaletteCommand>? Commands { get; set; }

    /// <summary>
    /// Renders a detail pane beside the list for the active command, Raycast style.
    /// </summary>
    [Parameter] public RenderFragment<BitCommandPaletteCommand>? DetailTemplate { get; set; }

    /// <summary>
    /// Ignores the pointer hovering a row for choosing the active one: only the keys and a click move it.
    /// </summary>
    [Parameter] public bool DisablePointerSelection { get; set; }

    /// <summary>
    /// What a page with no commands on it at all shows, in place of <see cref="EmptyText"/>.
    /// </summary>
    [Parameter] public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>
    /// The text a page with no commands on it at all shows. Default: the <see cref="BitCommandPaletteStrings.Empty"/> of the
    /// palette's strings, which falls back to <see cref="BitStrings.NoItemsFound"/>.
    /// </summary>
    [Parameter] public string? EmptyText { get; set; }

    /// <summary>
    /// Overrides <see cref="BitCommandPaletteOptions.EnableHistory"/>: recents, pins and frecency.
    /// </summary>
    [Parameter] public bool? EnableHistory { get; set; }

    /// <summary>
    /// Replaces the footer with its keyboard hints and toasts, with a snapshot of the palette's state.
    /// </summary>
    [Parameter] public RenderFragment<BitCommandPaletteStateContext>? FooterTemplate { get; set; }

    /// <summary>
    /// Replaces the content of a group heading; receives the heading text.
    /// </summary>
    [Parameter] public RenderFragment<string>? GroupHeaderTemplate { get; set; }

    /// <summary>
    /// Overrides <see cref="BitCommandPaletteOptions.GroupWhenSearching"/>: keeps the group headings while a query is typed.
    /// </summary>
    [Parameter] public bool? GroupWhenSearching { get; set; }

    /// <summary>
    /// Content rendered above the search box, with a snapshot of the palette's state.
    /// </summary>
    [Parameter] public RenderFragment<BitCommandPaletteStateContext>? HeaderTemplate { get; set; }

    /// <summary>
    /// Overrides <see cref="BitCommandPaletteOptions.HighlightMatches"/>: marks the matched characters.
    /// </summary>
    [Parameter] public bool? HighlightMatches { get; set; }

    /// <summary>
    /// Renders the icon of every row; receives the command. Render nothing for no icon.
    /// </summary>
    [Parameter] public RenderFragment<BitCommandPaletteCommand>? IconTemplate { get; set; }

    /// <summary>
    /// Whether the commands, sources and modes registered through <see cref="IBitCommandPaletteService"/> are part of this
    /// palette. Set it to false for a palette that lists only its own - the ones declared in its markup and passed to
    /// <see cref="Commands"/> - such as an inline search of one page's settings. Default: true.
    /// </summary>
    [Parameter] public bool IncludeRegisteredCommands { get; set; } = true;

    /// <summary>
    /// Renders the palette in the page instead of in a modal dialog - a settings search, a docs search page, a command
    /// list in a side panel. It is always open, Escape goes back a page or clears the query rather than closing, and a
    /// command that would close the palette resets it to its root page. It leaves the dialog's part to the modal palette
    /// a layout usually has as well: it neither opens on the toggle shortcut nor answers
    /// <see cref="IBitCommandPaletteService.OpenAsync"/>, <see cref="IBitCommandPaletteService.CloseAsync"/> or
    /// <see cref="IBitCommandPaletteService.ExecuteAsync"/>, and claims no global shortcuts - so nothing runs twice.
    /// Set before the first render; changing it later is not supported.
    /// </summary>
    [Parameter, ResetClassBuilder] public bool Inline { get; set; }

    /// <summary>
    /// Replaces the content of every row. The row element itself - its role, ARIA state, data attributes and click
    /// handling - stays the palette's.
    /// </summary>
    [Parameter] public RenderFragment<BitCommandPaletteItemContext>? ItemTemplate { get; set; }

    /// <summary>
    /// Replaces the loading state shown while a lazily loaded page or an async source has nothing to show yet.
    /// </summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// Overrides <see cref="BitCommandPaletteOptions.Loop"/>: the arrow keys wrap around the list.
    /// </summary>
    [Parameter] public bool? Loop { get; set; }

    /// <summary>
    /// What a search that matched nothing shows, in place of <see cref="NoResultsText"/>; receives the query.
    /// </summary>
    [Parameter] public RenderFragment<string>? NoResultsTemplate { get; set; }

    /// <summary>
    /// The text a search that matched nothing shows. Default: the <see cref="BitCommandPaletteStrings.NoResults"/> of the
    /// palette's strings, which falls back to <see cref="BitStrings.NoResultsFound"/>.
    /// </summary>
    [Parameter] public string? NoResultsText { get; set; }

    /// <summary>
    /// Raised after the palette closed.
    /// </summary>
    [Parameter] public EventCallback OnClosed { get; set; }

    /// <summary>
    /// Raised after a command of this palette ran, with its result and how long the handler took.
    /// </summary>
    [Parameter] public EventCallback<BitCommandPaletteCommandExecutedEventArgs> OnCommandExecuted { get; set; }

    /// <summary>
    /// Raised after the palette opened.
    /// </summary>
    [Parameter] public EventCallback OnOpened { get; set; }

    /// <summary>
    /// Raised after every change of the query, with the raw text (a mode prefix included).
    /// </summary>
    [Parameter] public EventCallback<string> OnQueryChanged { get; set; }

    /// <summary>
    /// Whether the modal palette is open (two-way bindable).
    /// </summary>
    [Parameter, TwoWayBound] public bool Open { get; set; }

    /// <summary>
    /// The placeholder of the search box on the root page.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// Overrides <see cref="BitCommandPaletteOptions.SearchNestedCommands"/>: finds nested commands from the root page.
    /// </summary>
    [Parameter] public bool? SearchNestedCommands { get; set; }

    /// <summary>
    /// Overrides <see cref="BitCommandPaletteOptions.ShowFooter"/>.
    /// </summary>
    [Parameter] public bool? ShowFooter { get; set; }

    /// <summary>
    /// Overrides <see cref="BitCommandPaletteOptions.ShowShortcuts"/>: shows each command's shortcut on its row.
    /// </summary>
    [Parameter] public bool? ShowShortcuts { get; set; }

    /// <summary>
    /// The words of the palette. Default: <see cref="BitCommandPaletteOptions.Strings"/>.
    /// </summary>
    [Parameter] public BitCommandPaletteStrings? Strings { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the command palette.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitCommandPaletteClassStyles? Styles { get; set; }

    /// <summary>
    /// Overrides <see cref="BitCommandPaletteOptions.ToggleShortcut"/>, e.g. <c>Ctrl+Shift+P</c>.
    /// </summary>
    [Parameter] public string? ToggleShortcut { get; set; }

    /// <summary>
    /// Overrides <see cref="BitCommandPaletteOptions.TypoTolerance"/>: retries a query that found nothing allowing a typo
    /// per term.
    /// </summary>
    [Parameter] public bool? TypoTolerance { get; set; }

    /// <summary>
    /// Overrides <see cref="BitCommandPaletteOptions.VimBindings"/>: <c>Ctrl+J</c> / <c>Ctrl+K</c> move the selection.
    /// </summary>
    [Parameter] public bool? VimBindings { get; set; }

    /// <summary>
    /// Overrides <see cref="BitCommandPaletteOptions.Virtualize"/>: renders the list through <c>&lt;Virtualize&gt;</c>.
    /// </summary>
    [Parameter] public bool? Virtualize { get; set; }



    // ---- Public API --------------------------------------------------------------------------

    /// <summary>Whether the dialog is open.</summary>
    public bool IsOpen => _isOpen;

    /// <summary>Current raw query (including a mode prefix).</summary>
    public string Query => _query;

    /// <summary>Currently highlighted command, if any.</summary>
    public BitCommandPaletteCommand? ActiveCommand => _activeIndex >= 0 && _activeIndex < _itemRows.Count ? _itemRows[_activeIndex].Command : null;

    /// <summary>Opens the palette, optionally with a query (a mode prefix is honoured) or directly on a command's page.</summary>
    public Task OpenAsync(string? query = null, string? pageCommandId = null) => OpenCoreAsync(query, pageCommandId);

    /// <summary>Closes the palette.</summary>
    public Task CloseAsync() => CloseCoreAsync();

    /// <summary>Opens the palette when it is closed and closes it when it is open.</summary>
    public Task ToggleAsync() => _isOpen ? CloseCoreAsync() : OpenCoreAsync(null, null);

    /// <summary>Sets the query programmatically, as if typed, and renders the results. Safe to call from any thread.</summary>
    public Task SetQueryAsync(string query) => InvokeAsync(async () =>
    {
        _query = query ?? string.Empty;
        await OnQueryUpdatedAsync();
        // Called from code rather than from one of this component's events, so nothing else renders it - as with
        // OpenAsync and CloseAsync. The box shows the new query: it differs from what the browser last sent.
        StateHasChanged();
    });

    // ---- Lifecycle ---------------------------------------------------------------------------

    protected override string RootElementClass => "bit-cpl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Inline ? "bit-cpl-inl" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        // The root page comes first: registering the DI sources below raises CommandsChanged, which rebuilds right
        // away on the renderer's thread and needs a current page.
        _pages.Add(new BitCommandPalettePageFrame { Title = S.Home });

        // An inline palette leaves opening, closing and running commands by id to the modal one: answering too would
        // run a command twice.
        if (!Inline)
        {
            Service.OpenRequested += OpenFromServiceAsync;
            Service.CloseRequested += CloseFromServiceAsync;
            Service.ExecuteRequested += ExecuteFromServiceAsync;
        }

        _isOpen = Inline;
        Service.CommandsChanged += OnRegistryChanged;
        Service.RefreshRequested += OnRefreshRequested;
        History.Changed += OnHistoryChanged;
        Navigation.LocationChanged += OnLocationChanged;

        // The sources registered in DI belong to the scope, like the service: they are added once and never removed with a
        // palette, so a page's palette that happened to start first cannot take them from the layout's when it goes.
        foreach (var source in IncludeRegisteredCommands ? Services.GetService(typeof(IEnumerable<IBitCommandPaletteSource>)) as IEnumerable<IBitCommandPaletteSource> ?? [] : [])
        {
            if (!Service.Sources.Contains(source))
            {
                Service.RegisterSource(source);
            }
        }

        Rebuild();

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!ReferenceEquals(_lastCommandsParameter, Commands))
        {
            _lastCommandsParameter = Commands;
            Rebuild();
        }

        SyncBrowserOptions();
        SyncShortcuts();

        if (Inline)
        {
            return;
        }

        // A trigger shows and announces the shortcut and the strings of the palette it opens, not the options.
        Service.SetModalPalette(this, EffectiveToggleShortcut, S);

        var openChanged = _openParameter != Open;
        _openParameter = Open;

        if (openChanged is false)
        {
            return;
        }

        if (Open && !_isOpen)
        {
            await OpenCoreAsync(null, null);
        }
        else if (!Open && _isOpen && OpenChanged.HasDelegate)
        {
            await CloseCoreAsync();
        }
    }

    // A keystroke whose search is still deferred (see OnInputAsync) renders together with its results.
    protected override bool ShouldRender() => !_inputPending;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            await StartBrowserLayerAsync();

            // Each await of the start can outlast the palette - a page left while the module downloads, or while init
            // crosses a Blazor Server connection - and DisposeAsync releases only what was there when it ran: whatever
            // arrived after it is released here, the one place that does, whichever await the start stopped at.
            if (_disposed)
            {
                await ReleaseBrowserLayerAsync();
                return;
            }
        }

        if (firstRender && Inline)
        {
            // Never opened, always open: what opening does for the dialog - history for recents, the sources an empty
            // query asks - starts here, once the page runs in the browser.
            if (EffectiveEnableHistory)
            {
                _ = History.EnsureLoadedAsync();
            }

            TriggerSourceSearch();
        }

        if (_handle is null)
        {
            return;
        }

        if (_pendingOpen)
        {
            _pendingOpen = false;
            // Closed again before this render: showing the dialog now would leave an empty modal nothing dismisses.
            if (_isOpen)
            {
                await TryInvokeAsync("open", _inputEl, _selectOnOpen);

                // A close that ran while the browser was opening the dialog has already unlocked, finding nothing locked:
                // locking now would leave the page unable to scroll under a closed palette.
                if (_isOpen)
                {
                    await LockScrollAsync();
                    _pendingScroll = true;
                }
            }
        }

        if (_pendingFocus)
        {
            _pendingFocus = false;
            // A command run from a shortcut or from code can ask for focus (KeepOpen, Reset) while the palette is
            // closed, and the input only exists while it is open.
            if (_isOpen)
            {
                await TryInvokeAsync("focusInput", _inputEl);
            }
        }

        if (_pendingScroll && _isOpen)
        {
            _pendingScroll = false;
            if (_activeIndex >= 0 && _activeIndex < _itemRows.Count)
            {
                // The row's position lets the browser layer reach a row Virtualize has not rendered.
                var active = _itemRows[_activeIndex];
                await TryInvokeAsync("scrollIntoView", _listEl, active.ElementId, active.Position, _rows.Count);
            }
        }
    }

    /// <summary>
    /// Loads the browser layer and starts it on the dialog. A palette disposed meanwhile stops at the next await, and
    /// <see cref="OnAfterRenderAsync"/> releases what the calls brought back.
    /// </summary>
    private async Task StartBrowserLayerAsync()
    {
        try
        {
            _selfRef = DotNetObjectReference.Create(this);
            var toggles = ToggleShortcutStrings();
            var shortcuts = GlobalShortcutStrings();
            _syncedToggles = string.Join('\n', toggles);
            _syncedShortcuts = string.Join('\n', shortcuts);
            _syncedVim = EffectiveVim;
            // Plain values rather than one options object: the serializer discovers an object's shape through
            // reflection, and a trimmed app no longer has the metadata it needs for that.
            _handle = await _js.BitCommandPaletteSetup(RootElement, _selfRef,
                toggles, shortcuts, Options.SuppressShortcutsInEditableElements, Options.SequenceTimeoutMs, _syncedVim, Inline);
            if (_disposed)
            {
                return;
            }

            // Parameters set and commands registered while init was on its way.
            SyncBrowserOptions();
            SyncShortcuts();
            WarnAboutShortcutProblems();
            _isMac = await _handle.InvokeAsync<bool>("isMac");
            if (_disposed)
            {
                return;
            }

            Service.SetPlatform(_isMac);
            StateHasChanged();
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException or InvalidOperationException or NotSupportedException)
        {
            // Prerendering without JS, module blocked, circuit gone or the arguments not serializable: palette will
            // still render but without hotkeys.
        }
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        _disposed = true;
        if (_runtime is not null)
        {
            _runtime.Service.OpenRequested -= OpenFromServiceAsync;
            _runtime.Service.CloseRequested -= CloseFromServiceAsync;
            _runtime.Service.ExecuteRequested -= ExecuteFromServiceAsync;
            _runtime.Service.CommandsChanged -= OnRegistryChanged;
            _runtime.Service.RefreshRequested -= OnRefreshRequested;
            _runtime.Service.ClearModalPalette(this);
            _runtime.History.Changed -= OnHistoryChanged;
        }

        Navigation.LocationChanged -= OnLocationChanged;
        _debounceCts?.Cancel();
        _executeCts?.Cancel();
        _decisionCts?.Cancel();
        _lifetimeCts.Cancel();
        _toastCts?.Cancel();
        CancelAutoClose();
        CancelPageLoads();

        await UnlockScrollAsync();
        await ReleaseBrowserLayerAsync();

        await base.DisposeAsync(disposing);
    }

    /// <summary>
    /// Removes the browser layer's listeners and releases the references to it. Called by <see cref="DisposeAsync"/>, and
    /// by the first render for whatever its calls brought back after that: each reference is taken once, so nothing is
    /// released twice.
    /// </summary>
    private async Task ReleaseBrowserLayerAsync()
    {
        var handle = _handle;
        var selfRef = _selfRef;
        _handle = null;
        _selfRef = null;

        try
        {
            if (handle is not null)
            {
                await handle.InvokeVoidAsync("dispose");
                await handle.DisposeAsync();
            }
        }
        catch (Exception ex) when (IsInteropFailure(ex))
        {
            // The circuit or the page is already gone, and the browser layer with it.
        }

        selfRef?.Dispose();
    }

    // ---- Declarative registration (used by BitCommandPaletteOption) -------------------------------------

    internal void AddDeclarative(BitCommandPaletteCommand command)
    {
        if (IsDisposing) return;

        _declarative.RemoveAll(c => c.Id == command.Id);
        _declarative.Add(command);
        _declarativeVersion++;
        Rebuild();
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>True when selecting the command opens a nested page (static, lazy, or declaratively registered children).</summary>
    internal bool HasPage(BitCommandPaletteCommand cmd) => cmd.HasChildren || _parentIds.Contains(cmd.Id);

    internal void RemoveDeclarative(string id)
    {
        // The options of a palette going away unregister one by one; rebuilding for each would only talk to a page
        // that is being torn down.
        if (IsDisposing) return;

        if (_declarative.RemoveAll(c => c.Id == id) > 0)
        {
            _declarativeVersion++;
            Rebuild();
            _ = InvokeAsync(StateHasChanged);
        }
    }

    // ---- Service / environment callbacks --------------------------------------------------------

    private void OnRegistryChanged() => _ = InvokeAsync(() =>
    {
        Rebuild();
        StateHasChanged();
    });

    private void OnRefreshRequested() => _ = InvokeAsync(() =>
    {
        RefreshSnapshot(force: true);
        SyncShortcuts();
        Rebuild();
        StateHasChanged();
    });

    private void OnHistoryChanged() => _ = InvokeAsync(() =>
    {
        Rebuild();
        StateHasChanged();
    });

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (_isOpen && !Inline && Options.CloseOnNavigation && !_executing)
        {
            _ = InvokeAsync(CloseCoreAsync);
        }
    }

    // The service can be called from any thread - a timer, a hub callback, code after ConfigureAwait(false) - and opening
    // or closing renders, so its requests move onto the renderer's dispatcher first.
    private Task OpenFromServiceAsync(string? query, string? pageCommandId) => InvokeAsync(() => OpenCoreAsync(query, pageCommandId));

    private Task CloseFromServiceAsync() => InvokeAsync(CloseCoreAsync);

    private Task ExecuteFromServiceAsync(string commandId) => InvokeAsync(async () =>
    {
        var command = FindCommand(commandId) ?? throw new KeyNotFoundException($"Command '{commandId}' is not registered.");

        // A disabled palette runs nothing, from code either.
        if (Disabled) return;

        await ExecuteCommandAsync(command, BitCommandPaletteExecutionTrigger.Programmatic);
        StateHasChanged();
    });

    /// <summary>
    /// Looks a command up in the registry first, then among the ones declared in markup and passed to <see cref="Commands"/>:
    /// the order in which <see cref="RefreshSnapshot"/> keeps one command per id.
    /// </summary>
    private BitCommandPaletteCommand? FindCommand(string commandId)
        => (IncludeRegisteredCommands ? Service.Find(commandId) : null)
        ?? _declarative.FirstOrDefault(c => c.Id == commandId)
        ?? Commands?.FirstOrDefault(c => c.Id == commandId);

    /// <summary>
    /// The page a command with children opens. Children registered through the service are flattened into the
    /// registry with their parent set, and found there; the static children of a command that never went through
    /// the registry - declared in markup or passed to <see cref="Commands"/> - are listed from the command itself.
    /// </summary>
    private BitCommandPalettePageFrame PageFor(BitCommandPaletteCommand cmd, string queryBefore) => new()
    {
        Title = cmd.Title,
        Command = cmd,
        Items = !_parentIds.Contains(cmd.Id) && cmd.Children is { Count: > 0 } ? cmd.Children : null,
        Placeholder = cmd.ChildrenPlaceholder,
        MultiSelect = cmd.MultiSelectChildren,
        OnSubmit = cmd.Handler,
        QueryBefore = queryBefore,
        Sources = cmd.ChildSources,
        // Loading from the first render on, so the page is announced as loading rather than as empty.
        Loading = cmd.ChildrenProvider is not null,
    };

    /// <summary>Opens the palette when something that needs it - a page, a prompt, a confirmation - starts while it is closed.</summary>
    private Task EnsureOpenAsync() => _isOpen ? Task.CompletedTask : OpenCoreAsync(null, null);

    // ---- Open / close ----------------------------------------------------------------------------

    private async Task OpenCoreAsync(string? query, string? pageCommandId)
    {
        CancelAutoClose();

        if (Inline)
        {
            if (Disabled) return;

            // Always open: only the query or the page changes, and the input takes the focus.
            if (query is not null || pageCommandId is not null)
            {
                // Opening a page lists it - and loads it: its guards decide first. Looked up once, so the command pushed
                // is the one they allowed, even if the registry changed while they decided.
                var target = pageCommandId is null ? null : FindCommand(pageCommandId);
                if (target is not null && !await MayRunAsync(target, BitCommandPaletteExecutionTrigger.Programmatic))
                {
                    return;
                }

                ResetState();
                if (target is not null)
                {
                    var targetFrame = PushPageWithAncestors(target, string.Empty);
                    if (target.ChildrenProvider is not null)
                    {
                        _ = LoadChildrenAsync(targetFrame, target);
                    }
                }

                _query = query ?? _query;
                Rebuild();
                TriggerSourceSearch();
            }

            _pendingFocus = true;
            StateHasChanged();
            return;
        }

        if (_isOpen && query is null && pageCommandId is null)
        {
            return;
        }

        if (Disabled && _isOpen is false)
        {
            return;
        }

        // Opening a page lists it - and loads it: its guards decide first. Refused, the palette stays as it was. Looked up
        // once, so the command pushed is the one they allowed, even if the registry changed while they decided.
        var page = pageCommandId is null ? null : FindCommand(pageCommandId);
        if (page is not null && !await MayRunAsync(page, BitCommandPaletteExecutionTrigger.Programmatic))
        {
            return;
        }

        // Closing already resets when ResetOnClose is set; an explicit query or page always starts fresh.
        if (query is not null || pageCommandId is not null)
        {
            ResetState();
        }

        if (page is not null)
        {
            var frame = PushPageWithAncestors(page, string.Empty);
            if (page.ChildrenProvider is not null)
            {
                _ = LoadChildrenAsync(frame, page);
            }
        }

        if (query is not null)
        {
            _query = query;
        }

        var wasOpen = _isOpen;
        if (!wasOpen && Options.ResetOnClose)
        {
            // The state was reset on closing, but rebuilds since may have put another command on the first row.
            ResetSelection();
        }

        _isOpen = true;
        _pendingOpen = !wasOpen;
        // A query passed in on purpose - often a mode prefix - is kept for typing after; one left over from the
        // last time the palette was open is selected, so typing replaces it.
        _selectOnOpen = query is null;
        Service.SetOpen(true);

        if (EffectiveEnableHistory)
        {
            _ = History.EnsureLoadedAsync();
        }

        if (!wasOpen)
        {
            SyncShortcuts();
        }

        Rebuild();
        TriggerSourceSearch();
        StateHasChanged();

        if (!wasOpen)
        {
            if (OpenChanged.HasDelegate)
            {
                await OpenChanged.InvokeAsync(true);
            }

            await OnOpened.InvokeAsync();
        }
    }

    private async Task CloseCoreAsync()
    {
        CancelAutoClose();

        if (!_isOpen)
        {
            return;
        }

        if (Inline)
        {
            // What closes a dialog brings an inline palette back to its root page, with the query cleared - unless
            // ResetOnClose is off, which keeps where it was, as it does for the dialog.
            _debounceCts?.Cancel();
            _decisionCts?.Cancel();
            if (_executing)
            {
                _executeCts?.Cancel();
            }

            if (Options.ResetOnClose)
            {
                ResetState();
            }

            Disarm();
            Rebuild();
            TriggerSourceSearch();
            StateHasChanged();
            return;
        }

        _isOpen = false;
        _pendingOpen = false;
        _inputPending = false;
        _debounceCts?.Cancel();
        Disarm();
        Service.SetOpen(false);

        // A handler still running would answer into a palette the user has dismissed - a page or a prompt would reopen
        // it - so it is told, and its answer is dropped (see RunHandlerAsync). Guards still deciding about a row are given up on.
        _decisionCts?.Cancel();
        if (_executing)
        {
            _executeCts?.Cancel();
        }

        await TryInvokeAsync("close");
        await UnlockScrollAsync();

        // Back to the page: whatever happened meanwhile may have changed which commands' shortcuts can run.
        SyncShortcuts();

        if (Options.ResetOnClose)
        {
            ResetState();
        }

        StateHasChanged();

        if (OpenChanged.HasDelegate)
        {
            await OpenChanged.InvokeAsync(false);
        }

        await OnClosed.InvokeAsync();
    }

    private void ResetState()
    {
        _query = string.Empty;
        _inputPending = false;
        CancelPageLoads();
        _pages.Clear();
        _pages.Add(new BitCommandPalettePageFrame { Title = S.Home });
        _sourceResults.Clear();
        _sourceCursors.Clear();
        _pendingSources.Clear();
        _sourceErrors.Clear();
        _sourcesLoading = false;
        _promptError = null;
        ResetSelection();
        _confirmingId = null;
        ShowToast(null);
    }

    /// <summary>Puts the highlight on the first row of the next rebuild, instead of keeping it on the command it was on.</summary>
    private void ResetSelection()
    {
        _activeIndex = 0;
        _selectionReset = true;
    }

    /// <summary>Drops a pending confirmation together with the prompt that asked for it.</summary>
    private void Disarm()
    {
        if (_confirmingId is null)
        {
            return;
        }

        _confirmingId = null;
        ShowToast(null);
    }

    // ---- Input ------------------------------------------------------------------------------------

    // A binding, not value="@_query" with an oninput handler: that writes the query back into the box on every render,
    // and on Blazor Server each render arrives after the keys typed since, which it then erases. A binding leaves the
    // box's text to the browser and writes only a value that differs from what the browser last sent.
    /// <summary>After a key typed into the input: the binding has already put its text in <see cref="_query"/>.</summary>
    private async Task OnInputAsync()
    {
        CancelAutoClose();

        if (_handle is null || _lastSearchCost < SlowSearchThreshold)
        {
            await OnQueryUpdatedAsync();
            return;
        }

        // Searching takes longer than a frame: let the keystroke paint first, and fold keys typed meanwhile into one search.
        // Rendering waits too (see ShouldRender), so the list never re-renders with stale rows in between.
        var version = ++_inputVersion;
        _inputPending = true;
        await TryInvokeAsync("afterPaint");

        if (_inputPending && version == _inputVersion)
        {
            await OnQueryUpdatedAsync();
        }
    }

    private async Task OnQueryUpdatedAsync()
    {
        _inputPending = false;
        Disarm();
        if (_promptError is not null && CurrentPage.Prompt?.Validate is { } validate)
        {
            _promptError = validate(_query);
        }

        ResetSelection();
        // The rows change under a pointer resting on the list; hover selects again once it moves (see OnPointerMoved).
        _pointerSelectionSuspended = true;
        _hoverSuspension++;
        var started = Stopwatch.GetTimestamp();
        Rebuild();
        _lastSearchCost = Stopwatch.GetElapsedTime(started);
        BitCommandPaletteDiagnostics.SearchEnded(_lastSearchCost);
        TriggerSourceSearch();
        _pendingScroll = true;
        if (OnQueryChanged.HasDelegate)
        {
            await OnQueryChanged.InvokeAsync(_query);
        }
    }

    /// <summary>Called by the browser layer for the keys it claimed inside the palette. Not meant to be called from application code.</summary>
    [JSInvokable("OnKeyDown")]
    public Task _OnKeyDown(string key, bool ctrl, bool shift, bool alt, bool meta)
    {
        if (IsDisposing) return Task.CompletedTask;

        return FromBrowserAsync(() => KeyDownAsync(key, ctrl, shift, alt, meta));
    }

    /// <summary>
    /// Runs what a call from the browser layer does. The application's code it reaches - a command's listeners, the
    /// palette's <see cref="OnClosed"/> or <see cref="OnQueryChanged"/> - may throw, and an exception left to the call
    /// would go back to the browser, where nothing reports it. It is dispatched to the palette instead, as one thrown from a
    /// click is, so an error boundary or the host's error handling sees it and it is logged, from whichever input it came.
    /// A call that ends cancelled - a listener that stopped on the application's own token - is no failure, unless the
    /// cancellation wraps a <see cref="TimeoutException"/>, which is how <c>HttpClient</c> reports a timeout: that one is
    /// dispatched, as a command handler's is reported (see <see cref="RunHandlerAsync"/>).
    /// </summary>
    /// <remarks>
    /// The work runs on the renderer's dispatcher and its exception is caught there: the dispatcher hands a cancellation
    /// back as a bare cancelled task, which would no longer tell a timeout from a deliberate stop.
    /// </remarks>
    private Task FromBrowserAsync(Func<Task> work) => InvokeAsync(async () =>
    {
        try
        {
            await work();
        }
        catch (Exception ex)
        {
            // Dispatched first, so nothing done after it - the render below, on a renderer that may be going away - can
            // keep the application's exception from being reported. DispatchExceptionAsync marshals to the dispatcher itself.
            if (ex is not OperationCanceledException || ex.InnerException is TimeoutException)
            {
                await DispatchExceptionAsync(ex);
            }

            // What the call changed before it ended is shown, as the click it stands for would have shown it. The one place
            // that re-renders after a failed browser call: the work itself need not order its renders around what may throw.
            StateHasChanged();
        }
    });

    private async Task KeyDownAsync(string key, bool ctrl, bool shift, bool alt, bool meta)
    {
        if (!_isOpen)
        {
            return;
        }

        // Disabled while open, a dialog can still be dismissed; nothing else a key does runs.
        if (Disabled)
        {
            if (key == "Escape" && !Inline)
            {
                await CloseCoreAsync();
            }

            return;
        }

        // Esc while a toast's close is pending is the close it was waiting for, not a step back from the page the
        // command ran on - unless that close would change nothing: an inline palette that keeps its place goes back a
        // page or clears its query, as always. Any other key means the palette is in use again.
        if (key == "Escape" && _autoCloseCts is not null && (!Inline || Options.ResetOnClose))
        {
            await CloseCoreAsync();
            return;
        }

        CancelAutoClose();

        // Navigation acts on the results of everything typed so far.
        if (_inputPending)
        {
            await OnQueryUpdatedAsync();
        }

        // Browsers report Shift+letter as an upper-case key ("P"); match bindings case-insensitively.
        if (key.Length == 1)
        {
            key = key.ToLowerInvariant();
        }

        var mod = _isMac ? meta : ctrl;
        var page = CurrentPage;
        switch (key)
        {
            case "ArrowDown":
                Move(+1);
                break;
            case "ArrowUp":
                Move(-1);
                break;
            case "n" when ctrl && !shift:
            case "j" when ctrl && !shift && EffectiveVim:
                Move(+1);
                break;
            case "p" when ctrl && !shift:
            case "k" when ctrl && !shift && EffectiveVim:
                Move(-1);
                break;
            case "Home":
                SetActive(0);
                break;
            case "End":
                SetActive(_itemRows.Count - 1);
                break;
            case "PageDown":
                Move(+10, clamp: true);
                break;
            case "PageUp":
                Move(-10, clamp: true);
                break;
            case "Enter":
                if (page.Prompt is not null)
                {
                    await SubmitPromptAsync();
                }
                else if (mod && page.MultiSelect)
                {
                    await SubmitMultiSelectAsync();
                }
                else if (mod && ActiveCommand is { Actions.Count: > 0 } ac)
                {
                    // Mod+Enter runs the first secondary action - of a row that may itself be acted on: enabled, still
                    // listed, and allowed by its guards, as its action panel would require. The row's checks and the
                    // action's are made together, so every guard is asked once.
                    CancelAutoClose();
                    if (ac.Disabled)
                    {
                        ShowToast(ac.DisabledReason ?? S.Disabled);
                    }
                    else
                    {
                        await ExecuteCommandAsync(ac.Actions[0], BitCommandPaletteExecutionTrigger.Palette, via: ac);
                    }
                }
                else if (mod && ActiveCommand is { Handler: null, Href: not null, Disabled: false, ConfirmMessage: null } link && !HasPage(link))
                {
                    // ...and on a plain link, without secondary actions, opens it in a new tab, as GitHub's palette does.
                    CancelAutoClose();
                    if (!IsBusy && await MayRunAsync(link, BitCommandPaletteExecutionTrigger.Palette, chosenFromList: true))
                    {
                        await OpenLinkAsync(link, newTab: true, BitCommandPaletteExecutionTrigger.Palette);
                    }
                }
                else if (ActiveCommand is { } cmd)
                {
                    await ExecuteCommandAsync(cmd, BitCommandPaletteExecutionTrigger.Palette);
                }
                else if (page.LoadFailed)
                {
                    await RetryPageLoadAsync();
                }

                break;
            case "Tab":
            case "ArrowRight" when string.IsNullOrEmpty(_query) || key == "Tab":
                if (ActiveCommand is { } target)
                {
                    await OpenActionsAsync(target);
                }

                break;
            case " " when page.MultiSelect && ActiveCommand is { } toggled:
                ToggleSelection(toggled);
                break;
            case "Backspace" when string.IsNullOrEmpty(_query) && _pages.Count > 1:
            case "ArrowLeft" when string.IsNullOrEmpty(_query) && _pages.Count > 1:
                PopPage();
                break;
            case "Escape":
                if (_confirmingId is not null)
                {
                    Disarm();
                }
                else if (_pages.Count > 1)
                {
                    PopPage();
                }
                else if (Inline)
                {
                    if (_query.Length > 0)
                    {
                        _query = string.Empty;
                        await OnQueryUpdatedAsync();
                    }
                }
                else
                {
                    await CloseCoreAsync();
                }

                break;
            case "Delete" when shift && ActiveCommand is { } removable && EffectiveEnableHistory:
                await History.RemoveAsync(removable.Id);
                break;
            case "p" when mod && shift && ActiveCommand is { } pinnable && EffectiveEnableHistory:
                await History.TogglePinAsync(pinnable.Id);
                break;
        }

        StateHasChanged();
    }

    /// <summary>Called by the browser layer when a toggle shortcut was pressed. Not meant to be called from application code.</summary>
    [JSInvokable("OnToggleShortcut")]
    public Task _OnToggleShortcut()
    {
        if (IsDisposing) return Task.CompletedTask;

        return FromBrowserAsync(ToggleAsync);
    }

    /// <summary>Called by the browser layer when a command's global shortcut was completed. Not meant to be called from application code.</summary>
    [JSInvokable("OnGlobalShortcut")]
    public Task _OnGlobalShortcut(string normalized) => IsDisposing ? Task.CompletedTask : FromBrowserAsync(async () =>
    {
        // Disabled, the shortcuts are withdrawn from the browser; one already on its way runs nothing either.
        if (Inline || Disabled)
        {
            return;
        }

        var cmd = ShortcutOwner(normalized);
        if (cmd is null)
        {
            return;
        }

        await ExecuteCommandAsync(cmd, BitCommandPaletteExecutionTrigger.Shortcut);
        StateHasChanged();
    });

    /// <summary>
    /// Called by the browser layer when the focus enters or leaves an inline palette, which announces result counts
    /// only while it has it. Not meant to be called from application code.
    /// </summary>
    [JSInvokable("OnInlineFocusChanged")]
    public Task _OnInlineFocusChanged(bool focused) => IsDisposing ? Task.CompletedTask : InvokeAsync(() =>
    {
        _inlineFocused = focused;
        StateHasChanged();
    });

    /// <summary>
    /// Called by the browser layer when the first chords of a shortcut sequence were pressed, with them, and with null when
    /// the sequence ended. Not meant to be called from application code.
    /// </summary>
    [JSInvokable("OnSequencePending")]
    public Task _OnSequencePending(string? chords) => IsDisposing ? Task.CompletedTask : FromBrowserAsync(() =>
    {
        _pendingSequence = chords is not null && BitCommandPaletteKeyboardShortcut.TryParse(chords, out var pending) ? pending : null;
        Service.RaiseSequencePending(_pendingSequence);
        StateHasChanged();
        return Task.CompletedTask;
    });

    /// <summary>Called by the browser layer when the backdrop was clicked. Not meant to be called from application code.</summary>
    [JSInvokable("OnBackdropClick")]
    public Task _OnBackdropClick() => IsDisposing is false && Options.CloseOnBackdropClick ? FromBrowserAsync(CloseCoreAsync) : Task.CompletedTask;

    /// <summary>
    /// Called by the browser layer when the pointer first moved after the dialog's <c>data-hover-suspended</c> changed,
    /// which re-enables hover selection. Not meant to be called from application code.
    /// </summary>
    [JSInvokable("OnPointerMoved")]
    public Task _OnPointerMoved()
    {
        if (IsDisposing) return Task.CompletedTask;

        // Called from the browser, off the renderer's dispatcher on Blazor Server, so it would race the handlers that set it.
        return InvokeAsync(() => _pointerSelectionSuspended = false);
    }

    private void Move(int delta, bool clamp = false)
    {
        if (_itemRows.Count == 0)
        {
            return;
        }

        var next = _activeIndex + delta;
        if (clamp || !EffectiveLoop)
        {
            next = Math.Clamp(next, 0, _itemRows.Count - 1);
        }
        else
        {
            next = ((next % _itemRows.Count) + _itemRows.Count) % _itemRows.Count;
        }

        SetActive(next);
    }

    private void SetActive(int index)
    {
        if (_itemRows.Count == 0)
        {
            _activeIndex = 0;
            return;
        }

        _activeIndex = Math.Clamp(index, 0, _itemRows.Count - 1);
        Disarm();
        _pendingScroll = true;
    }

    private void OnItemHover(BitCommandPaletteRow row)
    {
        if (DisablePointerSelection || _pointerSelectionSuspended)
        {
            return;
        }

        if (row.ItemIndex != _activeIndex)
        {
            _activeIndex = row.ItemIndex;
            Disarm();
        }
    }

    private Task OnItemClick(BitCommandPaletteRow row)
    {
        if (Disabled) return Task.CompletedTask;

        if (row.ItemIndex != _activeIndex)
        {
            _activeIndex = row.ItemIndex;
            Disarm();
        }

        return ExecuteCommandAsync(row.Command, BitCommandPaletteExecutionTrigger.Palette);
    }

    /// <summary>Whether a row has an action panel to open: its own actions, or pinning and forgetting it.</summary>
    private bool HasActionPanel(BitCommandPaletteCommand cmd)
        => cmd.Actions is { Count: > 0 } || (EffectiveEnableHistory && !cmd.ExcludeFromHistory && !cmd.Tags.Contains(ModeHintTag));

    /// <summary>A right click opens a row's action panel, as Tab does; a row without one keeps the browser's own menu.</summary>
    private Task OnItemContextMenu(BitCommandPaletteRow row)
    {
        if (Disabled) return Task.CompletedTask;

        if (!HasActionPanel(row.Command) || CurrentPage.MultiSelect)
        {
            return Task.CompletedTask;
        }

        _activeIndex = row.ItemIndex;
        Disarm();
        return OpenActionsAsync(row.Command);
    }

    private Task OnBackClick()
    {
        if (_pages.Count > 1)
        {
            PopPage();
        }

        return Task.CompletedTask;
    }

    /// <summary>Goes back to the page of breadcrumb <paramref name="crumb"/> (0 is the first page under the root).</summary>
    private void OnCrumbClick(int crumb)
    {
        // Breadcrumb c is _pages[c + 1]; every page above it goes, each restoring the query it was opened with.
        while (_pages.Count > crumb + 2)
        {
            PopPage();
        }
    }

    // ---- Pages -------------------------------------------------------------------------------------

    private BitCommandPalettePageFrame CurrentPage => _pages[^1];

    private void PushPage(BitCommandPalettePageFrame frame)
    {
        CancelAutoClose();
        _pages.Add(frame);
        _announcePage = true;
        _query = frame.Prompt?.InitialValue ?? string.Empty;
        _inputPending = false;
        _promptError = null;
        ResetSelection();
        Disarm();
        _sourceResults.Clear();
        _sourceCursors.Clear();
        _sourceErrors.Clear();
        _pendingFocus = true;
        _pendingScroll = true;
        Rebuild();

        // The page's own sources, if any - and in any case an end to the previous page's search, which would otherwise
        // keep this one loading until it answered.
        TriggerSourceSearch();
    }

    private void PopPage()
    {
        if (_pages.Count <= 1)
        {
            return;
        }

        CancelAutoClose();
        var frame = _pages[^1];
        _pages.RemoveAt(_pages.Count - 1);
        _announcePage = true;
        // Whatever the page was still loading is no longer wanted.
        frame.LoadCts?.Cancel();
        _query = frame.QueryBefore;
        _inputPending = false;
        _promptError = null;
        ResetSelection();
        Disarm();
        _sourceResults.Clear();
        _sourceCursors.Clear();
        _sourceErrors.Clear();
        _pendingFocus = true;
        _pendingScroll = true;
        Rebuild();
        TriggerSourceSearch();
    }

    /// <summary>
    /// Fills a page from its command's <see cref="BitCommandPaletteCommand.ChildrenProvider"/>. The provider's token is cancelled when
    /// the page is left, the palette is reset or disposed, or the page is loaded again, and a cancelled load changes
    /// nothing: an abandoned request can neither fill nor end the loading state of a page opened since.
    /// </summary>
    private async Task LoadChildrenAsync(BitCommandPalettePageFrame frame, BitCommandPaletteCommand parent)
    {
        frame.LoadCts?.Cancel();
        var cts = frame.LoadCts = new CancellationTokenSource();
        frame.Loading = true;
        frame.LoadFailed = false;
        StateHasChanged();
        try
        {
            var ctx = CreateContext(parent, null, cts.Token);
            var items = await parent.ChildrenProvider!(ctx, cts.Token);
            if (cts.IsCancellationRequested)
            {
                return;
            }

            frame.Items = items ?? [];
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            if (cts.IsCancellationRequested)
            {
                return;
            }

            // The page shows an error with a retry (see BitCommandPalette.razor) rather than an empty list that looks like "no children".
            frame.LoadFailed = true;
            ReportFailure(parent, ex, loadingChildren: true, propagate: false);
        }
        finally
        {
            if (frame.LoadCts == cts)
            {
                frame.Loading = false;
            }
        }

        if (_pages.Contains(frame))
        {
            Rebuild();
            _pendingScroll = true;
            StateHasChanged();
        }
    }

    /// <summary>Loads the current page again after its children failed to load.</summary>
    private Task RetryPageLoadAsync()
    {
        var page = CurrentPage;
        return page is { LoadFailed: true, Loading: false, Command.ChildrenProvider: not null }
            ? LoadChildrenAsync(page, page.Command)
            : Task.CompletedTask;
    }

    private void CancelPageLoads()
    {
        foreach (var frame in _pages)
        {
            frame.LoadCts?.Cancel();
        }
    }

    private Task OpenActionsAsync(BitCommandPaletteCommand target)
    {
        var actions = new List<BitCommandPaletteCommand>(target.Actions ?? []);
        if (EffectiveEnableHistory && !target.ExcludeFromHistory && !target.Tags.Contains(ModeHintTag))
        {
            var pinned = History.IsPinned(target.Id);
            actions.Add(new BitCommandPaletteCommand
            {
                Id = $"__pin:{target.Id}",
                IsPaletteOwned = true,
                Title = pinned ? S.Unpin : S.Pin,
                Kind = BitCommandPaletteCommandKind.Action,
                Icon = IconSvg("pin"),
                ExcludeFromHistory = true,
                Handler = async _ =>
                {
                    await History.TogglePinAsync(target.Id);
                    return BitCommandPaletteCommandResult.Back;
                },
            });

            if (History.Get(target.Id) is { Count: > 0 })
            {
                actions.Add(new BitCommandPaletteCommand
                {
                    Id = $"__forget:{target.Id}",
                    IsPaletteOwned = true,
                    Title = S.RemoveFromRecents,
                    Icon = IconSvg("clock"),
                    ExcludeFromHistory = true,
                    Handler = async _ =>
                    {
                        await History.RemoveAsync(target.Id);
                        return BitCommandPaletteCommandResult.Back;
                    },
                });
            }

            // Forgetting everything is as close as forgetting one: a privacy control, not a setting to go looking for.
            if (History.Recents(1).Count > 0)
            {
                actions.Add(new BitCommandPaletteCommand
                {
                    Id = "__clear-recents",
                    IsPaletteOwned = true,
                    Title = S.ClearRecents,
                    Icon = IconSvg("trash"),
                    ExcludeFromHistory = true,
                    Handler = async _ =>
                    {
                        await History.ClearAsync();
                        return BitCommandPaletteCommandResult.Back;
                    },
                });
            }
        }

        if (actions.Count == 0)
        {
            return Task.CompletedTask;
        }

        PushPage(new BitCommandPalettePageFrame
        {
            Title = target.Title,
            Items = actions,
            IsActions = true,
            Owner = target,
            Placeholder = S.Actions,
            QueryBefore = _query,
        });
        return Task.CompletedTask;
    }

    private void ToggleSelection(BitCommandPaletteCommand cmd)
    {
        var page = CurrentPage;
        if (!page.Selected.Remove(cmd.Id))
        {
            page.Selected.Add(cmd.Id);
        }
    }

    private async Task SubmitMultiSelectAsync()
    {
        var page = CurrentPage;
        // What a page's sources returned counts too, across every search typed on it.
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var selected = (page.Items ?? _all.Where(c => c.Parent == page.Command?.Id).ToList())
            .Concat(page.SourceCommands.Values)
            .Where(c => page.Selected.Contains(c.Id) && seen.Add(c.Id))
            .ToList();

        var owner = page.Command ?? new BitCommandPaletteCommand { Id = "__multiselect", Title = page.Title, ExcludeFromHistory = true };
        var submit = page.OnSubmit ?? page.Command?.Handler;
        if (submit is null)
        {
            await CloseCoreAsync();
            return;
        }

        // Asked again on submit - about the page's command, or the one whose handler made the page, and about every row
        // checked, which each must still be listed and allowed - since what the guards read may have changed while the
        // boxes were being checked. Checking a box asks nothing: the submit is what acts on the rows.
        if (IsBusy || !await MayRunAsync(page.Command ?? page.Owner ?? owner, BitCommandPaletteExecutionTrigger.Palette, selected: selected))
        {
            return;
        }

        await RunHandlerAsync(owner, submit, selected, null, BitCommandPaletteExecutionTrigger.Palette);
    }

    private async Task SubmitPromptAsync()
    {
        var page = CurrentPage;
        if (page.Prompt is null)
        {
            return;
        }

        if (page.Prompt.Validate is { } validate && validate(_query) is { } error)
        {
            // Shown, announced, and from now on following the text as it is corrected (see OnQueryUpdatedAsync).
            _promptError = error;
            return;
        }

        _promptError = null;
        // A prompt's page is always pushed with the command whose handler answered with it (see ApplyResultAsync), so
        // the submit is guarded as that command - its own guard, its ancestors' and those of the pages it was run from. A
        // prompt without one could not be guarded, and is not submitted.
        //
        // Asked again on submit: what the guards read may have changed while the value was being typed.
        if ((page.Command ?? page.Owner) is not { } owner || IsBusy || !await MayRunAsync(owner, BitCommandPaletteExecutionTrigger.Palette))
        {
            return;
        }
        await RunHandlerAsync(owner, page.Prompt.OnSubmit, [], _query, BitCommandPaletteExecutionTrigger.Palette);
    }

    // ---- Execution -----------------------------------------------------------------------------------

    /// <param name="cmd">The command to run, open or arm.</param>
    /// <param name="trigger">
    /// What started it. Started by <see cref="IBitCommandPaletteService.ExecuteAsync"/>, an unhandled exception is rethrown to that
    /// caller. Started from the palette's own UI or a shortcut, it is logged and shown instead - rethrown into a click
    /// handler it would end a Blazor Server circuit, and into a key handler it would only reach the browser's console.
    /// </param>
    /// <param name="via">
    /// The listed row <paramref name="cmd"/> is a secondary action of, run with Mod+Enter: the row must still be listed and
    /// its guards allow, checked together with the action's so that every guard is asked once.
    /// </param>
    private async Task ExecuteCommandAsync(BitCommandPaletteCommand cmd, BitCommandPaletteExecutionTrigger trigger, BitCommandPaletteCommand? via = null)
    {
        var fromCode = trigger == BitCommandPaletteExecutionTrigger.Programmatic;

        // Whatever it does - runs, arms a confirmation, toggles a row, explains why it is disabled - the palette is in use.
        CancelAutoClose();

        // A handler running turns everything away; guards deciding about a row only what the user does in the palette,
        // so a second Enter does not start the row again, while a shortcut or code still runs its own command.
        if (_executing || (trigger == BitCommandPaletteExecutionTrigger.Palette && _decisionCts is not null))
        {
            return;
        }

        if (cmd.Disabled)
        {
            ShowToast(cmd.DisabledReason ?? S.Disabled);
            return;
        }

        var page = CurrentPage;
        if (via is null && page.MultiSelect && !HasPage(cmd) && !IsPaletteRow(cmd))
        {
            ToggleSelection(cmd);
            return;
        }

        if (cmd.Tags.Contains(ModeHintTag))
        {
            _query = cmd.Value ?? string.Empty;
            await OnQueryUpdatedAsync();
            _pendingFocus = true;
            return;
        }

        // Before anything is armed, opened or run - and again on a confirmation's second Enter, which comes back here.
        if (!await MayRunAsync(cmd, trigger, chosenFromList: trigger == BitCommandPaletteExecutionTrigger.Palette, via: via))
        {
            // Nothing stays armed. The refusal's error took the confirmation's place in the footer; without one, it goes.
            if (_confirmingId is not null)
            {
                _confirmingId = null;
                if (!_toastIsError)
                {
                    ShowToast(null);
                }
            }

            return;
        }

        if (cmd.ConfirmMessage is not null && _confirmingId != cmd.Id)
        {
            await EnsureOpenAsync();

            // A confirmation needs the palette, and a disabled one does not open: nothing is armed.
            if (!_isOpen) return;

            // The second Enter runs the active row, so the command being confirmed has to be it. Started from a
            // shortcut or from code, it may not be on the current page at all: it then gets a page of its own, which has
            // nothing to do with the page under it. A row's secondary action (Mod+Enter) gets one too, but under that row
            // and its page: the second Enter asks their guards, and whether they are still listed, again.
            var row = _itemRows.FindIndex(r => r.Command.Id == cmd.Id);
            if (row < 0)
            {
                PushPage(new BitCommandPalettePageFrame
                {
                    Title = cmd.Title,
                    Items = [cmd],
                    QueryBefore = _query,
                    Owner = via,
                    Detached = trigger != BitCommandPaletteExecutionTrigger.Palette,
                });
                row = 0;
            }

            _activeIndex = row;
            _pendingScroll = true;
            _confirmingId = cmd.Id;
            ShowToast(S.ConfirmPrefix + cmd.ConfirmMessage, sticky: true);
            return;
        }

        Disarm();

        if (HasPage(cmd))
        {
            await EnsureOpenAsync();

            if (!_isOpen) return;

            // A nested page found from the root opens with its ancestors' pages under it.
            var frame = PushPageWithAncestors(cmd, _query, detached: trigger != BitCommandPaletteExecutionTrigger.Palette);
            if (cmd.ChildrenProvider is not null)
            {
                await LoadChildrenAsync(frame, cmd);
            }

            return;
        }

        if (cmd.Handler is null && cmd.Href is not null)
        {
            await OpenLinkAsync(cmd, cmd.OpenInNewTab, trigger);
            return;
        }

        if (cmd.Handler is null)
        {
            await CloseCoreAsync();
            return;
        }

        await RunHandlerAsync(cmd, cmd.Handler, [], null, trigger);
    }

    /// <summary>Follows a command's <see cref="BitCommandPaletteCommand.Href"/>, in this tab or a new one, when its scheme is allowed.</summary>
    private async Task OpenLinkAsync(BitCommandPaletteCommand cmd, bool newTab, BitCommandPaletteExecutionTrigger trigger)
    {
        if (BitCommandPaletteUrl.UnsafeScheme(cmd.Href!, Options.AllowedUrlSchemes) is { } scheme)
        {
            BitCommandPaletteLog.UnsafeLink(Logger, LoggedId(cmd), LoggedTitle(cmd), scheme);
            ShowError(S.Format(S.CommandFailed, cmd.Title));
            return;
        }

        await RunHandlerAsync(cmd, async ctx =>
        {
            if (newTab && _handle is not null)
            {
                await TryInvokeAsync("openInNewTab", cmd.Href);
            }
            else
            {
                ctx.Navigation.NavigateTo(cmd.Href!, cmd.ForceLoad);
            }

            return BitCommandPaletteCommandResult.Close;
        }, [], null, trigger);
    }

    /// <summary>
    /// Whether <paramref name="cmd"/> may run now. A row <paramref name="chosenFromList"/> must still be listed - its
    /// <see cref="BitCommandPaletteCommand.IsVisible"/>, an ancestor's or a page's it was reached through may have turned false since
    /// the list was built - and the guards of those ancestors and pages, its own and every registered
    /// <see cref="IBitCommandPaletteCommandGuard"/> must allow it. Without guards it answers synchronously. A guard that throws
    /// refuses, and so does a command an ancestor of which cannot be found, since that ancestor's guard could not be asked;
    /// one still deciding when the palette closes, or when the user moves on in it, refuses quietly. A refusal is logged,
    /// raised as <see cref="IBitCommandPaletteService.CommandRefused"/> and shown while the palette is open.
    /// </summary>
    /// <param name="cmd">The command about to run.</param>
    /// <param name="trigger">What started it.</param>
    /// <param name="chosenFromList">Chosen from the list: it - or the row <paramref name="via"/> - must still be listed.</param>
    /// <param name="via">
    /// The listed row <paramref name="cmd"/> is a secondary action of (Mod+Enter): it is the row chosen, so it must still be
    /// listed, and its guards and those of its ancestors and pages are asked - about <paramref name="cmd"/> - with the
    /// action's own, each once.
    /// </param>
    /// <param name="selected">
    /// The rows checked on a multi-select page being submitted: each must still be visible and enabled, and its own guard
    /// and the registered ones - asked about it - must allow it.
    /// </param>
    private async Task<bool> MayRunAsync(BitCommandPaletteCommand cmd, BitCommandPaletteExecutionTrigger trigger, bool chosenFromList = false,
        BitCommandPaletteCommand? via = null, IReadOnlyList<BitCommandPaletteCommand>? selected = null)
    {
        if (cmd.IsPaletteOwned)
        {
            return true;
        }

        // The row the user chose: the command itself, or the row whose action it is.
        var row = via ?? cmd;
        BitCommandPaletteCommand? orphan = null;
        var pages = trigger == BitCommandPaletteExecutionTrigger.Palette ? PageOwners(row, out orphan) : null;

        // The ancestors of the row, the action and the checked rows, outermost first. One that cannot be found - not
        // registered, out of reach (IncludeRegisteredCommands off), its registration disposed, or nested deeper than 32
        // levels - fails closed, as a guard that throws does: its guard, or one further up, could not be asked.
        List<BitCommandPaletteCommand>? ancestors = null;
        HashSet<string>? resolvedParents = null;
        bool AddAncestors(BitCommandPaletteCommand c)
        {
            // Rows under the same parent share its ancestors: walked once.
            if (c.Parent is null || !(resolvedParents ??= new(StringComparer.Ordinal)).Add(c.Parent))
            {
                return true;
            }

            if (AncestorsOf(c) is not { } found)
            {
                orphan = c;
                return false;
            }

            (ancestors ??= []).AddRange(found);
            return true;
        }

        var resolved = orphan is null && AddAncestors(row) && (via is null || AddAncestors(cmd));
        foreach (var checkedRow in selected ?? [])
        {
            resolved = resolved && AddAncestors(checkedRow);
        }

        if (!resolved)
        {
            BitCommandPaletteLog.AncestorMissing(Logger, LoggedId(orphan!), LoggedTitle(orphan!));
            Refuse(cmd, BitCommandPaletteGuardResult.Deny());
            return false;
        }

        if (chosenFromList && !IsStillListed(row, ancestors, pages))
        {
            return Unavailable(row);
        }

        if (via is not null && cmd.IsVisible?.Invoke() == false)
        {
            return Unavailable(cmd);
        }

        // A checked row hidden or disabled since it was checked is no longer one the user could have checked.
        foreach (var checkedRow in selected ?? [])
        {
            if (checkedRow.Disabled || checkedRow.IsVisible?.Invoke() == false)
            {
                return Unavailable(checkedRow);
            }
        }

        // A page's guard covers every command under it: its registered descendants (found through their Parent), and - run
        // from the palette - whatever its page lists, which a lazily loaded page, a page's sources, a handler's page and an
        // action panel list without a Parent. Outermost first, each command once. The guards of the command's chain are
        // asked about the command; a checked row's own guard and the registered ones, about that row.
        var context = new BitCommandPaletteGuardContext(cmd, trigger, Services);
        List<(Func<BitCommandPaletteGuardContext, CancellationToken, ValueTask<BitCommandPaletteGuardResult>> Check, BitCommandPaletteGuardContext Context)>? checks = null;
        HashSet<string>? asked = null;
        void Collect(BitCommandPaletteCommand guarded, BitCommandPaletteGuardContext about)
        {
            if (guarded.Guard is { } guard && (asked ??= new(StringComparer.Ordinal)).Add(guarded.Id))
            {
                (checks ??= []).Add((guard, about));
            }
        }

        foreach (var (owner, _) in pages ?? [])
        {
            Collect(owner, context);
        }

        foreach (var ancestor in ancestors ?? [])
        {
            Collect(ancestor, context);
        }

        if (via is not null)
        {
            Collect(via, context);
        }

        Collect(cmd, context);

        var registered = RegisteredGuards;
        List<BitCommandPaletteGuardContext>? selectedContexts = null;
        foreach (var checkedRow in selected ?? [])
        {
            var about = new BitCommandPaletteGuardContext(checkedRow, trigger, Services);
            (selectedContexts ??= []).Add(about);
            Collect(checkedRow, about);
        }

        if (checks is null && registered.Length == 0)
        {
            return true;
        }

        foreach (var guard in registered)
        {
            (checks ??= []).Add((guard.CheckAsync, context));
            foreach (var about in selectedContexts ?? [])
            {
                checks.Add((guard.CheckAsync, about));
            }
        }

        // Deciding can take a round trip to a server. About a row chosen in the palette it shows busy as a handler does,
        // a second Enter is turned away, and closing the palette cancels it - but it is not a running handler: navigating
        // still closes the palette, and a shortcut or code still runs its own command meanwhile. Asked while a handler
        // runs - a handler opening a page through the service - it leaves that handler's state alone and is cancelled
        // with it. Disposing the palette cancels any.
        var nested = _executing;
        var fromPalette = !nested && trigger == BitCommandPaletteExecutionTrigger.Palette;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(nested ? _executeCts?.Token ?? _lifetimeCts.Token : _lifetimeCts.Token);
        using var busy = new CancellationTokenSource();
        var startPage = CurrentPage;
        var startConfirming = _confirmingId;
        var verdict = BitCommandPaletteGuardResult.Allow;
        var refused = cmd;
        if (fromPalette)
        {
            _decisionCts = cts;
            _runningId = row.Id;
            _ = ShowBusyLaterAsync(cts, busy.Token);
        }

        try
        {
            foreach (var (check, about) in checks!)
            {
                refused = about.Command;
                if (!(verdict = await DecideAsync(check(about, cts.Token), cts.Token)).IsAllowed)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception ex)
        {
            // Fails closed: a guard that could not decide - its server down, a bug - must not let the command through.
            BitCommandPaletteLog.GuardFailed(Logger, LoggedId(refused), LoggedTitle(refused), ex);
            verdict = BitCommandPaletteGuardResult.Deny();
        }
        finally
        {
            busy.Cancel();
            if (fromPalette)
            {
                if (ReferenceEquals(_decisionCts, cts))
                {
                    _decisionCts = null;
                }

                // A handler a shortcut or code started meanwhile shows its own busy state.
                if (!_executing)
                {
                    _busy = false;
                    _runningId = null;
                    if (_announcement == S.Running)
                    {
                        _announcement = string.Empty;
                    }
                }
            }
        }

        // Closed or disposed while a guard decided: the user has moved on, and nothing is run or shown.
        if (cts.IsCancellationRequested)
        {
            return false;
        }

        // Moved on in the palette while it decided - went back a page, disarmed the confirmation: what was chosen is no
        // longer what is on screen, so it is neither run nor refused.
        if (trigger == BitCommandPaletteExecutionTrigger.Palette && (!ReferenceEquals(CurrentPage, startPage) || _confirmingId != startConfirming))
        {
            return false;
        }

        if (verdict.IsAllowed)
        {
            return true;
        }

        Refuse(refused, verdict);
        return false;

        bool Unavailable(BitCommandPaletteCommand gone)
        {
            ShowError(S.Format(S.CommandUnavailable, gone.Title));
            Rebuild();
            return false;
        }

        void Refuse(BitCommandPaletteCommand refusedCommand, BitCommandPaletteGuardResult refusal)
        {
            BitCommandPaletteLog.CommandRefused(Logger, LoggedId(refusedCommand), LoggedTitle(refusedCommand), trigger);
            BitCommandPaletteDiagnostics.CommandEnded(null, refusedCommand, trigger, BitCommandPaletteDiagnostics.Refused, null);
            Service.RaiseRefused(refusedCommand, trigger, refusal.Message);
            var message = refusal.Message ?? S.Format(S.CommandRefused, refusedCommand.Title);
            if (message.Length > 0)
            {
                ShowError(message);
            }
            else
            {
                // Said nothing on purpose: a message left from something else must not read as the answer.
                ShowToast(null);
            }
        }
    }

    /// <summary>The registered <see cref="IBitCommandPaletteCommandGuard"/>s, resolved once: they are scoped, as the palette is.</summary>
    private IBitCommandPaletteCommandGuard[] RegisteredGuards
        => _registeredGuards ??= (Services.GetService(typeof(IEnumerable<IBitCommandPaletteCommandGuard>)) as IEnumerable<IBitCommandPaletteCommandGuard>)?.ToArray() ?? [];

    /// <summary>
    /// A command's id as the palette's log messages write it: hidden when <see cref="BitCommandPaletteOptions.RecordCommandIdsInTraces"/>
    /// is off, since ids can embed personal data and logs are usually exported next to traces.
    /// </summary>
    private string LoggedId(BitCommandPaletteCommand cmd) => Options.RecordCommandIdsInTraces ? cmd.Id : HiddenInLogs;

    /// <summary>A command's title as the palette's log messages write it: hidden with its id - a title can name a person too.</summary>
    private string LoggedTitle(BitCommandPaletteCommand cmd) => Options.RecordCommandIdsInTraces ? cmd.Title : HiddenInLogs;

    private const string HiddenInLogs = "(hidden)";

    /// <summary>Whether something the user does in the palette has to wait: a handler is running, or guards are deciding about a row.</summary>
    private bool IsBusy => _executing || _decisionCts is not null;

    /// <summary>
    /// A guard's answer, given up on when <paramref name="cancellationToken"/> is cancelled even if the guard ignores it -
    /// so a palette closed on a guard that never answers is not left turning every command away. A late failure is observed.
    /// </summary>
    private static async ValueTask<BitCommandPaletteGuardResult> DecideAsync(ValueTask<BitCommandPaletteGuardResult> decision, CancellationToken cancellationToken)
    {
        if (decision.IsCompleted)
        {
            return decision.Result;
        }

        var task = decision.AsTask();
        _ = task.ContinueWith(static t => _ = t.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        return await task.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// The commands of the pages a row was reached through, outermost first: each page's command, or the command that made
    /// it (a handler's page, an action panel's row). Only down to the first page something outside the palette pushed - a
    /// confirmation or a prompt started by a shortcut or code sits on whatever page was open, which it has nothing to do
    /// with - and never <paramref name="cmd"/> itself.
    /// </summary>
    private List<(BitCommandPaletteCommand Owner, bool Detached)> PageOwners(BitCommandPaletteCommand cmd, out BitCommandPaletteCommand? orphan)
    {
        orphan = null;
        var found = new List<(BitCommandPaletteCommand, bool)>();
        for (var i = _pages.Count - 1; i >= 0; i--)
        {
            var page = _pages[i];
            if ((page.Command ?? page.Owner) is { } owner && owner.Id != cmd.Id)
            {
                found.Add((owner, page.Detached));
            }

            if (page.Detached)
            {
                break;
            }
        }

        // A page reached without its ancestors' pages under it - an action panel opened with Tab on a nested row found
        // from the root - is still under their guards: each owner comes after its ancestors.
        var owners = new List<(BitCommandPaletteCommand, bool)>();
        for (var i = found.Count - 1; i >= 0; i--)
        {
            var (owner, detached) = found[i];
            if (owner.Parent is not null)
            {
                if (AncestorsOf(owner) is not { } ancestors)
                {
                    orphan ??= owner;
                }
                else
                {
                    foreach (var ancestor in ancestors)
                    {
                        owners.Add((ancestor, detached));
                    }
                }
            }

            owners.Add((owner, detached));
        }

        return owners;
    }

    /// <summary>
    /// Whether a command is still listed as it was: it and its ancestors visible, its ancestors enabled, and the pages it
    /// was reached through from the list still visible (a page pushed from outside the palette was not reached that way).
    /// </summary>
    private static bool IsStillListed(BitCommandPaletteCommand cmd, List<BitCommandPaletteCommand>? ancestors, List<(BitCommandPaletteCommand Owner, bool Detached)>? pages)
    {
        if (cmd.IsVisible?.Invoke() == false)
        {
            return false;
        }

        foreach (var ancestor in ancestors ?? [])
        {
            if (ancestor.Disabled || ancestor.IsVisible?.Invoke() == false)
            {
                return false;
            }
        }

        foreach (var (owner, detached) in pages ?? [])
        {
            if (!detached && owner.IsVisible?.Invoke() == false)
            {
                return false;
            }
        }

        return true;
    }

    // Started by code (trigger), an unhandled exception is rethrown to that caller.
    private async Task RunHandlerAsync(BitCommandPaletteCommand cmd, Func<BitCommandPaletteCommandContext, Task<BitCommandPaletteCommandResult>> handler, IReadOnlyList<BitCommandPaletteCommand> selected, string? promptValue, BitCommandPaletteExecutionTrigger trigger)
    {
        var propagate = trigger == BitCommandPaletteExecutionTrigger.Programmatic;
        // Submitting a prompt or a multi-select page comes here directly: this is what turns away a second Enter.
        if (_executing)
        {
            return;
        }

        var cts = _executeCts = new CancellationTokenSource();
        var startPage = CurrentPage;
        var ctx = CreateContext(cmd, promptValue, cts.Token, selected);
        var sw = Stopwatch.StartNew();
        _executing = true;
        _runningId = cmd.Id;
        _ = ShowBusyLaterAsync(cts);
        // The handler runs inside it, so what it calls - an HttpClient, a database - is traced as its children.
        var tracked = !cmd.IsPaletteOwned;
        var activity = tracked ? BitCommandPaletteDiagnostics.StartCommand(cmd, trigger, Options.RecordCommandIdsInTraces) : null;
        BitCommandPaletteCommandResult result;
        try
        {
            result = await handler(ctx);
        }
        catch (OperationCanceledException ex) when (cts.IsCancellationRequested || ex.InnerException is not TimeoutException)
        {
            // The palette cancelled it, or the handler stopped on purpose. A timeout is a failure, reported below:
            // HttpClient reports one as a TaskCanceledException around a TimeoutException.
            if (tracked)
            {
                BitCommandPaletteDiagnostics.CommandEnded(activity, cmd, trigger, BitCommandPaletteDiagnostics.Cancelled, sw.Elapsed);
            }

            return;
        }
        catch (Exception ex)
        {
            _executing = false;
            if (tracked)
            {
                BitCommandPaletteDiagnostics.CommandEnded(activity, cmd, trigger, BitCommandPaletteDiagnostics.Failed, sw.Elapsed, ex);
            }

            ReportFailure(cmd, ex, loadingChildren: false, propagate);
            return;
        }
        finally
        {
            _executing = false;
            _busy = false;
            _runningId = null;
        }

        sw.Stop();
        if (tracked)
        {
            BitCommandPaletteDiagnostics.CommandEnded(activity, cmd, trigger, BitCommandPaletteDiagnostics.Succeeded, sw.Elapsed);
        }

        // What the handler did may have shown or hidden commands (VisibleWhen): their shortcuts follow.
        SyncShortcuts();

        if (EffectiveEnableHistory && !cmd.ExcludeFromHistory && cmd.Id.Length > 0 && !cmd.Id.StartsWith("__", StringComparison.Ordinal))
        {
            _ = History.RecordAsync(cmd.Id);
        }

        // A listener that throws is the application's failure, not the command's: the command ran, so its result is still
        // applied - the palette closes, shows its page or its toast - and the exception is rethrown after that, to whatever
        // started the command (see FromBrowserAsync).
        ExceptionDispatchInfo? listenerFailure = null;
        try
        {
            Service.RaiseExecuted(cmd, result, sw.Elapsed);
            if (OnCommandExecuted.HasDelegate)
            {
                await OnCommandExecuted.InvokeAsync(new BitCommandPaletteCommandExecutedEventArgs(cmd, result, sw.Elapsed));
            }
        }
        catch (Exception ex)
        {
            listenerFailure = ExceptionDispatchInfo.Capture(ex);
        }

        try
        {
            await FinishRunAsync(cmd, result, trigger, cts, startPage);
        }
        catch (Exception ex) when (listenerFailure is not null)
        {
            // Applying the result failed too - an OnClosed that throws as well: neither failure hides the other.
            throw new AggregateException(listenerFailure.SourceException, ex);
        }

        listenerFailure?.Throw();
    }

    /// <summary>Acts on a handler's <paramref name="result"/>, unless the palette or the page it ran on was left meanwhile.</summary>
    private async Task FinishRunAsync(BitCommandPaletteCommand cmd, BitCommandPaletteCommandResult result, BitCommandPaletteExecutionTrigger trigger, CancellationTokenSource cts, BitCommandPalettePageFrame startPage)
    {
        // Closed (or disposed) while the handler ran: what it answers with must not bring the palette back.
        if (cts.IsCancellationRequested)
        {
            return;
        }

        // The page it ran on was left meanwhile (Esc, Backspace): an answer about pages - back, a page, a prompt, a reset -
        // would act on a page it was not meant for. Closing, toasts and errors still apply.
        if (CurrentPage != startPage && result is BitCommandPaletteCommandResult.BackResult or BitCommandPaletteCommandResult.PageResult
            or BitCommandPaletteCommandResult.PromptResult or BitCommandPaletteCommandResult.ResetResult)
        {
            StateHasChanged();
            return;
        }

        await ApplyResultAsync(cmd, result, trigger);
    }

    private async Task ApplyResultAsync(BitCommandPaletteCommand cmd, BitCommandPaletteCommandResult result, BitCommandPaletteExecutionTrigger trigger)
    {
        switch (result)
        {
            case BitCommandPaletteCommandResult.CloseResult:
                await CloseCoreAsync();
                break;
            case BitCommandPaletteCommandResult.KeepOpenResult:
                Rebuild();
                _pendingFocus = true;
                break;
            case BitCommandPaletteCommandResult.ResetResult:
                ResetState();
                _pendingFocus = true;
                Rebuild();
                TriggerSourceSearch();
                break;
            case BitCommandPaletteCommandResult.BackResult when _pages.Count > 1:
                PopPage();
                break;
            case BitCommandPaletteCommandResult.BackResult:
                // Nothing to go back to: a page's option chosen from the root (SearchNestedCommands) stays there, with
                // the list rebuilt so it shows what the option changed.
                Rebuild();
                _pendingFocus = true;
                break;
            case BitCommandPaletteCommandResult.PageResult p:
                // A handler run from a global shortcut or from code can answer with UI while the palette is closed - and
                // a disabled palette stays closed, with nothing pushed into it.
                await EnsureOpenAsync();
                if (!_isOpen) break;

                PushPage(new BitCommandPalettePageFrame
                {
                    Title = p.Title,
                    Command = null,
                    Items = p.Commands,
                    Placeholder = p.Placeholder,
                    MultiSelect = p.MultiSelect,
                    OnSubmit = p.OnSubmit,
                    QueryBefore = _query,
                    // Its rows are under the command that made it, whose guard covers them.
                    Owner = cmd,
                    Detached = trigger != BitCommandPaletteExecutionTrigger.Palette,
                });
                break;
            case BitCommandPaletteCommandResult.PromptResult pr:
                await EnsureOpenAsync();
                if (!_isOpen) break;

                PushPage(new BitCommandPalettePageFrame
                {
                    Title = pr.Title,
                    Command = cmd,
                    Prompt = pr,
                    Placeholder = pr.Placeholder ?? S.PromptPlaceholder,
                    QueryBefore = _query,
                    Detached = trigger != BitCommandPaletteExecutionTrigger.Palette,
                });
                break;
            case BitCommandPaletteCommandResult.ErrorResult e:
                await EnsureOpenAsync();
                if (!_isOpen) break;

                ShowToast(e.Message, error: true);
                _pendingFocus = true;
                break;
            case BitCommandPaletteCommandResult.ToastResult t:
                if (t.StayOpen)
                {
                    await EnsureOpenAsync();
                }

                ShowToast(t.Message);
                if (!t.StayOpen && _isOpen)
                {
                    // Long enough to read a short confirmation before the palette goes away. Rendered first: run
                    // from the keyboard, nothing else renders until the whole flow - the close included - is over.
                    StateHasChanged();

                    // Anything done in the palette meanwhile calls the close off (see _autoCloseCts): it would close the
                    // palette under the user (or, inline, reset it).
                    var autoClose = _autoCloseCts = new CancellationTokenSource();
                    try
                    {
                        await Task.Delay(ToastCloseDelay, autoClose.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Whatever called it off renders what it changed; the palette may be gone altogether.
                        return;
                    }

                    // Called off too late to stop the delay - by an action that reached the renderer while this waited
                    // for it - or taken over by a later toast's close.
                    if (_autoCloseCts != autoClose)
                    {
                        return;
                    }

                    await CloseCoreAsync();
                }

                break;
        }

        StateHasChanged();
    }

    /// <summary>
    /// Shows the palette busy - a spinner, <c>aria-busy</c>, the running row marked and "Working…" announced - once a
    /// handler has run for <see cref="BusyDelay"/>, so a slow one is never mistaken for one that did nothing, and a quick
    /// one never flickers. <c>Esc</c> closes the palette and cancels it.
    /// </summary>
    private async Task ShowBusyLaterAsync(CancellationTokenSource cts, CancellationToken stop = default)
    {
        try
        {
            using var either = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, stop);
            await Task.Delay(BusyDelay, either.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await InvokeAsync(() =>
        {
            // Still this handler running, or these guards deciding about a row.
            if (!_isOpen || !(ReferenceEquals(_decisionCts, cts) || (_executing && ReferenceEquals(_executeCts, cts))))
            {
                return;
            }

            _busy = true;
            _announcement = S.Running;
            StateHasChanged();
        });
    }

    /// <summary>
    /// Raises <see cref="IBitCommandPaletteService.CommandFailed"/>, then - unless a subscriber handled it - logs the exception, or
    /// rethrows it to the code that called <see cref="IBitCommandPaletteService.ExecuteAsync"/>. The message the subscriber left is
    /// shown either way; a failed page load shows its error state instead.
    /// </summary>
    private void ReportFailure(BitCommandPaletteCommand cmd, Exception ex, bool loadingChildren, bool propagate)
    {
        var failure = Service.RaiseFailed(cmd, ex, S.Format(S.CommandFailed, cmd.Title));
        if (!loadingChildren && !string.IsNullOrEmpty(failure.Message))
        {
            ShowError(failure.Message);
        }

        if (failure.Handled)
        {
            return;
        }

        if (propagate)
        {
            ExceptionDispatchInfo.Capture(ex).Throw();
        }

        if (loadingChildren)
        {
            BitCommandPaletteLog.ChildrenFailed(Logger, LoggedId(cmd), LoggedTitle(cmd), ex);
        }
        else
        {
            BitCommandPaletteLog.CommandFailed(Logger, LoggedId(cmd), LoggedTitle(cmd), ex);
        }
    }

    /// <summary>Shows an error in the footer, announced assertively. Only while open: a closed palette has nowhere to show it.</summary>
    private void ShowError(string message)
    {
        if (_isOpen)
        {
            ShowToast(message, error: true);
        }
    }

    private BitCommandPaletteCommandContext CreateContext(BitCommandPaletteCommand cmd, string? promptValue, CancellationToken ct, IReadOnlyList<BitCommandPaletteCommand>? selected = null)
        => new(cmd, _effectiveQuery, _mode?.Prefix, promptValue, selected ?? [], Services, Navigation, Service, ct);

    /// <summary>Calls off the close a toast scheduled (see <see cref="ApplyResultAsync"/>): the user is using the palette again.</summary>
    private void CancelAutoClose()
    {
        _autoCloseCts?.Cancel();
        _autoCloseCts = null;
    }

    private void ShowToast(string? message, bool sticky = false, bool error = false)
    {
        _toast = message;
        _toastIsError = message is not null && error;
        _toastCts?.Cancel();
        if (message is null || sticky)
        {
            return;
        }

        _toastCts = new CancellationTokenSource();
        var token = _toastCts.Token;
        _ = Task.Delay(3000, token).ContinueWith(_ => InvokeAsync(() =>
        {
            if (!token.IsCancellationRequested && _toast == message)
            {
                _toast = null;
                StateHasChanged();
            }
        }), TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    // ---- Async sources --------------------------------------------------------------------------------

    /// <summary>The sources searched for the current query: the mode's or the registry's on the root page, the page's own on a nested one.</summary>
    private IEnumerable<IBitCommandPaletteSource> ActiveSources()
    {
        var page = CurrentPage;
        if (page.Prompt is not null)
        {
            return [];
        }

        if (!page.IsRoot)
        {
            return page.Sources ?? [];
        }

        return _mode is not null ? _mode.Sources : IncludeRegisteredCommands ? Service.Sources : [];
    }

    private void TriggerSourceSearch()
    {
        _debounceCts?.Cancel();
        _sourceErrors.Clear();
        var sources = ActiveSources().Where(s => _effectiveQuery.Length >= s.MinQueryLength).ToList();
        if (sources.Count == 0)
        {
            // Every caller has just rebuilt; only stale source rows or the loading state need another pass.
            var stale = _sourceResults.Count > 0 || _sourcesLoading;
            _sourceResults.Clear();
            _sourceCursors.Clear();
            _pendingSources.Clear();
            _sourcesLoading = false;
            if (stale)
            {
                Rebuild(reuseRegistryMatches: true);
            }

            return;
        }

        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;
        var query = _effectiveQuery;
        var frame = CurrentPage;
        var ctx = new BitCommandPaletteSourceContext(_mode?.Prefix, frame.Command?.Id, Services);

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(Options.DebounceMs, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            await InvokeAsync(() =>
            {
                // A newer search may have started between the delay and this callback reaching the dispatcher.
                if (token.IsCancellationRequested)
                {
                    return;
                }

                _sourcesLoading = true;
                _pendingSources.Clear();
                foreach (var s in sources)
                {
                    _pendingSources.Add(s.Name);
                }

                StateHasChanged();
            });

            await Task.WhenAll(sources.Select(source => SearchSourceAsync(source, query, ctx, frame, token)));
        }, token);
    }

    /// <summary>
    /// Asks one source and lists what it answers. One that throws or outlives <see cref="BitCommandPaletteOptions.SourceTimeoutMs"/>
    /// is logged and leaves a row that retries it, while the other sources' results stay: one failing API does not take
    /// the rest of the list with it. An answer to a query, page or mode that is no longer current is dropped.
    /// </summary>
    private async Task SearchSourceAsync(IBitCommandPaletteSource source, string query, BitCommandPaletteSourceContext ctx, BitCommandPalettePageFrame frame, CancellationToken token, string? cursor = null)
    {
        BitCommandPaletteSourcePage? results = null;
        var timeoutMs = Options.SourceTimeoutMs;
        var started = Stopwatch.GetTimestamp();
        var activity = BitCommandPaletteDiagnostics.StartSource(source);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        if (timeoutMs > 0)
        {
            timeout.CancelAfter(timeoutMs);
        }

        try
        {
            var search = source.SearchPageAsync(query, cursor, ctx, timeout.Token);
            // A search left behind - overtaken or timed out - may still fail later; that failure is nobody's to report.
            _ = search.ContinueWith(static t => _ = t.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

            // Waited for with the timeout as well as handed it, so a source that ignores its token still times out.
            results = await (timeoutMs > 0 ? search.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs), token) : search.WaitAsync(token)) ?? new BitCommandPaletteSourcePage([]);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            BitCommandPaletteDiagnostics.SourceEnded(activity, source, BitCommandPaletteDiagnostics.Cancelled, Stopwatch.GetElapsedTime(started));
            return;
        }
        catch (Exception ex) when (ex is TimeoutException || (ex is OperationCanceledException && timeout.IsCancellationRequested))
        {
            BitCommandPaletteLog.SourceTimedOut(Logger, source.Name, timeoutMs);
            BitCommandPaletteDiagnostics.SourceEnded(activity, source, BitCommandPaletteDiagnostics.TimedOut, Stopwatch.GetElapsedTime(started));
        }
        catch (Exception ex)
        {
            BitCommandPaletteLog.SourceFailed(Logger, source.Name, ex);
            BitCommandPaletteDiagnostics.SourceEnded(activity, source, BitCommandPaletteDiagnostics.Failed, Stopwatch.GetElapsedTime(started), ex);
        }

        if (results is not null)
        {
            BitCommandPaletteDiagnostics.SourceEnded(activity, source, BitCommandPaletteDiagnostics.Succeeded, Stopwatch.GetElapsedTime(started));
        }

        if (token.IsCancellationRequested)
        {
            return;
        }

        await InvokeAsync(() =>
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            if (results is null && cursor is not null)
            {
                // A next page that failed keeps the ones already listed, and its row to try again.
                ShowError(S.Format(S.SourceFailed, source.Name));
            }
            else if (results is null)
            {
                _sourceErrors.Add(source.Name);
                _sourceResults.Remove(source.Name);
                _sourceCursors.Remove(source.Name);
            }
            else
            {
                var page = (results.Commands ?? []).Take(source.MaxResults).ToList();
                List<BitCommandPaletteCommand> listed;
                if (cursor is not null && _sourceResults.TryGetValue(source.Name, out var earlier))
                {
                    // A next page: appended, without what an earlier page listed already, and the first new row selected.
                    var known = new HashSet<string>(earlier.Select(c => c.Id), StringComparer.Ordinal);
                    var added = page.Where(c => known.Add(c.Id)).ToList();
                    listed = [.. earlier, .. added];
                    _activateId = added.FirstOrDefault()?.Id;
                }
                else
                {
                    listed = page;
                }

                _sourceResults[source.Name] = listed;
                if (results.NextCursor is { } next && (results.TotalCount is not { } total || total > listed.Count))
                {
                    _sourceCursors[source.Name] = (next, results.TotalCount);
                }
                else
                {
                    _sourceCursors.Remove(source.Name);
                }

                foreach (var c in page)
                {
                    frame.SourceCommands[c.Id] = c;
                }
            }

            _pendingSources.Remove(source.Name);
            _sourcesLoading = _pendingSources.Count > 0;
            Rebuild(reuseRegistryMatches: true);
            _pendingScroll = true;
            StateHasChanged();
        });
    }

    /// <summary>Asks a source that failed again, for the same query, without waiting for the debounce.</summary>
    private void RetrySource(string name)
    {
        if (_debounceCts is not { IsCancellationRequested: false } cts || ActiveSources().FirstOrDefault(s => s.Name == name) is not { } source)
        {
            return;
        }

        _sourceErrors.Remove(name);
        _pendingSources.Add(name);
        _sourcesLoading = true;
        var frame = CurrentPage;
        var ctx = new BitCommandPaletteSourceContext(_mode?.Prefix, frame.Command?.Id, Services);
        var query = _effectiveQuery;
        _ = Task.Run(() => SearchSourceAsync(source, query, ctx, frame, cts.Token), cts.Token);
    }

    /// <summary>Asks a source for the page after the ones listed, for the same query, without waiting for the debounce.</summary>
    private void LoadMore(string name)
    {
        if (_debounceCts is not { IsCancellationRequested: false } cts || !_sourceCursors.TryGetValue(name, out var next)
            || ActiveSources().FirstOrDefault(s => s.Name == name) is not { } source || _pendingSources.Contains(name))
        {
            return;
        }

        _pendingSources.Add(name);
        _sourcesLoading = true;
        var frame = CurrentPage;
        var ctx = new BitCommandPaletteSourceContext(_mode?.Prefix, frame.Command?.Id, Services);
        var query = _effectiveQuery;
        _ = Task.Run(() => SearchSourceAsync(source, query, ctx, frame, cts.Token, next.Cursor), cts.Token);
    }

    /// <summary>The row after a source's results when it has more: choosing it loads the next page.</summary>
    private BitCommandPaletteCommand SourceMoreRow(string name, int listed, int? total) => new()
    {
        Id = $"__source-more:{name}",
        IsPaletteOwned = true,
        Title = total is { } all ? S.Format(S.MoreResults, all - listed) : S.LoadMore,
        Group = name,
        Kind = BitCommandPaletteCommandKind.Search,
        Icon = IconSvg("more"),
        ExcludeFromHistory = true,
        Tags = [SourceMoreTag],
        Handler = _ =>
        {
            LoadMore(name);
            return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen);
        },
    };

    /// <summary>A row the palette adds - a source's retry or next page - which is run, never checked, on a multi-select page.</summary>
    private static bool IsPaletteRow(BitCommandPaletteCommand cmd) => cmd.Tags.Contains(SourceErrorTag) || cmd.Tags.Contains(SourceMoreTag);

    /// <summary>The row a failed source leaves in place of its results: choosing it asks the source again.</summary>
    private BitCommandPaletteCommand SourceErrorRow(string name) => new()
    {
        Id = $"__source-error:{name}",
        IsPaletteOwned = true,
        Title = S.Format(S.SourceFailed, name),
        Subtitle = S.Retry,
        Icon = IconSvg("alert"),
        ExcludeFromHistory = true,
        Tags = [SourceErrorTag],
        Handler = _ =>
        {
            RetrySource(name);
            return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen);
        },
    };

    // ---- Row building -------------------------------------------------------------------------------------

    private IEnumerable<BitCommandPaletteCommand> AllCommands()
    {
        foreach (var c in IncludeRegisteredCommands ? Service.Commands : [])
        {
            yield return c;
        }

        foreach (var c in _declarative)
        {
            yield return c;
        }

        if (Commands is not null)
        {
            foreach (var c in Commands)
            {
                yield return c;
            }
        }
    }

    /// <summary>
    /// Takes one snapshot of every command source so rendering never rescans the registry per row - and only when one of
    /// them changed: the registry's version, the commands declared in markup, or the <see cref="Commands"/> parameter
    /// (a new collection; one changed in place needs <see cref="IBitCommandPaletteService.Refresh"/>). Typing never re-reads them.
    /// Returns false when the snapshot was still current.
    /// </summary>
    private bool RefreshSnapshot(bool force = false)
    {
        var key = (Service.Version, _declarativeVersion, Commands);
        if (!force && _snapshotKey is { } current && current.Registry == key.Version && current.Declarative == key._declarativeVersion
            && ReferenceEquals(current.Parameter, key.Commands))
        {
            return false;
        }

        _snapshotKey = key;
        // One command per id, in FindCommand's order: one registered through the service and declared in markup as well
        // would otherwise be listed twice.
        _allById = new Dictionary<string, BitCommandPaletteCommand>(StringComparer.Ordinal);
        _all = AllCommands().Where(c => _allById.TryAdd(c.Id, c)).ToList();
        _parentIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var c in _all)
        {
            if (c.Parent is not null)
            {
                _parentIds.Add(c.Parent);
            }
        }

        return true;
    }

    /// <param name="reuseRegistryMatches">Only async source results changed: keep the registry matches computed for this query.</param>
    private void Rebuild(bool reuseRegistryMatches = false)
    {
        // Rows move under the highlight - a command that just ran joins Recent, source results arrive above it - so it
        // stays on the command it was on, unless the selection was reset on purpose.
        var activeId = _selectionReset ? null : ActiveCommand?.Id;
        _selectionReset = false;
        var page = CurrentPage;
        ResolveMode();
        var key = (page, _mode, _effectiveQuery);
        var reuse = reuseRegistryMatches && _registryMatchKey == key;
        // A new snapshot can bring new shortcuts. Visibility changes are picked up when a command ran, the palette
        // closed, or the app called Refresh - not on every keystroke, while the palette has the keyboard anyway.
        if (!reuse && RefreshSnapshot())
        {
            SyncShortcuts();
        }

        var rows = new List<BitCommandPaletteRow>();
        var items = new List<BitCommandPaletteRow>();

        if (page.Prompt is not null)
        {
            _rows = rows;
            _itemRows = items;
            _announcement = string.Empty;
            return;
        }

        var hasQuery = _effectiveQuery.Length > 0;
        var matcher = Options.Matcher;
        var query = BitCommandPaletteQuery.Parse(_effectiveQuery);

        // Matching the registry dominates the cost of a keystroke; async source results arriving later reuse it.
        if (!reuse)
        {
            _registryMatches = MatchCandidates(page, matcher, query);
            _approximate = false;

            // Nothing matched: a second chance, allowing a typo per term. Only ever after an empty result, so it adds no
            // noise to a query that works.
            if (hasQuery && EffectiveTypoTolerance && query.HasFuzzyTerm(BitCommandPaletteTypoTolerantMatcher.MinTermLength)
                && _registryMatches.TrueForAll(m => double.IsNegativeInfinity(m.Score)))
            {
                var retried = MatchCandidates(page, new BitCommandPaletteTypoTolerantMatcher(matcher), query);
                if (!retried.TrueForAll(m => double.IsNegativeInfinity(m.Score)))
                {
                    _registryMatches = retried.ConvertAll(m => double.IsNegativeInfinity(m.Score) ? m : m with { IsApproximate = true });
                    _approximate = true;
                }
            }

            _registryMatchKey = key;
        }

        if (_approximate)
        {
            matcher = new BitCommandPaletteTypoTolerantMatcher(matcher);
        }

        var matches = new List<BitCommandPaletteMatch>(_registryMatches);

        // Interleave sources that opt into matcher ranking.
        var blockSources = new List<(string Name, List<BitCommandPaletteMatch> Items)>();
        foreach (var source in ActiveSources())
        {
            if (_sourceErrors.Contains(source.Name))
            {
                blockSources.Add((source.Name, [new BitCommandPaletteMatch(SourceErrorRow(source.Name), 0, [], [], null) { FromSource = true, SourceName = source.Name }]));
                continue;
            }

            if (!_sourceResults.TryGetValue(source.Name, out var results) || results.Count == 0)
            {
                continue;
            }

            var more = _sourceCursors.TryGetValue(source.Name, out var cursor) ? SourceMoreRow(source.Name, results.Count, cursor.Total) : null;
            if (source.ApplyMatcher)
            {
                foreach (var c in results)
                {
                    var scoped = c.Group is null ? c.With(b => b.Group(source.Name)) : c;
                    var m = matcher.Match(scoped, query, Options.Weights);
                    if (m is not null)
                    {
                        matches.Add(m with { FromSource = true, SourceName = source.Name, IsApproximate = _approximate });
                    }
                }

                // Ranked below everything, so it ends the source's group.
                if (more is not null)
                {
                    matches.Add(new BitCommandPaletteMatch(more, double.NegativeInfinity, [], [], null) { FromSource = true, SourceName = source.Name });
                }
            }
            else
            {
                var block = results.Select((c, i) =>
                {
                    var m = hasQuery ? matcher.Match(c, query, Options.Weights) : null;
                    return new BitCommandPaletteMatch(c, 0, m?.TitlePositions ?? [], m?.SubtitlePositions ?? [], null) { Rank = -i, FromSource = true, SourceName = source.Name };
                }).ToList();
                if (more is not null)
                {
                    block.Add(new BitCommandPaletteMatch(more, 0, [], [], null) { Rank = -block.Count, FromSource = true, SourceName = source.Name });
                }
                blockSources.Add((source.Name, block));
            }
        }

        var ranked = Ranker.Rank(matches, hasQuery, id => EffectiveEnableHistory ? History.Frecency(id) : 0, Options.MaxResults);

        var itemCounter = 0;
        var usedIds = new HashSet<string>(StringComparer.Ordinal);
        string? headerId = null;

        void AddHeader(string group)
        {
            headerId = FormattableString.Invariant($"{_Id}-g-{rows.Count}");
            rows.Add(new BitCommandPaletteRow(BitCommandPaletteRowKind.GroupHeader, group, null, -1, headerId) { Position = rows.Count });
        }

        void AddItem(BitCommandPaletteMatch m, string? group)
        {
            var row = new BitCommandPaletteRow(BitCommandPaletteRowKind.Item, group, m, itemCounter, FormattableString.Invariant($"{_Id}-i-{itemCounter}")) { Position = rows.Count, GroupHeaderId = headerId };
            itemCounter++;
            rows.Add(row);
            items.Add(row);
        }

        // Pinned & recents (root, default mode, empty query).
        if (!hasQuery && page.IsRoot && _mode is null && EffectiveEnableHistory && History.IsLoaded)
        {
            // A source result can share an id with a registered command; the better ranked one stands for both. The
            // unranked matches come next - a pinned command is listed even when MaxResults left it out of the ranking -
            // then nested commands, reachable from here when SearchNestedCommands is on.
            var byId = new Dictionary<string, BitCommandPaletteMatch>(matches.Count, StringComparer.Ordinal);
            foreach (var m in ranked)
            {
                byId.TryAdd(m.Command.Id, m);
            }

            foreach (var m in matches)
            {
                byId.TryAdd(m.Command.Id, m);
            }

            var chains = new Dictionary<string, AncestorChain?>(StringComparer.Ordinal);
            bool Listable(string id)
            {
                if (byId.ContainsKey(id))
                {
                    return true;
                }

                if (!EffectiveSearchNested || !_allById.TryGetValue(id, out var nested) || nested.Parent is null
                    || nested.IsVisible?.Invoke() == false || ChainOf(nested.Parent, chains) is not { } chain)
                {
                    return false;
                }

                byId[id] = new BitCommandPaletteMatch(nested, 0, [], [], null) { Path = chain.Titles, InheritedGroup = chain.RootGroup };
                return true;
            }

            var pinned = History.Pinned().Where(Listable).Select(id => byId[id]).ToList();
            if (pinned.Count > 0)
            {
                AddHeader(S.Pinned);
                foreach (var m in pinned)
                {
                    AddItem(m, S.Pinned);
                    usedIds.Add(m.Command.Id);
                }
            }

            var recents = History.Recents(Options.MaxRecents * 2)
                .Where(id => !usedIds.Contains(id) && Listable(id))
                .Take(Options.MaxRecents)
                .Select(id => byId[id])
                .ToList();
            if (recents.Count > 0)
            {
                AddHeader(S.Recent);
                foreach (var m in recents)
                {
                    AddItem(m with { Command = m.Command }, S.Recent);
                    usedIds.Add(m.Command.Id);
                }
            }
        }

        var remaining = ranked.Where(m => !usedIds.Contains(m.Command.Id)).ToList();
        var grouped = !hasQuery || EffectiveGroupWhenSearching;

        if (grouped)
        {
            var groups = remaining
                .GroupBy(m => m.Command.Group ?? m.InheritedGroup ?? string.Empty)
                .Select(g => new { Name = g.Key, Items = g.ToList(), Best = g.Max(x => x.Rank), Order = GroupOrderIndex(g.Key) })
                .OrderBy(g => g.Name.Length == 0 ? 0 : 1) // ungrouped items first
                .ThenBy(g => hasQuery ? 0 : g.Order)
                .ThenByDescending(g => g.Best)
                .ThenBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            foreach (var g in groups)
            {
                // Ungrouped commands come first, but below Pinned and Recent they need a heading of their own: without one
                // they would read, and look, like more recents.
                var name = g.Name.Length > 0 ? g.Name : rows.Count > 0 ? S.Suggestions : null;
                if (name is not null)
                {
                    AddHeader(name);
                }

                foreach (var m in g.Items)
                {
                    AddItem(m, name);
                }
            }
        }
        else
        {
            foreach (var m in remaining)
            {
                AddItem(m, null);
            }
        }

        foreach (var (name, block) in blockSources)
        {
            AddHeader(name);
            foreach (var m in block)
            {
                AddItem(m, name);
            }
        }

        _rows = rows;
        _itemRows = items;
        var kept = activeId is null ? -1 : items.FindIndex(r => r.Command.Id == activeId);
        if (kept >= 0)
        {
            _activeIndex = kept;
        }
        else
        {
            if (_activeIndex >= items.Count)
            {
                _activeIndex = Math.Max(0, items.Count - 1);
            }

            // Skip disabled items as initial selection.
            if (items.Count > 0 && items[_activeIndex].Command.Disabled)
            {
                var firstEnabled = items.FindIndex(r => !r.Command.Disabled);
                if (firstEnabled >= 0)
                {
                    _activeIndex = firstEnabled;
                }
            }
        }

        if (_activateId is not null)
        {
            var activate = items.FindIndex(r => r.Command.Id == _activateId);
            if (activate >= 0)
            {
                _activeIndex = activate;
                _pendingScroll = true;
            }

            _activateId = null;
        }

        var count = items.Count == 0 && IsLoading ? S.Loading : S.ResultsAnnouncementFor(items.Count);
        if (_approximate && items.Count > 0)
        {
            count = S.Format(S.ApproximateResultsAnnouncement, S.ApproximateResults, count);
        }
        // Moving to another page says where the palette went as well as what is there.
        _announcement = _announcePage ? S.Format(S.PageAnnouncement, page.IsRoot ? S.Home : page.Title, count) : count;
        _announcePage = false;
    }

    private List<BitCommandPaletteMatch> MatchCandidates(BitCommandPalettePageFrame page, IBitCommandPaletteMatcher matcher, BitCommandPaletteQuery query)
    {
        var matches = new List<BitCommandPaletteMatch>();
        foreach (var c in Candidates(page))
        {
            var m = matcher.Match(c, query, Options.Weights);
            if (m is null)
            {
                if (c.AlwaysShow)
                {
                    matches.Add(new BitCommandPaletteMatch(c, double.NegativeInfinity, [], [], null));
                }

                continue;
            }

            matches.Add(m);
        }

        // Nested commands, found from the root page as kbar finds them: matched with their ancestors' titles as one more
        // field, listed with them before the title and under the outermost ancestor's group. Only while typing - the
        // empty root query lists the root - and not in a mode, which scopes the root on purpose.
        if (page.IsRoot && _mode is null && query.Terms.Count > 0 && EffectiveSearchNested)
        {
            var chains = new Dictionary<string, AncestorChain?>(StringComparer.Ordinal);
            foreach (var c in _all)
            {
                if (c.Parent is null || c.IsVisible?.Invoke() == false || ChainOf(c.Parent, chains) is not { } chain)
                {
                    continue;
                }

                if (matcher.Match(c, query, Options.Weights, chain.Text) is { } m)
                {
                    // One point per level: a root command and a nested one that match equally well are listed root first.
                    matches.Add(m with { Score = m.Score - chain.Titles.Count, Path = chain.Titles, InheritedGroup = chain.RootGroup });
                }
            }
        }

        return matches;
    }

    /// <summary>The titles from the root down to a command, its outermost ancestor's group, and the titles joined for matching.</summary>
    private sealed record AncestorChain(IReadOnlyList<string> Titles, string? RootGroup, string Text);

    /// <summary>
    /// The chain of the command <paramref name="id"/> and its ancestors, or null when it cannot be reached from the root:
    /// an ancestor that is missing, hidden, disabled or a multi-select page (whose rows are choices, not commands).
    /// Memoized per rebuild in <paramref name="memo"/>, so every ancestor's predicate runs once.
    /// </summary>
    private AncestorChain? ChainOf(string id, Dictionary<string, AncestorChain?> memo, int depth = 0)
    {
        if (memo.TryGetValue(id, out var known))
        {
            return known;
        }

        AncestorChain? chain = null;
        if (depth < 32 && _allById.TryGetValue(id, out var c) && !c.Disabled && !c.MultiSelectChildren && c.IsVisible?.Invoke() != false)
        {
            if (c.Parent is null)
            {
                chain = new AncestorChain([c.Title], c.Group, c.Title);
            }
            else if (ChainOf(c.Parent, memo, depth + 1) is { } parent)
            {
                chain = new AncestorChain([.. parent.Titles, c.Title], parent.RootGroup, parent.Text + " " + c.Title);
            }
        }

        memo[id] = chain;
        return chain;
    }

    /// <summary>The registered ancestors of a nested command, outermost first, or null when one of them is missing.</summary>
    private List<BitCommandPaletteCommand>? AncestorsOf(BitCommandPaletteCommand cmd)
    {
        var ancestors = new List<BitCommandPaletteCommand>();
        for (var parent = cmd.Parent; parent is not null; parent = ancestors[^1].Parent)
        {
            if (ancestors.Count >= 32 || FindCommand(parent) is not { } p)
            {
                return null;
            }

            ancestors.Add(p);
        }

        ancestors.Reverse();
        return ancestors;
    }

    /// <summary>
    /// Opens a command's page from the root with its ancestors' pages under it, so the breadcrumbs show the whole path
    /// and going back goes up one level at a time rather than straight to the root.
    /// </summary>
    /// <param name="cmd">The page's command.</param>
    /// <param name="queryBefore">The query to restore when the first page pushed is left.</param>
    /// <param name="detached">Opened by a shortcut or code: the first page pushed is <see cref="BitCommandPalettePageFrame.Detached"/>.</param>
    private BitCommandPalettePageFrame PushPageWithAncestors(BitCommandPaletteCommand cmd, string queryBefore, bool detached = false)
    {
        if (CurrentPage.IsRoot && cmd.Parent is not null && AncestorsOf(cmd) is { } ancestors)
        {
            foreach (var ancestor in ancestors)
            {
                var ancestorFrame = PageFor(ancestor, queryBefore);
                ancestorFrame.Detached = detached;
                PushPage(ancestorFrame);
                queryBefore = string.Empty;
                detached = false;
            }
        }

        var frame = PageFor(cmd, queryBefore);
        frame.Detached = detached;
        PushPage(frame);
        return frame;
    }

    private int GroupOrderIndex(string group)
    {
        var idx = Options.GroupOrder.FindIndex(g => string.Equals(g, group, StringComparison.OrdinalIgnoreCase));
        return idx < 0 ? int.MaxValue : idx;
    }

    private void ResolveMode()
    {
        _mode = null;
        _effectiveQuery = _query.Trim();
        if (!CurrentPage.IsRoot)
        {
            return;
        }

        foreach (var mode in RegisteredModes.OrderByDescending(m => m.Prefix.Length))
        {
            if (_query.StartsWith(mode.Prefix, StringComparison.Ordinal))
            {
                _mode = mode;
                _effectiveQuery = _query[mode.Prefix.Length..].Trim();
                return;
            }
        }
    }

    private IEnumerable<BitCommandPaletteCommand> Candidates(BitCommandPalettePageFrame page)
    {
        IEnumerable<BitCommandPaletteCommand> source;
        if (page.Items is not null)
        {
            source = page.Items;
        }
        else if (page.Command is not null)
        {
            source = _all.Where(c => c.Parent == page.Command.Id);
        }
        else
        {
            source = _all.Where(c => c.Parent is null);
            if (_mode is not null)
            {
                source = source.Where(_mode.Accepts);
            }
            else if (Options.ShowModeHints && _effectiveQuery.Length == 0)
            {
                source = source.Concat(ModeHints());
            }
        }

        return source.Where(c => c.IsVisible?.Invoke() != false);
    }

    private IEnumerable<BitCommandPaletteCommand> ModeHints() => RegisteredModes.Where(m => m.ShowHint).Select(m => new BitCommandPaletteCommand
    {
        Id = $"__mode:{m.Prefix}",
        IsPaletteOwned = true,
        Title = m.Name,
        Subtitle = m.Description,
        Group = S.Modes,
        Kind = BitCommandPaletteCommandKind.Search,
        Value = m.Prefix,
        Badge = m.Prefix,
        Priority = BitCommandPaletteCommandPriority.Lowest,
        ExcludeFromHistory = true,
        Tags = [ModeHintTag],
    });

    // ---- JS plumbing ---------------------------------------------------------------------------------------

    private List<string> ToggleShortcutStrings()
    {
        var toggles = new List<string>();
        if (Inline || Disabled)
        {
            return toggles;
        }

        if (EffectiveToggleShortcut is { } toggle)
        {
            toggles.Add(toggle.Normalized);
        }

        toggles.AddRange(Options.AdditionalToggleShortcuts.Select(s => s.Normalized));
        return toggles;
    }

    /// <summary>The <see cref="ToggleShortcut"/> parameter when it parses, otherwise <see cref="BitCommandPaletteOptions.ToggleShortcut"/>.</summary>
    private BitCommandPaletteKeyboardShortcut? EffectiveToggleShortcut
        => ToggleShortcut is not null && BitCommandPaletteKeyboardShortcut.TryParse(ToggleShortcut, out var toggle) ? toggle : Options.ToggleShortcut;

    /// <summary>Passes on the parameters the browser layer acts on by itself when they changed since <c>init</c>.</summary>
    private void SyncBrowserOptions()
    {
        if (_handle is null)
        {
            return;
        }

        if (EffectiveVim != _syncedVim)
        {
            _syncedVim = EffectiveVim;
            InvokeInBackground("setVimBindings", _syncedVim);
        }

        var toggles = ToggleShortcutStrings();
        var key = string.Join('\n', toggles);
        if (key != _syncedToggles)
        {
            _syncedToggles = key;
            InvokeInBackground("setToggleShortcuts", toggles);
        }
    }

    /// <summary>
    /// Shortcuts browsers handle before a page sees them - closing or opening a tab or a window, switching tabs - written
    /// the way <see cref="BitCommandPaletteKeyboardShortcut.Normalized"/> writes them, with and without the platform modifier.
    /// </summary>
    private static readonly HashSet<string> BrowserReservedShortcuts = new(StringComparer.Ordinal)
    {
        "mod+w", "mod+t", "mod+n", "mod+shift+w", "mod+shift+t", "mod+shift+n", "mod+q",
        "ctrl+w", "ctrl+t", "ctrl+n", "ctrl+shift+w", "ctrl+shift+t", "ctrl+shift+n",
        "ctrl+tab", "ctrl+shift+tab", "mod+tab", "alt+f4",
    };

    // Only a command that can run now: the browser layer claims a matched key before .NET sees it, so the shortcut of a
    // disabled or hidden command would swallow the key and run nothing.
    private static bool ClaimsGlobalShortcut(BitCommandPaletteCommand c)
        => c is { Shortcut: not null, GlobalShortcut: true, Disabled: false } && c.IsVisible?.Invoke() != false;

    // From the snapshot, one command per id: OnGlobalShortcut looks the command up there, so a shortcut of a duplicate
    // it never finds must not be claimed either.
    private List<string> GlobalShortcutStrings() => Inline || Disabled ? [] : _all
        .Where(ClaimsGlobalShortcut)
        .Select(c => c.Shortcut!.Normalized)
        .Distinct(StringComparer.Ordinal)
        .ToList();

    private void SyncShortcuts()
    {
        if (_handle is null)
        {
            return;
        }

        var shortcuts = GlobalShortcutStrings();
        var key = string.Join('\n', shortcuts);
        var claimedChanged = key != _syncedShortcuts;
        if (claimedChanged)
        {
            _syncedShortcuts = key;
            InvokeInBackground("setCommandShortcuts", shortcuts);
        }

        // A new snapshot can bring a conflict without changing which keys are claimed: a second command registered on
        // keys another one already has. Each problem is still reported once.
        if (claimedChanged || !ReferenceEquals(_warnedSnapshot, _all))
        {
            _warnedSnapshot = _all;
            WarnAboutShortcutProblems();
        }
    }

    /// <summary>
    /// The command a completed global shortcut runs. When several claim it, the one registered last wins - the rule ids
    /// follow too - so a page's own <c>Mod+S</c> takes over from the layout's while the page is shown. Commands passed to
    /// the palette or declared in its markup come after the registry.
    /// </summary>
    private BitCommandPaletteCommand? ShortcutOwner(string normalized)
    {
        for (var i = _all.Count - 1; i >= 0; i--)
        {
            var c = _all[i];
            if (ClaimsGlobalShortcut(c) && c.Shortcut!.Normalized == normalized)
            {
                return c;
            }
        }

        return null;
    }

    /// <summary>
    /// Logs, once each, the shortcuts that can never do what their command says: one claimed by several commands, one the
    /// palette's toggle takes, and a sequence whose first chords are a shortcut of their own, which fires first.
    /// </summary>
    private void WarnAboutShortcutProblems()
    {
        if (Inline || !Logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        var owners = new Dictionary<string, List<BitCommandPaletteCommand>>(StringComparer.Ordinal);
        foreach (var c in _all)
        {
            if (ClaimsGlobalShortcut(c))
            {
                var key = c.Shortcut!.Normalized;
                if (!owners.TryGetValue(key, out var list))
                {
                    owners[key] = list = [];
                }

                list.Add(c);
            }
        }

        var toggles = new HashSet<string>(ToggleShortcutStrings(), StringComparer.Ordinal);
        foreach (var (shortcut, list) in owners)
        {
            var winner = list[^1].Id;
            if (BrowserReservedShortcuts.Contains(shortcut) && _reportedShortcutProblems.Add($"reserved|{shortcut}"))
            {
                BitCommandPaletteLog.BrowserReservedShortcut(Logger, shortcut, winner);
            }

            if (toggles.Contains(shortcut))
            {
                if (_reportedShortcutProblems.Add($"toggle|{shortcut}|{winner}"))
                {
                    BitCommandPaletteLog.ShortcutShadowedByToggle(Logger, shortcut, winner);
                }

                continue;
            }

            if (list.Count > 1 && _reportedShortcutProblems.Add($"conflict|{shortcut}|{string.Join('|', list.Select(c => c.Id))}"))
            {
                BitCommandPaletteLog.ShortcutConflict(Logger, shortcut, string.Join(", ", list.Select(c => $"'{c.Id}'")), winner);
            }

            var chords = shortcut.Split(' ');
            for (var n = 1; n < chords.Length; n++)
            {
                var prefix = string.Join(' ', chords, 0, n);
                if ((owners.ContainsKey(prefix) || toggles.Contains(prefix)) && _reportedShortcutProblems.Add($"prefix|{shortcut}|{prefix}"))
                {
                    BitCommandPaletteLog.UnreachableSequence(Logger, shortcut, winner, prefix);
                }
            }
        }
    }

    private void InvokeInBackground(string identifier, object argument)
        => _ = _handle!.InvokeVoidAsync(identifier, argument).AsTask().ContinueWith(_ => { }, TaskContinuationOptions.OnlyOnFaulted);

    /// <summary>
    /// Calls the browser layer where a failure must not escape into rendering: a circuit that disconnected, a page that
    /// navigated away or a call that timed out only means the browser no longer needs the call.
    /// </summary>
    private async Task TryInvokeAsync(string identifier, params object?[] args)
    {
        if (_handle is null)
        {
            return;
        }

        try
        {
            await _handle.InvokeVoidAsync(identifier, args);
        }
        catch (Exception ex) when (IsInteropFailure(ex))
        {
        }
    }

    private async Task LockScrollAsync()
    {
        if (_scrollLocked || Inline) return;

        _scrollLocked = true;
        _scrollLockKey = _Id;
        try
        {
            await _js.BitUtilsLockScroll(_scrollLockKey);
        }
        catch (Exception ex) when (IsInteropFailure(ex))
        {
        }
    }

    private async Task UnlockScrollAsync()
    {
        if (_scrollLocked is false) return;

        _scrollLocked = false;
        var key = _scrollLockKey ?? _Id;
        _scrollLockKey = null;
        try
        {
            await _js.BitUtilsUnlockScroll(key);
        }
        catch (Exception ex) when (IsInteropFailure(ex))
        {
        }
    }

    private static bool IsInteropFailure(Exception ex)
        => ex is JSException or JSDisconnectedException or OperationCanceledException or ObjectDisposedException or InvalidOperationException;

    private ILogger Logger => _logger ??= Services.GetService(typeof(ILogger<BitCommandPalette>)) as ILogger ?? NullLogger.Instance;

    // ---- Effective settings ----------------------------------------------------------------------------------

    private BitCommandPaletteStrings S => Strings ?? Options.Strings;

    private bool EffectiveLoop => Loop ?? Options.Loop;

    private bool EffectiveVim => VimBindings ?? Options.VimBindings;

    private bool EffectiveVirtualize => Virtualize ?? Options.Virtualize;

    private bool EffectiveShowFooter => ShowFooter ?? Options.ShowFooter;

    private bool EffectiveShowShortcuts => ShowShortcuts ?? Options.ShowShortcuts;

    private bool EffectiveHighlight => HighlightMatches ?? Options.HighlightMatches;

    private bool EffectiveEnableHistory => EnableHistory ?? Options.EnableHistory;

    private bool EffectiveGroupWhenSearching => GroupWhenSearching ?? Options.GroupWhenSearching;

    private bool EffectiveSearchNested => SearchNestedCommands ?? Options.SearchNestedCommands;

    private bool EffectiveTypoTolerance => TypoTolerance ?? Options.TypoTolerance;

    private bool EffectiveShowSequenceHint => !Inline && Options.ShowSequenceHint;

    private string EffectivePlaceholder => CurrentPage.Placeholder ?? (_mode?.Placeholder) ?? Placeholder ?? S.Placeholder;

    private IReadOnlyCollection<BitCommandPaletteCommandMode> RegisteredModes => IncludeRegisteredCommands ? Service.Modes : [];

    private string? HoverSuspended => _pointerSelectionSuspended && !DisablePointerSelection
        ? _hoverSuspension.ToString(System.Globalization.CultureInfo.InvariantCulture)
        : null;

    /// <summary>Whether the list is a listbox now: it has rows, and the page is not a prompt.</summary>
    private bool HasListbox => CurrentPage.Prompt is null && _rows.Count > 0;

    /// <summary>A lazily loaded page or an async source has not answered yet.</summary>
    private bool IsLoading => _sourcesLoading || CurrentPage.Loading;

    /// <summary>
    /// Virtualization sizes the list from <see cref="BitCommandPaletteOptions.RowHeight"/>, so while it is on the rows are pinned
    /// to that height inline. Otherwise the stylesheet's <c>--bit-cpl-row-height</c> token is left alone, where an
    /// app's own override of it can reach the rows.
    /// </summary>
    private string? ListStyle
    {
        get
        {
            var rowHeight = EffectiveVirtualize
                ? $"--bit-cpl-row-height:{Options.RowHeight.ToString(System.Globalization.CultureInfo.InvariantCulture)}px"
                : null;

            return rowHeight is null ? Styles?.List : Styles?.List is { Length: > 0 } style ? $"{rowHeight};{style}" : rowHeight;
        }
    }

    /// <summary>
    /// Searches slower than this defer to the next frame so typing stays responsive. Only on WebAssembly, where search and
    /// painting share one thread; on the server each keystroke already round-trips, so another hop would only add latency.
    /// </summary>
    internal TimeSpan SlowSearchThreshold { get; set; } = OperatingSystem.IsBrowser() ? TimeSpan.FromMilliseconds(8) : TimeSpan.MaxValue;

    private IReadOnlyList<string> Breadcrumbs => _pages.Skip(1).Select(p => p.Title).ToList();

    private BitCommandPaletteStateContext StateContext => new(
        _effectiveQuery,
        _mode?.Prefix,
        _mode?.Name,
        Breadcrumbs,
        _itemRows.Count,
        _activeIndex,
        ActiveCommand,
        IsLoading,
        CurrentPage.MultiSelect,
        CurrentPage.Selected.Count,
        _toast,
        _isMac);

    private string? ActiveDescendant => _itemRows.Count > 0 && _activeIndex < _itemRows.Count ? _itemRows[_activeIndex].ElementId : null;
}
