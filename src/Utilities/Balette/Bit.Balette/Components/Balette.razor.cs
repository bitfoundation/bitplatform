using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace Bit.Balette;

/// <summary>
/// The command palette dialog. Place it once in your layout. Commands come from
/// <see cref="IBaletteService"/>, from declarative <see cref="BaletteItem"/> children, and from async sources.
/// </summary>
public partial class Balette : ComponentBase, IAsyncDisposable
{
    private const string ModeHintTag = "__balette_mode_hint";
    private const string SourceErrorTag = "__balette_source_error";
    private const string SourceMoreTag = "__balette_source_more";

    /// <summary>How long a handler runs before the palette shows it is busy: long enough that a quick one never flickers.</summary>
    private static readonly TimeSpan BusyDelay = TimeSpan.FromMilliseconds(150);

    private readonly string _id = $"bit-balette-{Guid.NewGuid():N}"[..20];
    private readonly List<BaletteCommand> _declarative = [];
    private readonly List<BalettePageFrame> _pages = [];
    private readonly Dictionary<string, IReadOnlyList<BaletteCommand>> _sourceResults = new(StringComparer.Ordinal);
    private readonly HashSet<string> _pendingSources = new(StringComparer.Ordinal);
    private readonly HashSet<string> _sourceErrors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (string Cursor, int? Total)> _sourceCursors = new(StringComparer.Ordinal);

    private ElementReference _dialogEl;
    private ElementReference _inputEl;
    private ElementReference _listEl;
    private IJSObjectReference? _module;
    private IJSObjectReference? _handle;
    private DotNetObjectReference<Balette>? _selfRef;

    private bool _isOpen;
    private bool _pendingOpen;
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
    private BaletteKeyboardShortcut? _pendingSequence;
    private ILogger? _logger;
    private readonly HashSet<string> _reportedShortcutProblems = new(StringComparer.Ordinal);
    private string _announcement = string.Empty;
    private CancellationTokenSource? _debounceCts;
    private CancellationTokenSource? _executeCts;
    private CancellationTokenSource? _toastCts;
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

    private List<BaletteCommand> _all = [];
    // The snapshot the shortcut problems were last looked for in.
    private List<BaletteCommand>? _warnedSnapshot;
    private int _declarativeVersion;
    private (int Registry, int Declarative, IEnumerable<BaletteCommand>? Parameter)? _snapshotKey;
    private Dictionary<string, BaletteCommand> _allById = new(StringComparer.Ordinal);
    private HashSet<string> _parentIds = new(StringComparer.Ordinal);
    private bool _approximate;
    private IEnumerable<BaletteCommand>? _lastCommandsParameter;
    private List<BaletteRow> _rows = [];
    private List<BaletteRow> _itemRows = [];
    private BaletteCommandMode? _mode;
    private string _effectiveQuery = string.Empty;
    private List<BaletteMatch> _registryMatches = [];
    private (BalettePageFrame? Page, BaletteCommandMode? Mode, string? Query) _registryMatchKey;

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IServiceProvider Services { get; set; } = default!;
    [Inject] private BaletteService Service { get; set; } = default!;
    [Inject] private BaletteOptions Options { get; set; } = default!;
    [Inject] private BaletteRanker Ranker { get; set; } = default!;
    [Inject] private BaletteHistory History { get; set; } = default!;

    // ---- Parameters -------------------------------------------------------------------------

    /// <summary>Declarative <see cref="BaletteGroup"/> / <see cref="BaletteItem"/> children.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Placeholder override for the root page.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Strings override (defaults to <see cref="BaletteOptions.Strings"/>).</summary>
    [Parameter] public BaletteStrings? Strings { get; set; }

    /// <summary>Extra CSS class on the dialog element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Inline style on the dialog element (e.g. <c>--bit-balette-width: 720px</c>).</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Theme override: <c>auto</c>, <c>light</c> or <c>dark</c>.</summary>
    [Parameter] public string? Theme { get; set; }

    /// <summary>Render no built-in styles/classes beyond structural data attributes.</summary>
    [Parameter] public bool Unstyled { get; set; }

    /// <summary>Controlled open state (two-way bindable).</summary>
    [Parameter] public bool Open { get; set; }

    /// <summary>Raised when the palette opens or closes, for <c>@bind-Open</c>.</summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Replaces the content of every row. The row element itself - its role, ARIA state, data attributes and click handling - stays the palette's.</summary>
    [Parameter] public RenderFragment<BaletteItemContext>? ItemTemplate { get; set; }

    /// <summary>Replaces the content of a group heading; receives the heading text.</summary>
    [Parameter] public RenderFragment<string>? GroupHeaderTemplate { get; set; }

    /// <summary>Replaces the "no results" state; receives the query that found nothing.</summary>
    [Parameter] public RenderFragment<string>? EmptyTemplate { get; set; }

    /// <summary>Replaces the loading state shown while a lazily loaded page or an async source has nothing to show yet.</summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>Content rendered above the search box, with a snapshot of the palette's state.</summary>
    [Parameter] public RenderFragment<BaletteStateContext>? HeaderTemplate { get; set; }

    /// <summary>Replaces the footer with its keyboard hints and toasts, with a snapshot of the palette's state.</summary>
    [Parameter] public RenderFragment<BaletteStateContext>? FooterTemplate { get; set; }

    /// <summary>Renders a detail/preview pane for the active command (Raycast style).</summary>
    [Parameter] public RenderFragment<BaletteCommand>? DetailTemplate { get; set; }

    /// <summary>Custom icon renderer. Receives the command; return nothing for no icon.</summary>
    [Parameter] public RenderFragment<BaletteCommand>? IconTemplate { get; set; }

    /// <summary>Override <see cref="BaletteOptions.Loop"/>.</summary>
    [Parameter] public bool? Loop { get; set; }

    /// <summary>Override of <see cref="BaletteOptions.VimBindings"/>: <c>Ctrl+J</c>/<c>Ctrl+K</c> move the selection.</summary>
    [Parameter] public bool? VimBindings { get; set; }

    /// <summary>Override of <see cref="BaletteOptions.Virtualize"/>: render the list through <c>&lt;Virtualize&gt;</c>.</summary>
    [Parameter] public bool? Virtualize { get; set; }

    /// <summary>Override of <see cref="BaletteOptions.ShowFooter"/>.</summary>
    [Parameter] public bool? ShowFooter { get; set; }

    /// <summary>Override of <see cref="BaletteOptions.ShowShortcuts"/>: show each command's shortcut on its row.</summary>
    [Parameter] public bool? ShowShortcuts { get; set; }

    /// <summary>Override of <see cref="BaletteOptions.HighlightMatches"/>: mark the matched characters.</summary>
    [Parameter] public bool? HighlightMatches { get; set; }

    /// <summary>Override of <see cref="BaletteOptions.EnableHistory"/>: recents, pins and frecency.</summary>
    [Parameter] public bool? EnableHistory { get; set; }

    /// <summary>Override of <see cref="BaletteOptions.GroupWhenSearching"/>: keep group headings while a query is typed.</summary>
    [Parameter] public bool? GroupWhenSearching { get; set; }

    /// <summary>Override of <see cref="BaletteOptions.SearchNestedCommands"/>: find nested commands from the root page.</summary>
    [Parameter] public bool? SearchNestedCommands { get; set; }

    /// <summary>Override of <see cref="BaletteOptions.TypoTolerance"/>: retry a query that found nothing allowing a typo per term.</summary>
    [Parameter] public bool? TypoTolerance { get; set; }

    /// <summary>
    /// Renders the palette in the page instead of in a modal dialog - a settings search, a docs search page, a command
    /// list in a side panel. It is always open, <c>Esc</c> goes back a page or clears the query rather than closing, and
    /// a command that would close the palette resets it to its root page. It leaves the dialog's part to the modal
    /// palette a layout usually has as well: it neither opens on the toggle shortcut nor answers
    /// <see cref="IBaletteService.OpenAsync"/>, <see cref="IBaletteService.CloseAsync"/> or
    /// <see cref="IBaletteService.ExecuteAsync"/>, and claims no global shortcuts - so nothing runs twice.
    /// Set before the first render; changing it later is not supported.
    /// </summary>
    [Parameter] public bool Inline { get; set; }

    /// <summary>
    /// Whether the commands, sources and modes registered through <see cref="IBaletteService"/> are part of this palette
    /// (default true). Set false for a palette that lists only its own - the ones declared in its markup and passed to
    /// <see cref="Commands"/> - such as an inline search of one page's settings.
    /// </summary>
    [Parameter] public bool IncludeRegisteredCommands { get; set; } = true;

    /// <summary>Override of <see cref="BaletteOptions.Density"/>: <c>comfortable</c> or <c>compact</c>.</summary>
    [Parameter] public string? Density { get; set; }

    /// <summary>Ignore mouse hover for selecting the active item (keyboard only).</summary>
    [Parameter] public bool DisablePointerSelection { get; set; }

    /// <summary>Toggle shortcut override (e.g. <c>Ctrl+Shift+P</c>).</summary>
    [Parameter] public string? ToggleShortcut { get; set; }

    /// <summary>Commands supplied directly to this instance (in addition to the service registry).</summary>
    [Parameter] public IEnumerable<BaletteCommand>? Commands { get; set; }

    /// <summary>Raised after the palette opened.</summary>
    [Parameter] public EventCallback OnOpened { get; set; }

    /// <summary>Raised after the palette closed.</summary>
    [Parameter] public EventCallback OnClosed { get; set; }

    /// <summary>Raised after every change of the query, with the raw text (a mode prefix included).</summary>
    [Parameter] public EventCallback<string> OnQueryChanged { get; set; }

    /// <summary>Raised after a command of this palette ran, with its result and how long the handler took.</summary>
    [Parameter] public EventCallback<BaletteCommandExecutedEventArgs> OnCommandExecuted { get; set; }

    /// <summary>Any other attribute, rendered on the <c>&lt;dialog&gt;</c> element.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    // ---- Public API --------------------------------------------------------------------------

    /// <summary>Whether the dialog is open.</summary>
    public bool IsOpen => _isOpen;

    /// <summary>Current raw query (including a mode prefix).</summary>
    public string Query => _query;

    /// <summary>Currently highlighted command, if any.</summary>
    public BaletteCommand? ActiveCommand => _activeIndex >= 0 && _activeIndex < _itemRows.Count ? _itemRows[_activeIndex].Command : null;

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

    protected override void OnInitialized()
    {
        // The root page comes first: registering the DI sources below raises CommandsChanged, which rebuilds right
        // away on the renderer's thread and needs a current page.
        _pages.Add(new BalettePageFrame { Title = S.Home });

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
        foreach (var source in IncludeRegisteredCommands ? Services.GetService(typeof(IEnumerable<IBaletteSource>)) as IEnumerable<IBaletteSource> ?? [] : [])
        {
            if (!Service.Sources.Contains(source))
            {
                Service.RegisterSource(source);
            }
        }

        Rebuild();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!ReferenceEquals(_lastCommandsParameter, Commands))
        {
            _lastCommandsParameter = Commands;
            Rebuild();
        }

        SyncBrowserOptions();

        if (Inline)
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
        if (firstRender)
        {
            try
            {
                _module = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Bit.Balette/bit-balette.js");
                _selfRef = DotNetObjectReference.Create(this);
                var toggles = ToggleShortcutStrings();
                var shortcuts = GlobalShortcutStrings();
                _syncedToggles = string.Join('\n', toggles);
                _syncedShortcuts = string.Join('\n', shortcuts);
                _syncedVim = EffectiveVim;
                // Plain values rather than one options object: the serializer discovers an object's shape through
                // reflection, and a trimmed app no longer has the metadata it needs for that.
                _handle = await _module.InvokeAsync<IJSObjectReference>("init", _dialogEl, _selfRef,
                    toggles, shortcuts, Options.SuppressShortcutsInEditableElements, Options.SequenceTimeoutMs, _syncedVim, Inline);
                // Parameters set and commands registered while init was on its way.
                SyncBrowserOptions();
                SyncShortcuts();
                WarnAboutShortcutProblems();
                _isMac = await _handle.InvokeAsync<bool>("isMac");
                Service.SetPlatform(_isMac);
                StateHasChanged();
            }
            catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException or InvalidOperationException or NotSupportedException)
            {
                // Prerendering without JS, module blocked, circuit gone or the arguments not serializable: palette will
                // still render but without hotkeys.
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
                _pendingScroll = true;
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

    public async ValueTask DisposeAsync()
    {
        Service.OpenRequested -= OpenFromServiceAsync;
        Service.CloseRequested -= CloseFromServiceAsync;
        Service.ExecuteRequested -= ExecuteFromServiceAsync;
        Service.CommandsChanged -= OnRegistryChanged;
        Service.RefreshRequested -= OnRefreshRequested;
        History.Changed -= OnHistoryChanged;
        Navigation.LocationChanged -= OnLocationChanged;
        _debounceCts?.Cancel();
        _executeCts?.Cancel();
        _toastCts?.Cancel();
        CancelPageLoads();

        try
        {
            if (_handle is not null)
            {
                await _handle.InvokeVoidAsync("dispose");
                await _handle.DisposeAsync();
            }

            if (_module is not null)
            {
                await _module.DisposeAsync();
            }
        }
        catch (Exception ex) when (IsInteropFailure(ex))
        {
            // The circuit or the page is already gone, and the browser layer with it.
        }

        _selfRef?.Dispose();
    }

    // ---- Declarative registration (used by BaletteItem) -------------------------------------

    internal void AddDeclarative(BaletteCommand command)
    {
        _declarative.RemoveAll(c => c.Id == command.Id);
        _declarative.Add(command);
        _declarativeVersion++;
        Rebuild();
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>True when selecting the command opens a nested page (static, lazy, or declaratively registered children).</summary>
    internal bool HasPage(BaletteCommand cmd) => cmd.HasChildren || _parentIds.Contains(cmd.Id);

    internal void RemoveDeclarative(string id)
    {
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
        await ExecuteCommandAsync(command, fromCode: true);
        StateHasChanged();
    });

    /// <summary>
    /// Looks a command up in the registry first, then among the ones declared in markup and passed to <see cref="Commands"/>:
    /// the order in which <see cref="RefreshSnapshot"/> keeps one command per id.
    /// </summary>
    private BaletteCommand? FindCommand(string commandId)
        => (IncludeRegisteredCommands ? Service.Find(commandId) : null)
        ?? _declarative.FirstOrDefault(c => c.Id == commandId)
        ?? Commands?.FirstOrDefault(c => c.Id == commandId);

    /// <summary>
    /// The page a command with children opens. Children registered through the service are flattened into the
    /// registry with their parent set, and found there; the static children of a command that never went through
    /// the registry - declared in markup or passed to <see cref="Commands"/> - are listed from the command itself.
    /// </summary>
    private BalettePageFrame PageFor(BaletteCommand cmd, string queryBefore) => new()
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
        if (Inline)
        {
            // Always open: only the query or the page changes, and the input takes the focus.
            if (query is not null || pageCommandId is not null)
            {
                ResetState();
                if (pageCommandId is not null && FindCommand(pageCommandId) is { } target)
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

        // Closing already resets when ResetOnClose is set; an explicit query or page always starts fresh.
        if (query is not null || pageCommandId is not null)
        {
            ResetState();
        }

        if (pageCommandId is not null && FindCommand(pageCommandId) is { } page)
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
        if (!_isOpen)
        {
            return;
        }

        if (Inline)
        {
            // What closes a dialog brings an inline palette back to its root page, with the query cleared - unless
            // ResetOnClose is off, which keeps where it was, as it does for the dialog.
            _debounceCts?.Cancel();
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
        // it - so it is told, and its answer is dropped (see RunHandlerAsync).
        if (_executing)
        {
            _executeCts?.Cancel();
        }

        await TryInvokeAsync("close");

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
        _pages.Add(new BalettePageFrame { Title = S.Home });
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
        TriggerSourceSearch();
        _pendingScroll = true;
        if (OnQueryChanged.HasDelegate)
        {
            await OnQueryChanged.InvokeAsync(_query);
        }
    }

    /// <summary>Called from JS for navigation keys (already preventDefault-ed).</summary>
    [JSInvokable]
    public async Task OnKeyDown(string key, bool ctrl, bool shift, bool alt, bool meta)
    {
        if (!_isOpen)
        {
            return;
        }

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
                    // Mod+Enter runs the first secondary action.
                    await ExecuteCommandAsync(ac.Actions[0], fromCode: false);
                }
                else if (mod && ActiveCommand is { Handler: null, Href: not null, Disabled: false, ConfirmMessage: null } link && !HasPage(link))
                {
                    // ...and on a plain link, without secondary actions, opens it in a new tab, as GitHub's palette does.
                    await OpenLinkAsync(link, newTab: true, fromCode: false);
                }
                else if (ActiveCommand is { } cmd)
                {
                    await ExecuteCommandAsync(cmd, fromCode: false);
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
    [JSInvokable]
    public Task OnToggleShortcut() => InvokeAsync(ToggleAsync);

    /// <summary>Called by the browser layer when a command's global shortcut was completed. Not meant to be called from application code.</summary>
    [JSInvokable]
    public Task OnGlobalShortcut(string normalized) => InvokeAsync(async () =>
    {
        if (Inline)
        {
            return;
        }

        var cmd = ShortcutOwner(normalized);
        if (cmd is null)
        {
            return;
        }

        await ExecuteCommandAsync(cmd, fromCode: false);
        StateHasChanged();
    });

    /// <summary>
    /// Called by the browser layer when the focus enters or leaves an inline palette, which announces result counts
    /// only while it has it. Not meant to be called from application code.
    /// </summary>
    [JSInvokable]
    public Task OnInlineFocusChanged(bool focused) => InvokeAsync(() =>
    {
        _inlineFocused = focused;
        StateHasChanged();
    });

    /// <summary>
    /// Called by the browser layer when the first chords of a shortcut sequence were pressed, with them, and with null when
    /// the sequence ended. Not meant to be called from application code.
    /// </summary>
    [JSInvokable]
    public Task OnSequencePending(string? chords) => InvokeAsync(() =>
    {
        _pendingSequence = chords is not null && BaletteKeyboardShortcut.TryParse(chords, out var pending) ? pending : null;
        Service.RaiseSequencePending(_pendingSequence);
        StateHasChanged();
    });

    /// <summary>Called by the browser layer when the backdrop was clicked. Not meant to be called from application code.</summary>
    [JSInvokable]
    public Task OnBackdropClick() => Options.CloseOnBackdropClick ? InvokeAsync(CloseCoreAsync) : Task.CompletedTask;

    /// <summary>
    /// Called by the browser layer when the pointer first moved after the dialog's <c>data-hover-suspended</c> changed,
    /// which re-enables hover selection. Not meant to be called from application code.
    /// </summary>
    [JSInvokable]
    public Task OnPointerMoved()
    {
        _pointerSelectionSuspended = false;
        return Task.CompletedTask;
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

    private void OnItemHover(BaletteRow row)
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

    private Task OnItemClick(BaletteRow row)
    {
        if (row.ItemIndex != _activeIndex)
        {
            _activeIndex = row.ItemIndex;
            Disarm();
        }

        return ExecuteCommandAsync(row.Command, fromCode: false);
    }

    /// <summary>Whether a row has an action panel to open: its own actions, or pinning and forgetting it.</summary>
    private bool HasActionPanel(BaletteCommand cmd)
        => cmd.Actions is { Count: > 0 } || (EffectiveEnableHistory && !cmd.ExcludeFromHistory && !cmd.Tags.Contains(ModeHintTag));

    /// <summary>A right click opens a row's action panel, as Tab does; a row without one keeps the browser's own menu.</summary>
    private Task OnItemContextMenu(BaletteRow row)
    {
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

    private BalettePageFrame CurrentPage => _pages[^1];

    private void PushPage(BalettePageFrame frame)
    {
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
    /// Fills a page from its command's <see cref="BaletteCommand.ChildrenProvider"/>. The provider's token is cancelled when
    /// the page is left, the palette is reset or disposed, or the page is loaded again, and a cancelled load changes
    /// nothing: an abandoned request can neither fill nor end the loading state of a page opened since.
    /// </summary>
    private async Task LoadChildrenAsync(BalettePageFrame frame, BaletteCommand parent)
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

            // The page shows an error with a retry (see Balette.razor) rather than an empty list that looks like "no children".
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

    private Task OpenActionsAsync(BaletteCommand target)
    {
        var actions = new List<BaletteCommand>(target.Actions ?? []);
        if (EffectiveEnableHistory && !target.ExcludeFromHistory && !target.Tags.Contains(ModeHintTag))
        {
            var pinned = History.IsPinned(target.Id);
            actions.Add(new BaletteCommand
            {
                Id = $"__pin:{target.Id}",
                Title = pinned ? S.Unpin : S.Pin,
                Kind = BaletteCommandKind.Action,
                Icon = IconSvg("pin"),
                ExcludeFromHistory = true,
                Handler = async _ =>
                {
                    await History.TogglePinAsync(target.Id);
                    return BaletteCommandResult.Back;
                },
            });

            if (History.Get(target.Id) is { Count: > 0 })
            {
                actions.Add(new BaletteCommand
                {
                    Id = $"__forget:{target.Id}",
                    Title = S.RemoveFromRecents,
                    Icon = IconSvg("clock"),
                    ExcludeFromHistory = true,
                    Handler = async _ =>
                    {
                        await History.RemoveAsync(target.Id);
                        return BaletteCommandResult.Back;
                    },
                });
            }

            // Forgetting everything is as close as forgetting one: a privacy control, not a setting to go looking for.
            if (History.Recents(1).Count > 0)
            {
                actions.Add(new BaletteCommand
                {
                    Id = "__clear-recents",
                    Title = S.ClearRecents,
                    Icon = IconSvg("trash"),
                    ExcludeFromHistory = true,
                    Handler = async _ =>
                    {
                        await History.ClearAsync();
                        return BaletteCommandResult.Back;
                    },
                });
            }
        }

        if (actions.Count == 0)
        {
            return Task.CompletedTask;
        }

        PushPage(new BalettePageFrame
        {
            Title = target.Title,
            Items = actions,
            IsActions = true,
            Placeholder = S.Actions,
            QueryBefore = _query,
        });
        return Task.CompletedTask;
    }

    private void ToggleSelection(BaletteCommand cmd)
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

        var owner = page.Command ?? new BaletteCommand { Id = "__multiselect", Title = page.Title, ExcludeFromHistory = true };
        var submit = page.OnSubmit ?? page.Command?.Handler;
        if (submit is null)
        {
            await CloseCoreAsync();
            return;
        }

        await RunHandlerAsync(owner, submit, selected, null);
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
        var owner = page.Command ?? new BaletteCommand { Id = "__prompt", Title = page.Title, ExcludeFromHistory = true };
        await RunHandlerAsync(owner, page.Prompt.OnSubmit, [], _query);
    }

    // ---- Execution -----------------------------------------------------------------------------------

    /// <param name="cmd">The command to run, open or arm.</param>
    /// <param name="fromCode">
    /// Started by <see cref="IBaletteService.ExecuteAsync"/>: an unhandled exception is rethrown to that caller. Started
    /// from the palette's own UI, it is logged and shown instead - rethrown into a click handler it would end a Blazor
    /// Server circuit, and into a key handler it would only reach the browser's console.
    /// </param>
    private async Task ExecuteCommandAsync(BaletteCommand cmd, bool fromCode)
    {
        if (_executing)
        {
            return;
        }

        if (cmd.Disabled)
        {
            ShowToast(cmd.DisabledReason ?? S.Disabled);
            return;
        }

        var page = CurrentPage;
        if (page.MultiSelect && !HasPage(cmd) && !IsPaletteRow(cmd))
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

        if (cmd.ConfirmMessage is not null && _confirmingId != cmd.Id)
        {
            await EnsureOpenAsync();

            // The second Enter runs the active row, so the command being confirmed has to be it. Started from a
            // shortcut or from code, it may not be on the current page at all: it then gets a page of its own.
            var row = _itemRows.FindIndex(r => r.Command.Id == cmd.Id);
            if (row < 0)
            {
                PushPage(new BalettePageFrame { Title = cmd.Title, Items = [cmd], QueryBefore = _query });
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

            // A nested page found from the root opens with its ancestors' pages under it.
            var frame = PushPageWithAncestors(cmd, _query);
            if (cmd.ChildrenProvider is not null)
            {
                await LoadChildrenAsync(frame, cmd);
            }

            return;
        }

        if (cmd.Handler is null && cmd.Href is not null)
        {
            await OpenLinkAsync(cmd, cmd.OpenInNewTab, fromCode);
            return;
        }

        if (cmd.Handler is null)
        {
            await CloseCoreAsync();
            return;
        }

        await RunHandlerAsync(cmd, cmd.Handler, [], null, fromCode);
    }

    /// <summary>Follows a command's <see cref="BaletteCommand.Href"/>, in this tab or a new one, when its scheme is allowed.</summary>
    private async Task OpenLinkAsync(BaletteCommand cmd, bool newTab, bool fromCode)
    {
        if (BaletteUrl.UnsafeScheme(cmd.Href!, Options.AllowedUrlSchemes) is { } scheme)
        {
            BaletteLog.UnsafeLink(Logger, cmd.Id, cmd.Title, scheme);
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

            return BaletteCommandResult.Close;
        }, [], null, fromCode);
    }

    private async Task RunHandlerAsync(BaletteCommand cmd, Func<BaletteCommandContext, Task<BaletteCommandResult>> handler, IReadOnlyList<BaletteCommand> selected, string? promptValue, bool propagate = false)
    {
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
        BaletteCommandResult result;
        try
        {
            result = await handler(ctx);
        }
        catch (OperationCanceledException ex) when (cts.IsCancellationRequested || ex.InnerException is not TimeoutException)
        {
            // The palette cancelled it, or the handler stopped on purpose. A timeout is a failure, reported below:
            // HttpClient reports one as a TaskCanceledException around a TimeoutException.
            return;
        }
        catch (Exception ex)
        {
            _executing = false;
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

        // What the handler did may have shown or hidden commands (VisibleWhen): their shortcuts follow.
        SyncShortcuts();

        if (EffectiveEnableHistory && !cmd.ExcludeFromHistory && cmd.Id.Length > 0 && !cmd.Id.StartsWith("__", StringComparison.Ordinal))
        {
            _ = History.RecordAsync(cmd.Id);
        }

        Service.RaiseExecuted(cmd, result, sw.Elapsed);
        if (OnCommandExecuted.HasDelegate)
        {
            await OnCommandExecuted.InvokeAsync(new BaletteCommandExecutedEventArgs(cmd, result, sw.Elapsed));
        }

        // Closed (or disposed) while the handler ran: what it answers with must not bring the palette back.
        if (cts.IsCancellationRequested)
        {
            return;
        }

        // The page it ran on was left meanwhile (Esc, Backspace): an answer about pages - back, a page, a prompt, a reset -
        // would act on a page it was not meant for. Closing, toasts and errors still apply.
        if (CurrentPage != startPage && result is BaletteCommandResult.BackResult or BaletteCommandResult.PageResult
            or BaletteCommandResult.PromptResult or BaletteCommandResult.ResetResult)
        {
            StateHasChanged();
            return;
        }

        await ApplyResultAsync(cmd, result);
    }

    private async Task ApplyResultAsync(BaletteCommand cmd, BaletteCommandResult result)
    {
        switch (result)
        {
            case BaletteCommandResult.CloseResult:
                await CloseCoreAsync();
                break;
            case BaletteCommandResult.KeepOpenResult:
                Rebuild();
                _pendingFocus = true;
                break;
            case BaletteCommandResult.ResetResult:
                ResetState();
                _pendingFocus = true;
                Rebuild();
                TriggerSourceSearch();
                break;
            case BaletteCommandResult.BackResult when _pages.Count > 1:
                PopPage();
                break;
            case BaletteCommandResult.BackResult:
                // Nothing to go back to: a page's option chosen from the root (SearchNestedCommands) stays there, with
                // the list rebuilt so it shows what the option changed.
                Rebuild();
                _pendingFocus = true;
                break;
            case BaletteCommandResult.PageResult p:
                // A handler run from a global shortcut or from code can answer with UI while the palette is closed.
                await EnsureOpenAsync();
                PushPage(new BalettePageFrame
                {
                    Title = p.Title,
                    Command = null,
                    Items = p.Commands,
                    Placeholder = p.Placeholder,
                    MultiSelect = p.MultiSelect,
                    OnSubmit = p.OnSubmit,
                    QueryBefore = _query,
                });
                break;
            case BaletteCommandResult.PromptResult pr:
                await EnsureOpenAsync();
                PushPage(new BalettePageFrame
                {
                    Title = pr.Title,
                    Command = cmd,
                    Prompt = pr,
                    Placeholder = pr.Placeholder ?? S.PromptPlaceholder,
                    QueryBefore = _query,
                });
                break;
            case BaletteCommandResult.ErrorResult e:
                await EnsureOpenAsync();
                ShowToast(e.Message, error: true);
                _pendingFocus = true;
                break;
            case BaletteCommandResult.ToastResult t:
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
                    await Task.Delay(900);
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
    private async Task ShowBusyLaterAsync(CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(BusyDelay, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await InvokeAsync(() =>
        {
            if (!_executing || _executeCts != cts || !_isOpen)
            {
                return;
            }

            _busy = true;
            _announcement = S.Running;
            StateHasChanged();
        });
    }

    /// <summary>
    /// Raises <see cref="IBaletteService.CommandFailed"/>, then - unless a subscriber handled it - logs the exception, or
    /// rethrows it to the code that called <see cref="IBaletteService.ExecuteAsync"/>. The message the subscriber left is
    /// shown either way; a failed page load shows its error state instead.
    /// </summary>
    private void ReportFailure(BaletteCommand cmd, Exception ex, bool loadingChildren, bool propagate)
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
            BaletteLog.ChildrenFailed(Logger, cmd.Id, cmd.Title, ex);
        }
        else
        {
            BaletteLog.CommandFailed(Logger, cmd.Id, cmd.Title, ex);
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

    private BaletteCommandContext CreateContext(BaletteCommand cmd, string? promptValue, CancellationToken ct, IReadOnlyList<BaletteCommand>? selected = null)
        => new(cmd, _effectiveQuery, _mode?.Prefix, promptValue, selected ?? [], Services, Navigation, Service, ct);

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
    private IEnumerable<IBaletteSource> ActiveSources()
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
        var ctx = new BaletteSourceContext(_mode?.Prefix, frame.Command?.Id, Services);

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
    /// Asks one source and lists what it answers. One that throws or outlives <see cref="BaletteOptions.SourceTimeoutMs"/>
    /// is logged and leaves a row that retries it, while the other sources' results stay: one failing API does not take
    /// the rest of the list with it. An answer to a query, page or mode that is no longer current is dropped.
    /// </summary>
    private async Task SearchSourceAsync(IBaletteSource source, string query, BaletteSourceContext ctx, BalettePageFrame frame, CancellationToken token, string? cursor = null)
    {
        BaletteSourcePage? results = null;
        var timeoutMs = Options.SourceTimeoutMs;
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
            results = await (timeoutMs > 0 ? search.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs), token) : search.WaitAsync(token)) ?? new BaletteSourcePage([]);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex) when (ex is TimeoutException || (ex is OperationCanceledException && timeout.IsCancellationRequested))
        {
            BaletteLog.SourceTimedOut(Logger, source.Name, timeoutMs);
        }
        catch (Exception ex)
        {
            BaletteLog.SourceFailed(Logger, source.Name, ex);
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
                List<BaletteCommand> listed;
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
        var ctx = new BaletteSourceContext(_mode?.Prefix, frame.Command?.Id, Services);
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
        var ctx = new BaletteSourceContext(_mode?.Prefix, frame.Command?.Id, Services);
        var query = _effectiveQuery;
        _ = Task.Run(() => SearchSourceAsync(source, query, ctx, frame, cts.Token, next.Cursor), cts.Token);
    }

    /// <summary>The row after a source's results when it has more: choosing it loads the next page.</summary>
    private BaletteCommand SourceMoreRow(string name, int listed, int? total) => new()
    {
        Id = $"__source-more:{name}",
        Title = total is { } all ? S.Format(S.MoreResults, all - listed) : S.LoadMore,
        Group = name,
        Kind = BaletteCommandKind.Search,
        Icon = IconSvg("more"),
        ExcludeFromHistory = true,
        Tags = [SourceMoreTag],
        Handler = _ =>
        {
            LoadMore(name);
            return Task.FromResult(BaletteCommandResult.KeepOpen);
        },
    };

    /// <summary>A row the palette adds - a source's retry or next page - which is run, never checked, on a multi-select page.</summary>
    private static bool IsPaletteRow(BaletteCommand cmd) => cmd.Tags.Contains(SourceErrorTag) || cmd.Tags.Contains(SourceMoreTag);

    /// <summary>The row a failed source leaves in place of its results: choosing it asks the source again.</summary>
    private BaletteCommand SourceErrorRow(string name) => new()
    {
        Id = $"__source-error:{name}",
        Title = S.Format(S.SourceFailed, name),
        Subtitle = S.Retry,
        Icon = IconSvg("alert"),
        ExcludeFromHistory = true,
        Tags = [SourceErrorTag],
        Handler = _ =>
        {
            RetrySource(name);
            return Task.FromResult(BaletteCommandResult.KeepOpen);
        },
    };

    // ---- Row building -------------------------------------------------------------------------------------

    private IEnumerable<BaletteCommand> AllCommands()
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
    /// (a new collection; one changed in place needs <see cref="IBaletteService.Refresh"/>). Typing never re-reads them.
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
        _allById = new Dictionary<string, BaletteCommand>(StringComparer.Ordinal);
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

        var rows = new List<BaletteRow>();
        var items = new List<BaletteRow>();

        if (page.Prompt is not null)
        {
            _rows = rows;
            _itemRows = items;
            _announcement = string.Empty;
            return;
        }

        var hasQuery = _effectiveQuery.Length > 0;
        var matcher = Options.Matcher;
        var query = BaletteQuery.Parse(_effectiveQuery);

        // Matching the registry dominates the cost of a keystroke; async source results arriving later reuse it.
        if (!reuse)
        {
            _registryMatches = MatchCandidates(page, matcher, query);
            _approximate = false;

            // Nothing matched: a second chance, allowing a typo per term. Only ever after an empty result, so it adds no
            // noise to a query that works.
            if (hasQuery && EffectiveTypoTolerance && query.HasFuzzyTerm(BaletteTypoTolerantMatcher.MinTermLength)
                && _registryMatches.TrueForAll(m => double.IsNegativeInfinity(m.Score)))
            {
                var retried = MatchCandidates(page, new BaletteTypoTolerantMatcher(matcher), query);
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
            matcher = new BaletteTypoTolerantMatcher(matcher);
        }

        var matches = new List<BaletteMatch>(_registryMatches);

        // Interleave sources that opt into matcher ranking.
        var blockSources = new List<(string Name, List<BaletteMatch> Items)>();
        foreach (var source in ActiveSources())
        {
            if (_sourceErrors.Contains(source.Name))
            {
                blockSources.Add((source.Name, [new BaletteMatch(SourceErrorRow(source.Name), 0, [], [], null) { FromSource = true, SourceName = source.Name }]));
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
                    matches.Add(new BaletteMatch(more, double.NegativeInfinity, [], [], null) { FromSource = true, SourceName = source.Name });
                }
            }
            else
            {
                var block = results.Select((c, i) =>
                {
                    var m = hasQuery ? matcher.Match(c, query, Options.Weights) : null;
                    return new BaletteMatch(c, 0, m?.TitlePositions ?? [], m?.SubtitlePositions ?? [], null) { Rank = -i, FromSource = true, SourceName = source.Name };
                }).ToList();
                if (more is not null)
                {
                    block.Add(new BaletteMatch(more, 0, [], [], null) { Rank = -block.Count, FromSource = true, SourceName = source.Name });
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
            headerId = $"{_id}-g-{rows.Count}";
            rows.Add(new BaletteRow(BaletteRowKind.GroupHeader, group, null, -1, headerId) { Position = rows.Count });
        }

        void AddItem(BaletteMatch m, string? group)
        {
            var row = new BaletteRow(BaletteRowKind.Item, group, m, itemCounter, $"{_id}-i-{itemCounter}") { Position = rows.Count, GroupHeaderId = headerId };
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
            var byId = new Dictionary<string, BaletteMatch>(matches.Count, StringComparer.Ordinal);
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

                byId[id] = new BaletteMatch(nested, 0, [], [], null) { Path = chain.Titles, InheritedGroup = chain.RootGroup };
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

    private List<BaletteMatch> MatchCandidates(BalettePageFrame page, IBaletteMatcher matcher, BaletteQuery query)
    {
        var matches = new List<BaletteMatch>();
        foreach (var c in Candidates(page))
        {
            var m = matcher.Match(c, query, Options.Weights);
            if (m is null)
            {
                if (c.AlwaysShow)
                {
                    matches.Add(new BaletteMatch(c, double.NegativeInfinity, [], [], null));
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
    private List<BaletteCommand>? AncestorsOf(BaletteCommand cmd)
    {
        var ancestors = new List<BaletteCommand>();
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
    private BalettePageFrame PushPageWithAncestors(BaletteCommand cmd, string queryBefore)
    {
        if (CurrentPage.IsRoot && cmd.Parent is not null && AncestorsOf(cmd) is { } ancestors)
        {
            foreach (var ancestor in ancestors)
            {
                PushPage(PageFor(ancestor, queryBefore));
                queryBefore = string.Empty;
            }
        }

        var frame = PageFor(cmd, queryBefore);
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

    private IEnumerable<BaletteCommand> Candidates(BalettePageFrame page)
    {
        IEnumerable<BaletteCommand> source;
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

    private IEnumerable<BaletteCommand> ModeHints() => RegisteredModes.Where(m => m.ShowHint).Select(m => new BaletteCommand
    {
        Id = $"__mode:{m.Prefix}",
        Title = m.Name,
        Subtitle = m.Description,
        Group = S.Modes,
        Kind = BaletteCommandKind.Search,
        Value = m.Prefix,
        Badge = m.Prefix,
        Priority = BaletteCommandPriority.Lowest,
        ExcludeFromHistory = true,
        Tags = [ModeHintTag],
    });

    // ---- JS plumbing ---------------------------------------------------------------------------------------

    private List<string> ToggleShortcutStrings()
    {
        var toggles = new List<string>();
        if (Inline)
        {
            return toggles;
        }

        if (ToggleShortcut is not null && BaletteKeyboardShortcut.TryParse(ToggleShortcut, out var ts))
        {
            toggles.Add(ts.Normalized);
        }
        else if (Options.ToggleShortcut is not null)
        {
            toggles.Add(Options.ToggleShortcut.Normalized);
        }

        toggles.AddRange(Options.AdditionalToggleShortcuts.Select(s => s.Normalized));
        return toggles;
    }

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
    /// the way <see cref="BaletteKeyboardShortcut.Normalized"/> writes them, with and without the platform modifier.
    /// </summary>
    private static readonly HashSet<string> BrowserReservedShortcuts = new(StringComparer.Ordinal)
    {
        "mod+w", "mod+t", "mod+n", "mod+shift+w", "mod+shift+t", "mod+shift+n", "mod+q",
        "ctrl+w", "ctrl+t", "ctrl+n", "ctrl+shift+w", "ctrl+shift+t", "ctrl+shift+n",
        "ctrl+tab", "ctrl+shift+tab", "mod+tab", "alt+f4",
    };

    // Only a command that can run now: the browser layer claims a matched key before .NET sees it, so the shortcut of a
    // disabled or hidden command would swallow the key and run nothing.
    private static bool ClaimsGlobalShortcut(BaletteCommand c)
        => c is { Shortcut: not null, GlobalShortcut: true, Disabled: false } && c.IsVisible?.Invoke() != false;

    // From the snapshot, one command per id: OnGlobalShortcut looks the command up there, so a shortcut of a duplicate
    // it never finds must not be claimed either.
    private List<string> GlobalShortcutStrings() => Inline ? [] : _all
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
    private BaletteCommand? ShortcutOwner(string normalized)
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

        var owners = new Dictionary<string, List<BaletteCommand>>(StringComparer.Ordinal);
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
                BaletteLog.BrowserReservedShortcut(Logger, shortcut, winner);
            }

            if (toggles.Contains(shortcut))
            {
                if (_reportedShortcutProblems.Add($"toggle|{shortcut}|{winner}"))
                {
                    BaletteLog.ShortcutShadowedByToggle(Logger, shortcut, winner);
                }

                continue;
            }

            if (list.Count > 1 && _reportedShortcutProblems.Add($"conflict|{shortcut}|{string.Join('|', list.Select(c => c.Id))}"))
            {
                BaletteLog.ShortcutConflict(Logger, shortcut, string.Join(", ", list.Select(c => $"'{c.Id}'")), winner);
            }

            var chords = shortcut.Split(' ');
            for (var n = 1; n < chords.Length; n++)
            {
                var prefix = string.Join(' ', chords, 0, n);
                if ((owners.ContainsKey(prefix) || toggles.Contains(prefix)) && _reportedShortcutProblems.Add($"prefix|{shortcut}|{prefix}"))
                {
                    BaletteLog.UnreachableSequence(Logger, shortcut, winner, prefix);
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

    private static bool IsInteropFailure(Exception ex)
        => ex is JSException or JSDisconnectedException or OperationCanceledException or ObjectDisposedException or InvalidOperationException;

    private ILogger Logger => _logger ??= Services.GetService(typeof(ILogger<Balette>)) as ILogger ?? NullLogger.Instance;

    // ---- Effective settings ----------------------------------------------------------------------------------

    private BaletteStrings S => Strings ?? Options.Strings;

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

    private string EffectiveTheme => Theme ?? Options.Theme;

    private string EffectiveDensity => Density ?? Options.Density;

    private IReadOnlyCollection<BaletteCommandMode> RegisteredModes => IncludeRegisteredCommands ? Service.Modes : [];

    private string RootClass => string.Join(' ', new[] { Unstyled ? null : "bit-balette-dialog", Inline && !Unstyled ? "bit-balette-inline" : null, Class }.Where(c => !string.IsNullOrEmpty(c)));

    private string? HoverSuspended => _pointerSelectionSuspended && !DisablePointerSelection
        ? _hoverSuspension.ToString(System.Globalization.CultureInfo.InvariantCulture)
        : null;

    /// <summary>Whether the list is a listbox now: it has rows, and the page is not a prompt.</summary>
    private bool HasListbox => CurrentPage.Prompt is null && _rows.Count > 0;

    /// <summary>A lazily loaded page or an async source has not answered yet.</summary>
    private bool IsLoading => _sourcesLoading || CurrentPage.Loading;

    /// <summary>
    /// Virtualization sizes the list from <see cref="BaletteOptions.RowHeight"/>, so while it is on the rows are pinned
    /// to that height inline. Otherwise the stylesheet's <c>--bit-balette-row-height</c> token is left alone, where an
    /// app's own override of it can reach the rows.
    /// </summary>
    private string? RowHeightStyle => EffectiveVirtualize
        ? $"--bit-balette-row-height:{Options.RowHeight.ToString(System.Globalization.CultureInfo.InvariantCulture)}px"
        : null;

    /// <summary>
    /// Searches slower than this defer to the next frame so typing stays responsive. Only on WebAssembly, where search and
    /// painting share one thread; on the server each keystroke already round-trips, so another hop would only add latency.
    /// </summary>
    internal TimeSpan SlowSearchThreshold { get; set; } = OperatingSystem.IsBrowser() ? TimeSpan.FromMilliseconds(8) : TimeSpan.MaxValue;

    private IReadOnlyList<string> Breadcrumbs => _pages.Skip(1).Select(p => p.Title).ToList();

    private BaletteStateContext StateContext => new(
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
