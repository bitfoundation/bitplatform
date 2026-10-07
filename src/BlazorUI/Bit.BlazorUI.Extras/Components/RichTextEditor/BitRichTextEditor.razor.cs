using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitRichTextEditor is a native WYSIWYG rich text editor. All component logic lives in C#;
/// a thin JavaScript bridge handles the browser-only concerns (contenteditable events,
/// formatting commands, and selection). Two-way bind the HTML content with <c>@bind-Value</c>.
/// </summary>
public partial class BitRichTextEditor : BitComponentBase
{
    private bool _initialized;
    private string _currentHtml = "";
    // What the editing surface is rendered with: the value as it stood until the first render, sanitized, and frozen
    // after it (see the surface in the markup).
    private MarkupString _surfaceSeed;
    // The value and the policy _surfaceSeed was sanitized from.
    private (string? Value, BitRichTextEditorSanitizationPolicy? Policy)? _surfaceSeedSource;
    private string? _lastSetupSnapshot;
    private string? _lastPolicySnapshot;
    private bool _toolbarRovingEnabled;
    private ElementReference _editorRef = default!;
    private BitRichTextEditorContentFacts _facts;
    private BitRichTextEditorSelectionState _state = new();
    private DotNetObjectReference<BitRichTextEditor>? _dotnetObj = null;

    /// <summary>Transient inline error message shown in the editor chrome.</summary>
    private string? _inlineError;

    /// <summary>What the polite live region says about a change nothing on screen announces (a checked task).</summary>
    private string? _announcement;

    // Set when a panel or a menu closes from its own controls, so the focus that was inside it (and is about to be
    // removed with it) goes back to the text on the render that follows instead of falling to the page.
    private bool _pendingEditorFocus;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the rich text editor component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration (the localizer of a localized app, its toolbar, its
    /// sanitization policy and its image upload handler, above all) to be applied to multiple editors through the
    /// <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitRichTextEditorParams.ParamName)]
    public BitRichTextEditorParams? CascadingParameters { get; set; }



    /// <summary>
    /// Automatically moves keyboard focus into the editor after the first render.
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// Turns a URL typed into the editor into a link as soon as the word is finished.
    /// </summary>
    [Parameter] public bool AutoLink { get; set; } = true;

    /// <summary>
    /// Custom CSS classes for different parts of the rich text editor.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitRichTextEditorClassStyles? Classes { get; set; }

    private int _debounceMs = 200;
    /// <summary>
    /// Debounce window (ms) for content-change notifications while typing. Negative values are
    /// rejected and treated as 0 so the bridge never receives an invalid timer interval.
    /// </summary>
    [Parameter]
    public int DebounceMs
    {
        get => _debounceMs;
        set => _debounceMs = value < 0 ? 0 : value;
    }

    /// <summary>
    /// The helper text shown in the footer of the editor (what to write, a format to follow). The editing surface is
    /// described by it, so a screen reader reads it along with the name on focus.
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// The message shown under the editor when its content was rejected, which turns a red frame into something the
    /// reader can act on. Setting it marks the editor invalid on its own - the same look and aria-invalid that
    /// <see cref="Invalid"/> gives it - describes the editing surface by it and announces it once as it appears.
    /// </summary>
    /// <remarks>
    /// It is meant for a rejection the app itself knows about (a server response, a rule spanning two fields). An
    /// editor inside an <c>EditForm</c> already gets its messages from the cascading EditContext through the
    /// <c>ValidationMessage</c> component.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The height the editing surface starts at (any CSS length); it grows with the content from there, up to
    /// <see cref="MaxHeight"/>. Null leaves it to the --bit-RichTextEditor-height CSS variable (300px by default).
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Height { get; set; }

    /// <summary>
    /// Marks the content as invalid, which gives a rejection by something other than the cascading EditContext - a
    /// server, a rule of the app - the same error border and aria-invalid a failing data annotation gives it. An
    /// editor failing its own field validation stays invalid regardless of this parameter.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Invalid { get; set; }

    /// <summary>
    /// The visible label of the editor, rendered above the toolbar. It also names the editing surface for assistive
    /// technologies (taking the place of <see cref="BitComponentBase.AriaLabel"/>), and clicking it moves the focus
    /// into the text.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Maximum height of the editing surface (any CSS length). Content beyond it scrolls inside
    /// the editor instead of growing the page. Null leaves it to the --bit-RichTextEditor-max-height CSS variable
    /// (unbounded by default).
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? MaxHeight { get; set; }

    /// <summary>
    /// Callback for when the editor loses focus.
    /// </summary>
    [Parameter] public EventCallback OnBlur { get; set; }

    /// <summary>
    /// Callback for when the editor content changes.
    /// </summary>
    [Parameter] public EventCallback<string?> OnChange { get; set; }

    /// <summary>
    /// Callback for when the editor encounters a recoverable error (invalid input, etc.).
    /// </summary>
    [Parameter] public EventCallback<BitRichTextEditorError> OnError { get; set; }

    /// <summary>
    /// Callback for when the editor gains focus.
    /// </summary>
    [Parameter] public EventCallback OnFocus { get; set; }

    /// <summary>
    /// Callback for when the selection - or the formatting under it - changes. Receives the same
    /// snapshot the toolbar highlights itself from, so a host can drive its own chrome (a custom
    /// toolbar, a status bar) from the caret without reaching into the browser.
    /// </summary>
    [Parameter] public EventCallback<BitRichTextEditorSelectionState> OnSelectionChange { get; set; }

    /// <summary>
    /// The placeholder value of the editor shown while it is empty.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// Makes the editor readonly.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Marks the editor as required: the editing surface reports <c>aria-required</c> and the <see cref="Label"/>
    /// carries an asterisk. The rule itself is the bound model's (a [Required] attribute in an EditForm).
    /// </summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>
    /// Lets the reader drag the bottom edge of the editing surface to make it taller or shorter,
    /// the way the source-view textarea already can be resized.
    /// </summary>
    [Parameter] public bool Resizable { get; set; }

    /// <summary>
    /// Whether a small formatting toolbar floats next to the current text selection.
    /// </summary>
    [Parameter] public bool ShowQuickToolbar { get; set; }

    /// <summary>
    /// Whether the formatting toolbar is shown.
    /// </summary>
    [Parameter] public bool ShowToolbar { get; set; } = true;

    /// <summary>
    /// Replaces common text patterns with their typographic characters as they are typed: straight
    /// quotes become curly ones, <c>--</c> an em dash, <c>...</c> an ellipsis, <c>(c)</c> a
    /// copyright sign, and the arrow, fraction and comparison shorthands their real symbols. Off by
    /// default, since a document full of code or measurements wants what was typed.
    /// </summary>
    [Parameter] public bool SmartTypography { get; set; }

    /// <summary>
    /// Whether the browser's native spell checking runs over the editor content.
    /// </summary>
    [Parameter] public bool SpellCheck { get; set; } = true;

    /// <summary>
    /// Keeps the toolbar in view while the page scrolls past a tall editor, pinned the
    /// --bit-RichTextEditor-toolbar-sticky-offset CSS variable below the top of the scrolling area.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool StickyToolbar { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the rich text editor.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitRichTextEditorClassStyles? Styles { get; set; }

    /// <summary>
    /// Which toolbar groups to display.
    /// </summary>
    [Parameter] public BitRichTextEditorToolbar Toolbar { get; set; } = BitRichTextEditorToolbar.All;

    /// <summary>
    /// The two-way bound HTML content of the editor.
    /// </summary>
    [Parameter, TwoWayBound, CallOnSet(nameof(OnValueSet))]
    public string? Value { get; set; }



    /// <summary>
    /// Moves keyboard focus into the editor.
    /// </summary>
    public async ValueTask FocusAsync()
    {
        // While source view is active the WYSIWYG surface is detached/hidden and the raw-HTML
        // textarea drives editing, so route focus there instead of the hidden _editorRef.
        if (_inSourceView)
        {
            await _sourceRef.FocusAsync();
            return;
        }
        await _js.BitRichTextEditorFocus(_editorRef);
    }

    /// <summary>
    /// Returns the current HTML content of the editor.
    /// </summary>
    public async ValueTask<string> GetHtmlAsync()
    {
        if (_initialized is false) return _currentHtml;
        // While source view is active the WYSIWYG element is detached/hidden and the raw-HTML
        // textarea (_sourceText) drives the live content, so reading the DOM would return stale
        // markup. Return the source buffer instead in that mode.
        if (_inSourceView) return _sourceText;
        return await _js.BitRichTextEditorGetHtml(_editorRef);
    }

    /// <summary>
    /// Returns the current content of the editor as plain text: visible text only, with block
    /// boundaries and line breaks rendered as newlines and all markup removed.
    /// </summary>
    public async ValueTask<string> GetTextAsync()
    {
        // Before the JS bridge is set up there is no editor DOM to read and interop may not be
        // available (prerendering); the cached content is empty at that point, so return empty.
        if (_initialized is false) return "";
        // While source view is active the WYSIWYG element is detached/hidden and the raw-HTML
        // textarea (_sourceText) drives the live content, so extract the text from the source
        // buffer instead of the stale editor DOM in that mode.
        if (_inSourceView) return await _js.BitRichTextEditorHtmlToText(_editorRef, _sourceText);
        return await _js.BitRichTextEditorGetText(_editorRef);
    }

    /// <summary>
    /// Returns the plain text of the current selection, or an empty string when nothing inside the
    /// editor is selected.
    /// </summary>
    public async ValueTask<string> GetSelectedTextAsync()
    {
        if (_initialized is false || _inSourceView) return "";
        return await _js.BitRichTextEditorGetSelectedText(_editorRef);
    }

    /// <summary>
    /// The formatting under the current selection - the snapshot the toolbar highlights itself from and
    /// <see cref="OnSelectionChange"/> reports - so a custom toolbar item can act on what is at the caret.
    /// </summary>
    public BitRichTextEditorSelectionState SelectionState => _state;

    /// <summary>
    /// Runs a raw editing command against the editor.
    /// </summary>
    public Task ExecuteCommandAsync(string command, string? value = null) => ExecAsync(command, value);

    /// <summary>
    /// Inserts plain text at the current caret position, honoring <see cref="MaxLength"/>.
    /// </summary>
    public async Task InsertTextAsync(string text)
    {
        if (ControlsDisabled || string.IsNullOrEmpty(text)) return;
        await _js.BitRichTextEditorInsertText(_editorRef, text);
    }

    /// <summary>
    /// Inserts HTML at the current caret position. The markup is run through the active
    /// sanitization policy first, so it can never introduce content the editor would strip.
    /// </summary>
    public async Task InsertHtmlAsync(string html)
    {
        if (ControlsDisabled || string.IsNullOrEmpty(html)) return;
        await _js.BitRichTextEditorInsertHtml(_editorRef, html);
    }

    /// <summary>
    /// Replaces the whole content with the given HTML (sanitized), or clears it when null/empty.
    /// </summary>
    public async Task SetHtmlAsync(string? html)
    {
        if (ControlsDisabled) return;
        var next = html ?? "";
        if (string.IsNullOrEmpty(next) is false)
        {
            next = await _js.BitRichTextEditorSanitizeHtml(_editorRef, next);
        }
        // Always replace the DOM: _currentHtml lags an edit whose debounced OnContentChanged has
        // not run yet, so a reset to that stale value must still overwrite what the user typed.
        // Only a real change to the cached value is published.
        await _js.BitRichTextEditorSetHtml(_editorRef, next);
        if (next == _currentHtml) return;
        _currentHtml = next;
        await AssignValue(next);
        NotifyEditContextChanged();
        await OnChange.InvokeAsync(next);
    }

    /// <summary>Clears the editor content.</summary>
    public Task ClearAsync() => SetHtmlAsync(null);

    /// <summary>Undoes the last edit.</summary>
    public Task UndoAsync() => ExecAsync("undo");

    /// <summary>Redoes the last undone edit.</summary>
    public Task RedoAsync() => ExecAsync("redo");

    /// <summary>Selects the whole editor content.</summary>
    public async ValueTask SelectAllAsync()
    {
        if (_initialized is false || _inSourceView) return;
        await _js.BitRichTextEditorSelectAll(_editorRef);
    }



    /// <summary>
    /// The effective read-only state: an editor is locked either by ReadOnly or by being disabled
    /// through the inherited Disabled parameter, and both must reach the surface, the bridge, and
    /// the toolbar identically.
    /// </summary>
    private bool EffectiveReadOnly => ReadOnly || Disabled;

    private bool ControlsDisabled => EffectiveReadOnly || _inSourceView;

    private string LabelId => $"{UniqueId}-label";

    private string ErrorMessageId => $"{UniqueId}-error-message";

    private string DescriptionId => $"{UniqueId}-description";

    private string HintId => $"{UniqueId}-hint";

    /// <summary>
    /// The ids the content is described by, what is wrong first: the error message, the description, the count footer,
    /// and the inline error while one is shown. The source view reads them as they are.
    /// </summary>
    private string? FieldDescribedBy
    {
        get
        {
            var ids = (ErrorMessage.HasValue() ? $"{ErrorMessageId} " : "")
                    + (Description.HasValue() ? $"{DescriptionId} " : "")
                    + (ShowCount ? $"{UniqueId}-count " : "")
                    + (_inlineError is null ? "" : $"{UniqueId}-error");
            return ids.Length == 0 ? null : ids.TrimEnd();
        }
    }

    /// <summary>
    /// What the editing surface is described by: <see cref="FieldDescribedBy"/>, then the hint that Alt+0 lists the
    /// keyboard shortcuts - which only the surface answers to.
    /// </summary>
    private string SurfaceDescribedBy => FieldDescribedBy is { } ids ? $"{ids} {HintId}" : HintId;

    /// <summary>
    /// Whether the floating selection toolbar should be on screen: it is opt-in, needs a real
    /// selection to anchor to, and never competes with the slash menu, an open tool panel (whose
    /// fields it would float over), or a disabled surface.
    /// </summary>
    private bool ShowingQuickToolbar
        => ShowQuickToolbar && _state.HasSelection && ControlsDisabled is false
        && _showSlash is false && _showMention is false && AnyPanelOpen is false;

    /// <summary>Whether one of the inline tool bars under the toolbar is currently showing.</summary>
    private bool AnyPanelOpen
        => _showLinkInput || _showImageInput || _showMediaInput || _showTableInput || _showFind || _showEmoji || _showColor || _showHelp;

    /// <summary>
    /// Places the selection toolbar just above the selection, in the component root's coordinates.
    /// The values are formatted with the invariant culture: a comma decimal separator would make
    /// the browser drop the declaration under a locale like fr-FR.
    /// </summary>
    private string QuickToolbarStyle
    {
        get
        {
            // Roughly the toolbar's own height plus a small gap, so it sits clear of the text; when
            // the selection is near the top of the component it flips below instead of off-screen.
            const double offset = 44;
            var top = _state.SelectionTop >= offset
                ? _state.SelectionTop - offset
                : _state.SelectionTop + _state.SelectionHeight + 8;
            var left = Math.Max(0, _state.SelectionLeft);
            return string.Create(System.Globalization.CultureInfo.InvariantCulture,
                $"top:{top:0.##}px;left:{left:0.##}px");
        }
    }

    private bool Has(BitRichTextEditorToolbar group) => Toolbar.HasFlag(group);

    private static readonly HashSet<string> BlockFormats = ["p", "h1", "h2", "h3", "h4", "h5", "h6", "blockquote", "pre"];

    /// <summary>
    /// The paragraph format selector's value: the block at the caret, or Normal while it is one the selector does
    /// not offer (a list item, a table cell, nothing reported yet), which would otherwise draw the selector blank.
    /// </summary>
    private string SelectedBlockFormat => BlockFormats.Contains(_state.Block) ? _state.Block : "p";



    // ---- callbacks from JS ----

    // Preserve the callback argument types under trimming: they are only ever produced by the JS
    // bridge, so nothing in C# statically references their constructors/setters and the trimmer
    // would otherwise break (facts: constructor parameter names) or silently default (state:
    // property setters) their deserialization in release builds.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitRichTextEditorContentFacts))]
    [JSInvokable("OnContentChanged")]
    public async Task _OnContentChanged(string html, BitRichTextEditorContentFacts facts)
    {
        _currentHtml = html;
        _facts = facts;
        if (ShowCount)
        {
            StateHasChanged();
        }

        await AssignValue(html);
        NotifyEditContextChanged();
        await OnChange.InvokeAsync(html);
    }

    /// <summary>
    /// Reported by the bridge after a programmatic content set (e.g. a bound Value assignment):
    /// refreshes the cached content facts so count-dependent UI stays accurate, without treating
    /// the change as a user edit (no AssignValue / OnChange).
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitRichTextEditorContentFacts))]
    [JSInvokable("OnFactsChanged")]
    public void _OnFactsChanged(BitRichTextEditorContentFacts facts)
    {
        _facts = facts;
        if (ShowCount)
        {
            StateHasChanged();
        }
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitRichTextEditorSelectionState))]
    [JSInvokable("OnSelectionChanged")]
    public Task _OnSelectionChanged(BitRichTextEditorSelectionState state)
    {
        _state = state;
        StateHasChanged();
        return OnSelectionChange.InvokeAsync(state);
    }

    /// <summary>
    /// Reported by the bridge when a checklist item is ticked or unticked from the keyboard or by its marker: the
    /// marker is drawn by CSS, so the new state is said through the live region.
    /// </summary>
    [JSInvokable("OnTaskToggled")]
    public void _OnTaskToggled(bool isChecked)
    {
        Announce(isChecked ? Loc("task-checked", "Task checked") : Loc("task-unchecked", "Task unchecked"));
    }

    [JSInvokable("OnFocused")]
    public Task _OnFocused() => OnFocus.InvokeAsync();

    [JSInvokable("OnBlurred")]
    public Task _OnBlurred() => OnBlur.InvokeAsync();

    /// <summary>Reported by the bridge when a formatting command fails; content is unchanged.</summary>
    [JSInvokable("OnCommandError")]
    public Task _OnCommandError(string command, string message)
    {
        // Keep the raw JS bridge detail (browser internals) out of the user-facing message; log
        // it for diagnostics instead. Use Trace (not Debug) so it is still recorded in Release.
        System.Diagnostics.Trace.TraceError($"BitRichTextEditor command '{command}' failed: {message}");
        // Surface a consistent localized message tied to the command-failed key, matching the
        // other error paths (e.g. custom-action-failed) rather than exposing bridge internals.
        return RaiseErrorAsync(new BitRichTextEditorError("command-failed",
            string.Format(Loc("command-failed", "Command '{0}' failed."), command)));
    }



    // ---- commands ----

    private async Task ExecAsync(string command, string? value = null)
    {
        if (ControlsDisabled) return;
        await _js.BitRichTextEditorExec(_editorRef, command, value);
    }

    private Task OnBlockFormatChanged(ChangeEventArgs e)
        => ExecBlockAsync(e.Value?.ToString() ?? "p");

    private async Task ExecBlockAsync(string tag)
    {
        if (ControlsDisabled) return;
        await _js.BitRichTextEditorExecBlock(_editorRef, tag);
    }

    private Task FormatBlockToggleAsync(string tag)
        => ExecBlockAsync(_state.Block == tag ? "p" : tag);

    private async Task ClearFormattingAsync()
    {
        if (ControlsDisabled) return;
        await _js.BitRichTextEditorExec(_editorRef, "removeFormat", null);
        await _js.BitRichTextEditorExecBlock(_editorRef, "p");
    }



    // ---- helpers ----

    // The inline panels (link, image, media, find, emoji) open in response to a toolbar click,
    // so without this the caret would stay in the editor and the panel's first field would have to
    // be reached with a Tab. Each panel registers the field it wants focused and the focus is moved
    // on the render that follows, once the element actually exists.
    private Func<ElementReference>? _pendingPanelFocus;

    private void RequestPanelFocus(Func<ElementReference> target) => _pendingPanelFocus = target;

    /// <summary>
    /// Escape anywhere in a tool panel - a field, a checkbox, one of its buttons, the emoji grid - closes it and puts
    /// the focus back in the text.
    /// </summary>
    private static async Task OnPanelKeyDown(KeyboardEventArgs e, Func<Task> close)
    {
        if (e.Key == "Escape") await close();
    }

    /// <summary>Sends the focus back to the text on the next render (a panel or a menu is closing under it).</summary>
    private void RequestEditorFocus() => _pendingEditorFocus = true;

    private async Task FocusEditorIfPendingAsync()
    {
        if (_pendingEditorFocus is false) return;
        _pendingEditorFocus = false;
        if (_initialized is false || _inSourceView || Disabled) return;
        try
        {
            await _js.BitRichTextEditorRestoreFocus(_editorRef);
        }
        catch (JSDisconnectedException) { } // circuit gone; nothing to focus
        catch (JSException) { } // interop unavailable
    }

    // A live region only speaks when its text changes, so the same message twice in a row (two tasks checked one
    // after the other) is told apart by an invisible character.
    private void Announce(string message)
    {
        SetAnnouncement(message);
        StateHasChanged();
    }

    private void SetAnnouncement(string message)
        => _announcement = _announcement == message ? message + "\u200B" : message;

    // The error message last said through the live region, so a message is announced once as it appears rather than
    // on every render that keeps it.
    private string? _announcedErrorMessage;

    private void AnnounceErrorMessageIfNew()
    {
        if (_initialized && ErrorMessage.HasValue() && ErrorMessage != _announcedErrorMessage)
        {
            SetAnnouncement(ErrorMessage!);
        }
        _announcedErrorMessage = ErrorMessage;
    }

    /// <summary>
    /// Closes every inline panel except the one being opened. They all occupy the same strip under
    /// the toolbar, so leaving two open stacks unrelated bars over the editor and makes the
    /// aria-expanded state of the toolbar buttons disagree with what is on screen.
    /// </summary>
    private async Task CloseOtherPanels(string keep)
    {
        if (keep != "link") { _showLinkInput = false; _linkUrl = ""; _linkText = ""; _linkNewTab = false; }
        if (keep != "image") { _showImageInput = false; _imageUrl = ""; _imageAlt = ""; }
        if (keep != "media") { _showMediaInput = false; _mediaUrl = ""; }
        if (keep != "table") { _showTableInput = false; }
        if (keep != "emoji") { _showEmoji = false; _emojiSearch = ""; }
        if (keep != "color") { _showColor = false; }
        if (keep != "help") { _showHelp = false; }
        if (keep != "find" && _showFind)
        {
            // The find panel is one of the same strip of bars, so opening another tool closes it -
            // and closing it has to take its highlight markup out of the content with it, exactly
            // as ToggleFind does.
            _showFind = false;
            _findTerm = "";
            _replaceTerm = "";
            ResetFindCount();
            await ClearFindAsync();
        }
    }

    private async Task FocusPanelIfPendingAsync()
    {
        if (_pendingPanelFocus is null) return;
        var target = _pendingPanelFocus;
        _pendingPanelFocus = null;
        await target().FocusSafelyAsync();
    }

    private async Task RaiseErrorAsync(BitRichTextEditorError error)
    {
        _inlineError = error.Message;
        StateHasChanged();
        await OnError.InvokeAsync(error);
    }

    private void ClearInlineError()
    {
        if (_inlineError is not null)
        {
            _inlineError = null;
            StateHasChanged();
        }
    }



    protected override string RootElementClass => "bit-rte";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
        ClassBuilder.Register(() => _fullScreen ? "bit-rte-fsc" : string.Empty);
        ClassBuilder.Register(() => EffectiveReadOnly ? "bit-rte-ro" : string.Empty);
        ClassBuilder.Register(() => StickyToolbar ? "bit-rte-stk" : string.Empty);
        ClassBuilder.Register(() => IsInvalid ? "bit-inv" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        // The size parameters set the public variables on the instance, so the surface and the source view (which
        // both read them) keep one box, and a full-screen editor can still lift the cap in its stylesheet.
        StyleBuilder.Register(() => Height is null ? string.Empty : $"--bit-RichTextEditor-height:{Height}");
        StyleBuilder.Register(() => MaxHeight is null ? string.Empty : $"--bit-RichTextEditor-max-height:{MaxHeight}");
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitRichTextEditorParams))]
    protected override void OnParametersSet()
    {
        // Before anything below reads the parameters it may fill in. A BitParams that has gone away takes what it
        // had cascaded with it, which the base class has already put back by now.
        CascadingParameters?.UpdateParameters(this);

        // Before the first render there is no bridge to fill the surface - a prerender or a static SSR page never
        // gets one - so the value is put into it here, through the same allowlist the bridge would apply. After it,
        // the surface is the bridge's alone. The interactive render that replaces a prerendered one is seeded the same
        // way, so the surface does not drop to its placeholder until the bridge has filled it. The value is only
        // sanitized again when it, or the policy it is sanitized against, is another one: a parent re-rendering
        // before the first render hands the same ones in again.
        if (IsRendered is false && (_surfaceSeedSource is null ||
                                    ReferenceEquals(_surfaceSeedSource.Value.Value, Value) is false ||
                                    ReferenceEquals(_surfaceSeedSource.Value.Policy, SanitizationPolicy) is false))
        {
            _surfaceSeedSource = (Value, SanitizationPolicy);
            _surfaceSeed = new(BitRichTextEditorHtmlSanitizer.Sanitize(Value, SanitizationPolicy));
        }

        EnsureField();
        TrackEditContext();

        AnnounceErrorMessageIfNew();

        base.OnParametersSet();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        ValidateCustomItems();

        // Keep the JS bridge config aligned with the current C# parameter state. The first
        // render seeds these via BitRichTextEditorSetup; afterwards parameter changes must be
        // pushed explicitly, otherwise the bridge keeps the frozen initial options. Skip the
        // interop call when nothing the bridge cares about (debounce, policy, upload, paste,
        // max-length, owned shortcuts) actually changed since the last push.
        if (_initialized)
        {
            var options = BuildSetupOptions();
            var snapshot = SerializeSetupOptions(options);
            if (snapshot != _lastSetupSnapshot)
            {
                // Detect whether the sanitization policy specifically changed so a tightened
                // allowlist can be re-applied to the already-loaded content, not just future input.
                var policySnapshot = System.Text.Json.JsonSerializer.Serialize(options.Policy);
                var policyChanged = policySnapshot != _lastPolicySnapshot;

                _lastSetupSnapshot = snapshot;
                _lastPolicySnapshot = policySnapshot;
                await _js.BitRichTextEditorUpdateOptions(_editorRef, options);

                // A changed (e.g. tightened) policy must also clean the content already in the
                // editor; otherwise markup permitted under the previous allowlist would linger
                // until the next external Value change. Run the current content through the new
                // policy and push the cleaned result back through the same sync path as OnValueSet.
                if (policyChanged)
                {
                    await ResanitizeCurrentContentAsync();
                }
            }
        }
    }

    private BitRichTextEditorSetupOptions BuildSetupOptions() => new()
    {
        Debounce = DebounceMs,
        Policy = BuildPolicyPayload(),
        HasUpload = OnImageUpload is not null,
        MaxImageBytes = MaxImageSize,
        PlainTextPaste = PasteAsPlainText,
        MaxLength = MaxLength,
        ShortcutKeys = BuildOwnedShortcutCombos(),
        ReadOnly = EffectiveReadOnly,
        AutoLink = AutoLink,
        QuickToolbar = ShowQuickToolbar,
        Mentions = OnMentionSearch is not null,
        SmartTypography = SmartTypography
    };

    // Serializes the setup payload so OnParametersSetAsync can detect whether any bridge-backed
    // setting changed and avoid redundant BitRichTextEditorUpdateOptions interop calls.
    private static string SerializeSetupOptions(BitRichTextEditorSetupOptions options)
        => System.Text.Json.JsonSerializer.Serialize(options);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);

            var setupOptions = BuildSetupOptions();
            await _js.BitRichTextEditorSetup(_editorRef, _dotnetObj, setupOptions);
            _lastSetupSnapshot = SerializeSetupOptions(setupOptions);
            _lastPolicySnapshot = System.Text.Json.JsonSerializer.Serialize(setupOptions.Policy);

            // Sanitize the initial Value through the bridge so the first content load can't bypass
            // sanitization. The bridge enforces a secure default allowlist when no SanitizationPolicy
            // is set, and the custom policy when one is, so sanitize any non-empty HTML either way.
            var html = Value ?? "";
            if (string.IsNullOrEmpty(html) is false)
            {
                html = await _js.BitRichTextEditorSanitizeHtml(_editorRef, html);
            }
            _currentHtml = html;

            if (string.IsNullOrEmpty(_currentHtml) is false)
            {
                await _js.BitRichTextEditorSetHtml(_editorRef, _currentHtml);
            }

            // Sanitization may have changed the markup; push the cleaned HTML back through the
            // binding so @bind-Value cannot retain unsafe content that was stripped for rendering.
            if ((Value ?? "") != html)
            {
                await AssignValue(html);
                NotifyEditContextChanged();
            }

            _initialized = true;

            if (AutoFocus)
            {
                await FocusAsync();
            }
        }

        // Wire (or re-wire) the toolbar roving tabindex whenever the toolbar becomes visible.
        // The JS side is idempotent per element, and resetting the flag when the toolbar is
        // hidden lets a later ShowToolbar=true (a fresh element) initialize again.
        if (ShowToolbar)
        {
            if (_toolbarRovingEnabled is false)
            {
                _toolbarRovingEnabled = true;
                await _js.BitRichTextEditorEnableToolbarRoving(_toolbarRef);
            }
        }
        else
        {
            _toolbarRovingEnabled = false;
        }

        // Move focus into the slash filter input on the render right after the menu opens so the
        // menu is keyboard-driven (filter, arrow navigation, Enter to apply) rather than leaving
        // focus in the editor.
        await FocusSlashIfPendingAsync();
        await FocusMentionIfPendingAsync();
        await FocusPanelIfPendingAsync();
        await EnableEmojiGridIfPendingAsync();
        await EnableColorGridIfPendingAsync();
        await FocusEditorIfPendingAsync();
    }

    private async ValueTask OnValueSet()
    {
        if (_initialized is false) return;
        if ((Value ?? "") == _currentHtml) return; // originated from the editor

        var html = Value ?? "";
        // Sanitize any non-empty HTML through the bridge regardless of SanitizationPolicy: the
        // bridge applies its secure default allowlist when no custom policy is set and the custom
        // policy when one is, so an updated Value can never bypass sanitization.
        if (string.IsNullOrEmpty(html) is false)
        {
            html = await _js.BitRichTextEditorSanitizeHtml(_editorRef, html);
        }
        _currentHtml = html;

        // While source view is open the WYSIWYG surface is detached from the live value, so don't
        // push into the editor element. Instead reflect the external change into the raw-HTML
        // textarea (and the cached _currentHtml above) so leaving source view starts from the
        // latest parent Value rather than the stale content captured when source view was entered.
        if (_inSourceView)
        {
            _sourceText = html;
            StateHasChanged();
        }
        else
        {
            await _js.BitRichTextEditorSetHtml(_editorRef, html);
        }

        // Keep the bound model in sync with the sanitized/rendered content: if the policy
        // stripped anything, write the cleaned HTML back so @bind-Value never holds the
        // unsafe original. The guard above (Value == _currentHtml) short-circuits the
        // re-entrant OnValueSet that this assignment triggers.
        if ((Value ?? "") != html)
        {
            await AssignValue(html);
            NotifyEditContextChanged();
        }
    }



    // Re-runs the editor's current content through the bridge under the now-active policy and
    // synchronizes the cleaned result across _currentHtml, the source-view text, the editor DOM,
    // and the bound Value, mirroring OnValueSet's sync path so a policy change leaves no stale
    // markup behind. Invoked from OnParametersSetAsync when the policy actually changes.
    private async Task ResanitizeCurrentContentAsync()
    {
        // Sanitize the live content rather than the cached _currentHtml: read the source-view text
        // when it is driving the visible content, otherwise pull the latest DOM HTML so newer user
        // input is not overwritten by stale cached markup.
        var html = (_inSourceView
            ? _sourceText
            : (_initialized ? await _js.BitRichTextEditorGetHtml(_editorRef) : _currentHtml)) ?? "";
        if (string.IsNullOrEmpty(html)) return;

        var sanitized = await _js.BitRichTextEditorSanitizeHtml(_editorRef, html);
        // Compare against the live content (html) rather than the cached _currentHtml: if the live
        // DOM/source already matches the sanitized result no resync is needed, but a stale
        // _currentHtml must not short-circuit cleanup while the live content still differs.
        if (sanitized == html) return;

        _currentHtml = sanitized;

        if (_inSourceView)
        {
            _sourceText = sanitized;
            StateHasChanged();
        }
        else
        {
            await _js.BitRichTextEditorSetHtml(_editorRef, sanitized);
        }

        if ((Value ?? "") != sanitized)
        {
            await AssignValue(sanitized);
            NotifyEditContextChanged();
        }
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        UntrackEditContext();

        _dotnetObj?.Dispose();

        try
        {
            await _js.BitRichTextEditorDispose(_editorRef);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        await base.DisposeAsync(disposing);
    }
}
