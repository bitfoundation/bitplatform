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
    private string? _lastSetupSnapshot;
    private string? _lastPolicySnapshot;
    private bool _toolbarRovingEnabled;
    private ElementReference _editorRef = default!;
    private BitRichTextEditorContentFacts _facts;
    private BitRichTextEditorSelectionState _state = new();
    private DotNetObjectReference<BitRichTextEditor>? _dotnetObj = null;

    /// <summary>Transient inline error message shown in the editor chrome.</summary>
    private string? _inlineError;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Custom CSS classes for different parts of the rich text editor.
    /// </summary>
    [Parameter] public BitRichTextEditorClassStyles? Classes { get; set; }

    /// <summary>
    /// Debounce window (ms) for content-change notifications while typing.
    /// </summary>
    [Parameter] public int DebounceMs { get; set; } = 200;

    /// <summary>
    /// Minimum height of the editing surface (any CSS length).
    /// </summary>
    [Parameter] public string Height { get; set; } = "300px";

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
    /// The placeholder value of the editor shown while it is empty.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// Makes the editor readonly.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Whether the formatting toolbar is shown.
    /// </summary>
    [Parameter] public bool ShowToolbar { get; set; } = true;

    /// <summary>
    /// Custom CSS styles for different parts of the rich text editor.
    /// </summary>
    [Parameter] public BitRichTextEditorClassStyles? Styles { get; set; }

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
    /// Runs a raw editing command against the editor.
    /// </summary>
    public Task ExecuteCommandAsync(string command, string? value = null) => ExecAsync(command, value);



    private bool ControlsDisabled => ReadOnly || _inSourceView;

    private bool Has(BitRichTextEditorToolbar group) => Toolbar.HasFlag(group);



    // ---- callbacks from JS ----

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
    [JSInvokable("OnFactsChanged")]
    public void _OnFactsChanged(BitRichTextEditorContentFacts facts)
    {
        _facts = facts;
        if (ShowCount)
        {
            StateHasChanged();
        }
    }

    [JSInvokable("OnSelectionChanged")]
    public void _OnSelectionChanged(BitRichTextEditorSelectionState state)
    {
        _state = state;
        StateHasChanged();
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
            string.Format(Label("command-failed", "Command '{0}' failed."), command)));
    }



    // ---- commands ----

    private async Task ExecAsync(string command, string? value = null)
    {
        if (ControlsDisabled) return;
        await _js.BitRichTextEditorExec(_editorRef, command, value);
    }

    private Task UndoAsync() => ExecAsync("undo");
    private Task RedoAsync() => ExecAsync("redo");

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
        ClassBuilder.Register(() => ReadOnly ? "bit-rte-ro" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
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
        PlainTextPaste = PasteAsPlainText,
        MaxLength = MaxLength,
        ShortcutKeys = BuildOwnedShortcutCombos()
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

        _dotnetObj?.Dispose();

        try
        {
            await _js.BitRichTextEditorDispose(_editorRef);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        await base.DisposeAsync(disposing);
    }
}
