using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Borm;

/// <summary>Base class shared by all built-in field input components.</summary>
public abstract class BormInputBase : ComponentBase, IDisposable
{
    private CancellationTokenSource? _debounce;
    private bool _focusRequested;
    private bool _autofocusApplied;
    private string? _pendingRaw;
    private bool _hasPendingRaw;
    private BormFieldContext? _pendingContext;
    private int? _liveLength;
    private string? _lastCommitted;
    private string? _typed;
    private BormFieldContext? _typedInto;

    /// <summary>The render context for the field being displayed.</summary>
    [Parameter, EditorRequired] public BormFieldContext Context { get; set; } = default!;

    protected BormFieldDefinition Field => Context.Field;

    /// <summary>
    /// The element the control renders. Renderers bind it with <c>@ref</c> so
    /// <see cref="BormFieldDefinition.Autofocus"/> and focus-the-first-error can actually move
    /// the caret there.
    /// </summary>
    protected ElementReference Element { get; set; }

    /// <summary>True when a condition has disabled the field. Read-only is rendered separately — see <see cref="ReadOnly"/>.</summary>
    protected bool Disabled => Context.IsDisabled;

    /// <summary>True when the field is read-only.</summary>
    protected bool ReadOnly => Context.IsReadOnly;

    /// <summary>
    /// For controls that have no <c>readonly</c> attribute of their own (select, checkbox, radio),
    /// read-only has to be expressed as <c>disabled</c> — there is no other way to stop the browser
    /// changing them.
    /// </summary>
    protected bool DisabledOrReadOnly => Disabled || ReadOnly;

    /// <summary>
    /// The name a read-only control's answer is posted under when it is drawn as <c>disabled</c> (see
    /// <see cref="DisabledOrReadOnly"/>), in a form that posts (<see cref="BormFieldContext.InputName"/>): a
    /// disabled control is never sent, so a hidden input carries the answer instead, as a read-only text box
    /// sends its own. Null when the control posts for itself, the form does not post, or the field is disabled -
    /// which is not sent at all.
    /// </summary>
    protected string? ReadOnlyPostedName => ReadOnly && !Disabled ? Context.InputName : null;

    protected string CssClass => Context.HasError && Context.ShowMessages
        ? "bit-borm-input bit-borm-input--invalid"
        : "bit-borm-input";

    /// <summary>Value for <c>aria-invalid</c>, or null to omit the attribute.</summary>
    protected string? AriaInvalid => Context.AriaInvalid;

    /// <summary>Ids of the help text and error list, for <c>aria-describedby</c>.</summary>
    protected string? DescribedBy => Context.DescribedBy;

    /// <summary>Value for <c>aria-required</c>, or null to omit the attribute.</summary>
    protected string? AriaRequired => Context.IsRequired ? "true" : null;

    /// <summary>Id of the visually hidden "required" note a group of controls points at.</summary>
    protected string RequiredHintId => Context.ElementId + "_required";

    /// <summary>
    /// <c>aria-describedby</c> for a <c>role="group"</c> — a multi-select or a tag list. ARIA does not
    /// allow <c>aria-required</c> on a group, and the asterisk beside the label is hidden from assistive
    /// technology, so without this a screen-reader user was never told the field had to be answered.
    /// The group is described by a hidden note instead (see <see cref="RequiredHintId"/>), ahead of its
    /// help text and messages.
    /// </summary>
    protected string? GroupDescribedBy => !Context.IsRequired ? DescribedBy
        : DescribedBy is { } described ? RequiredHintId + " " + described
        : RequiredHintId;

    /// <summary>
    /// The accessible name to use when the visible label is suppressed. Null the rest of the time,
    /// because a real <c>&lt;label&gt;</c> already points at the control and an <c>aria-label</c> would
    /// silently override it.
    /// </summary>
    protected string? AriaLabel => Field.ShowLabel ? null : Context.Label;

    /// <summary>
    /// Extra attributes declared on the schema, splatted onto the control. Attributes the renderer sets
    /// itself win, so nothing here can break the id/aria wiring.
    /// </summary>
    protected IReadOnlyDictionary<string, object>? ExtraAttributes => Context.InputAttributes;

    /// <summary>Lower bound, formatted invariantly so it is never localised into invalid HTML.</summary>
    protected string? MinAttribute => BormNumber.ToAttribute(Field.Min);

    /// <summary>Upper bound, formatted invariantly.</summary>
    protected string? MaxAttribute => BormNumber.ToAttribute(Field.Max);

    /// <summary>
    /// Whether this field writes back on every keystroke rather than on the browser's <c>change</c>
    /// event. Both handlers are always wired; each is a no-op when it is not the configured one, which
    /// keeps the markup of every renderer identical.
    /// </summary>
    protected bool UpdatesOnInput => Field.UpdateOn == BormUpdateTrigger.Input;

    /// <summary>
    /// Handles the browser's <c>change</c> event. An input-driven field has already written every
    /// keystroke — except one still waiting out its debounce, which is written now.
    /// </summary>
    /// <remarks>
    /// Pressing Enter in a text box commits it (<c>change</c>) and then submits the form, with no blur in
    /// between. A debounced field used to ignore the commit, so the submit — or the wizard's Next —
    /// judged the value as it stood one pause ago, and on a wizard the step then went away and took the
    /// half-written text with it.
    /// </remarks>
    protected Task OnChange(ChangeEventArgs e)
    {
        if (!UpdatesOnInput) return Context.SetFromStringAsync(e.Value?.ToString());
        if (!_partialPending) return FlushAsync();

        // The keystrokes left it to this commit (see OnInput): whatever the box holds now - emptied, or never
        // made into a number - is the answer.
        _partialPending = false;
        return Context.SetFromStringAsync(e.Value?.ToString());
    }

    /// <summary>
    /// Handles the browser's <c>input</c> event. It is only wired at all when something needs it (see
    /// <see cref="InputHandler"/>), so a change-driven field costs no round-trip per keystroke; when it
    /// is wired only for the character counter, the keystroke updates the counter and nothing else.
    /// </summary>
    protected Task OnInput(ChangeEventArgs e) => OnTyped(e.Value?.ToString());

    /// <summary>
    /// The setter of a control bound on <c>input</c> (<c>@bind:set</c>, with <see cref="LiveValue"/> as the
    /// getter), for a field that writes as the user types. Such a control is a binding rather than <c>value</c>
    /// plus an <c>oninput</c> handler: written as an attribute, the value each render carries is put back into the
    /// box, and on Blazor Server a render arrives after the keys typed since it was asked for - so fast typing over
    /// a slow link lost characters. A binding takes the box to hold what the browser last sent, and writes only a
    /// value that differs from it.
    /// </summary>
    protected Task OnTyped(string? raw)
    {
        _typed = raw ?? "";
        _typedInto = Context;
        _liveLength = raw?.Length ?? 0;
        if (!UpdatesOnInput) return Task.CompletedTask;

        // A number or a date half-typed - "-", "1e", a day with no month yet - is reported as no value at all, and
        // written that way it put "0" (or nothing) back into the box under the caret: "-5" became 5, "1e3" became
        // 3. The keystroke is left to the commit, which hears what the box finally holds.
        if (string.IsNullOrEmpty(raw) && Field.Type is BormFieldType.Integer or BormFieldType.Number
                or BormFieldType.Date or BormFieldType.DateTime or BormFieldType.Time)
        {
            CancelDebounce();
            _hasPendingRaw = false;
            _partialPending = true;
            return Task.CompletedTask;
        }

        _partialPending = false;
        return Debounced(raw);
    }

    private bool _partialPending;

    /// <summary>
    /// What a control bound on <c>input</c> shows (its <c>@bind:get</c>): the text typed into it, else
    /// <paramref name="committed"/>. The renderer writes the bound value into the box whenever it differs from
    /// what the browser sent, so it stays the user's text for as long as that stands for what the model holds
    /// (see <see cref="OnParametersSet"/>): the model's own text would erase a debounced word not written yet,
    /// or the "." of a "1." the model holds as 1.
    /// </summary>
    protected string LiveValue(string committed) => _typed ?? committed;

    /// <summary>
    /// The <c>input</c> handler to bind, or a callback with no delegate — which Blazor omits from the
    /// rendered element entirely. That omission is the point: an <c>oninput</c> attribute costs a
    /// server round-trip on every keystroke of a Blazor Server circuit, and most fields write on
    /// <c>change</c> and would do nothing with it.
    /// </summary>
    protected EventCallback<ChangeEventArgs> InputHandler
        => WantsInputEvent ? EventCallback.Factory.Create<ChangeEventArgs>(this, OnInput) : default;

    /// <summary>
    /// Whether this control has any use for the <c>input</c> event: it writes as the user types, or it
    /// shows a live character counter.
    /// </summary>
    protected virtual bool WantsInputEvent => UpdatesOnInput || Field.ShowCharacterCount;

    /// <summary>
    /// The length to show in a character counter: what the user has typed so far, not what has been
    /// committed to the model. Under the default <see cref="BormUpdateTrigger.Change"/> those are
    /// the same thing only on blur, which is precisely when a counter is no longer useful.
    /// </summary>
    protected int DisplayLength => _liveLength ?? Context.StringValue.Length;

    /// <summary>
    /// The value for the HTML <c>maxlength</c> attribute, or null when the limit should be shown rather
    /// than enforced.
    /// </summary>
    /// <remarks>
    /// A field with a live counter deliberately drops it. <c>maxlength</c> is destructive: pasting a
    /// slightly-too-long answer silently truncates it, with no message and nothing to undo. A counter
    /// plus the length rule is the non-destructive equivalent — the user keeps their text, sees the
    /// count go red, and edits it down. This is why GOV.UK's character count removes the attribute too.
    /// </remarks>
    protected int? MaxLengthAttribute => Field.ShowCharacterCount ? null : Field.MaxLength;

    /// <summary>
    /// What a polite live region should say about the character limit, or null when it should stay
    /// silent. The visible counter is <c>aria-hidden</c> — a number re-announced on every keystroke
    /// would bury everything else — but leaving assistive technology with nothing at all means a
    /// screen-reader user meets the limit by having their typing stop working. So the count is
    /// announced only as the limit comes into view, and again once it is passed.
    /// </summary>
    /// <remarks>
    /// The threshold defaults to a tenth of the limit (at least five characters) and can be set per
    /// field with <c>Attr("countAnnounceAt", 25)</c>.
    /// </remarks>
    protected string? CharacterCountAnnouncement
    {
        get
        {
            if (!Field.ShowCharacterCount || Field.MaxLength is not { } max || max <= 0) return null;

            var remaining = max - DisplayLength;
            if (remaining < 0) return Context.Text(BormMessageKeys.CharactersOver, -remaining);

            var threshold = Math.Max(1, Attr("countAnnounceAt", Math.Max(5, max / 10)));
            return remaining <= threshold ? Context.Text(BormMessageKeys.CharactersRemaining, remaining) : null;
        }
    }

    /// <summary>
    /// Writes the value once the user pauses. A newer keystroke supersedes the pending write, so a
    /// debounced field performs one write per pause instead of one per character — the difference
    /// between a remote uniqueness check that is usable and one that is not.
    /// </summary>
    private async Task Debounced(string? raw)
    {
        var delay = Field.DebounceMilliseconds;
        if (delay <= 0)
        {
            await Context.SetFromStringAsync(raw);
            return;
        }

        CancelDebounce();
        _pendingRaw = raw;
        _hasPendingRaw = true;
        // The field the keystroke was typed into. A wizard reuses this component for whatever field
        // sits in the same place on the next step, so by the time the delay elapses Context may be
        // somebody else's — and writing there would put one answer into another question.
        var context = _pendingContext = Context;

        var cts = _debounce = new CancellationTokenSource();
        try
        {
            await Task.Delay(delay, cts.Token);
            // The timer can fire just as the next keystroke arrives, too late for that keystroke to stop the
            // delay: the newer text is the one pending now, and this older one must not land over it.
            if (!ReferenceEquals(_debounce, cts)) return;
            // Read again rather than trusting the one captured above: a repeater row that moved while
            // the timer ran has been retargeted to its new path (see OnParametersSet).
            context = _pendingContext ?? context;
            _hasPendingRaw = false;
            _pendingContext = null;
            // Async rules run here and only here on the typing path. A pause is what a debounce exists
            // to detect, and it is the moment a remote uniqueness check stops being one request per
            // character and becomes one request per thing the user actually typed — which is the whole
            // reason the field asked to be debounced. An undebounced field still waits for blur.
            await context.SetFromStringAsync(raw, includeAsync: true);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a later keystroke, or the component went away. The value is still held in
            // _pendingRaw, so a blur arriving before the delay elapses can still write it.
        }
    }

    protected async Task OnBlur()
    {
        // A pending debounced write has to *land* before the field is judged. Dropping it would judge
        // the value as it stood one keystroke ago — and, worse, leave the user's last characters in the
        // box but never in the model.
        await FlushAsync();
        await Context.BlurAsync();
    }

    /// <summary>
    /// Writes a debounced value that is still waiting out its delay, immediately. Called on blur, so
    /// leaving a field never discards what was typed into it.
    /// </summary>
    protected async Task FlushAsync()
    {
        if (!_hasPendingRaw) return;

        var raw = _pendingRaw;
        var context = _pendingContext ?? Context;
        _hasPendingRaw = false;
        _pendingRaw = null;
        _pendingContext = null;
        CancelDebounce();
        await context.SetFromStringAsync(raw);
    }

    /// <summary>
    /// Writes a pending debounced value synchronously, to the field it was typed into, without
    /// revalidating. For the moments nothing can be awaited and the control is about to stop being that
    /// field: disposal, and being handed a different field to render.
    /// </summary>
    /// <remarks>
    /// Only while that field is still somewhere the user could see it. A control also goes away because
    /// a condition hid it or its row was removed, and writing then undid exactly what the form had just
    /// done: a <c>ClearOnHide</c> field got its value back while hidden, and a removed repeater row was
    /// re-created to hold the text.
    /// </remarks>
    private void WritePendingNow()
    {
        if (!_hasPendingRaw) return;

        var raw = _pendingRaw;
        var context = _pendingContext ?? Context;
        _hasPendingRaw = false;
        _pendingRaw = null;
        _pendingContext = null;
        CancelDebounce();
        if (context is null || !context.State.IsLive(context.Field, context.Path)) return;

        var field = context.Field;
        context.State.SetValue(context.Path, BormValueConverter.FromInputString(raw, field.ValueType, field.Type, context.Value));
    }

    protected override void OnParametersSet()
    {
        if (_hasPendingRaw && !ReferenceEquals(_pendingContext, Context))
        {
            // The same field of the same form at a new path is a repeater row that moved — the component
            // follows its row — so the pending text moves with it. Writing it to the old path would put
            // it into whichever row now stands there.
            if (_pendingContext is { } pending && ReferenceEquals(pending.State, Context.State)
                && ReferenceEquals(pending.Field, Context.Field))
                _pendingContext = Context;
            // Handed a different field — the next wizard step reusing this component — with the
            // previous field's last keystrokes still waiting: they belong to that field, so they land
            // there now.
            else
                WritePendingNow();
        }

        // Registered per path so the form can send focus here — "jump to the first error" and the error
        // summary's links both end up going through this. Per state too: a view handed a new model
        // builds a new state but keeps these components, and a registration left on the old, disposed
        // state meant nothing on the new one could ever be focused.
        if (_focusRegistration is null || _registeredPath != Context.Path
            || !ReferenceEquals(_registeredState, Context.State))
        {
            _focusRegistration?.Dispose();
            _registeredPath = Context.Path;
            _registeredState = Context.State;
            _focusRegistration = Context.State.RegisterFocusTarget(_registeredPath, FocusAsync);
        }

        // A value that changed underneath us — a reset, a computed update, another field's cascade —
        // supersedes whatever the user had half-typed, so the counter goes back to tracking the model.
        var committed = Context.StringValue;
        var changed = !string.Equals(_lastCommitted, committed, StringComparison.Ordinal);
        if (changed)
        {
            _lastCommitted = committed;
            _liveLength = null;
        }

        // A bound box keeps the text typed into it while the model holds what that text says - "1." is the 1
        // the model holds, and a debounced word not written yet changed nothing - and while it is still that
        // field's box (a repeater row that moved is). Anything else put a value there that the user did not
        // type: a reset, a formula, the field's own tidying on blur, the next wizard step's field.
        if (_typed is not null)
        {
            if (!ReferenceEquals(_typedInto!.State, Context.State) || !ReferenceEquals(_typedInto.Field, Context.Field)
                || (changed && !string.Equals(ReadBack(_typed), committed, StringComparison.Ordinal)))
                _typed = null;
            else
                _typedInto = Context;
        }

        if (_autofocusApplied || !Field.Autofocus || DisabledOrReadOnly) return;
        _autofocusApplied = true;
        _focusRequested = true;
    }

    private IDisposable? _focusRegistration;
    private string? _registeredPath;
    private BormState? _registeredState;
    private bool _renderHeld;

    /// <summary>
    /// Skips a render while a mouse button is down in the form, and renders once it is released (see
    /// <c>BormState.RendersHeld</c>): what leaving this control draws - a typed signature's preview, a counter -
    /// moved the button being pressed out from under the pointer, and the click was lost.
    /// </summary>
    protected override bool ShouldRender()
    {
        if (!Context.State.RendersHeld) return true;
        if (!_renderHeld)
        {
            _renderHeld = true;
            Context.State.RenderAfterHold(() =>
            {
                _renderHeld = false;
                _ = InvokeAsync(StateHasChanged);
            });
        }
        return false;
    }

    /// <summary>
    /// Moves focus to the control once the render in progress has reached the browser — for a control
    /// whose element is about to be replaced, where focusing it now would focus the one on its way out.
    /// </summary>
    protected void RequestFocusAfterRender() => _focusRequested = true;

    /// <summary>
    /// Asks for the field's loaded options, unless the last attempt failed. A failure is shown in place
    /// of the choices and retried when something invalidates it — a dependency changing, or
    /// <see cref="BormState.InvalidateOptions"/>. Retrying on every render instead turned a
    /// provider that is down into a request per re-render, and a render per request.
    /// </summary>
    protected Task LoadOptionsAsync()
        => Context.OptionsFailed ? Task.CompletedTask : Context.EnsureOptionsAsync().AsTask();

    /// <summary>
    /// Applies <see cref="BormFieldDefinition.Autofocus"/> for real. The HTML attribute of the
    /// same name is only honoured on the browser's initial page load, which an interactively rendered
    /// Blazor form almost never is.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_focusRequested) return;
        _focusRequested = false;
        await FocusAsync();
    }

    /// <summary>
    /// Moves focus to the control. Returns false when there is nothing to focus — a static prerender
    /// with no circuit, or a renderer that never bound <see cref="Element"/> — so a caller walking a
    /// list of candidates can try the next one instead of believing it has succeeded.
    /// </summary>
    public virtual async ValueTask<bool> FocusAsync()
    {
        try
        {
            await Element.FocusAsync();
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or JSDisconnectedException or ObjectDisposedException)
        {
            return false;
        }
    }

    /// <summary>Reads a renderer hint from <see cref="BormFieldDefinition.Attributes"/>.</summary>
    protected T? Attr<T>(string key, T? fallback = default)
    {
        if (!Field.Attributes.TryGetValue(key, out var raw) || raw is null) return fallback;
        if (raw is T typed) return typed;
        return BormValueConverter.TryCoerce(raw, typeof(T), out var converted) && converted is T c ? c : fallback;
    }

    /// <summary>How the model's value reads once <paramref name="text"/> is written to this field: "1." as "1".</summary>
    private string ReadBack(string text) => BormValueConverter.ToInputString(
        BormValueConverter.FromInputString(text, Field.ValueType, Field.Type, Context.Value), Field.Type);

    private void CancelDebounce()
    {
        _debounce?.Cancel();
        _debounce?.Dispose();
        _debounce = null;
    }

    public virtual void Dispose()
    {
        // A debounced value still waiting out its delay is written, not dropped. The control goes away
        // when its wizard step does or a condition hides it, and the user's last characters are in the
        // box they were looking at — losing them because the timer had not fired yet would be the form
        // discarding what was typed into it. Written without revalidating: the control that would show
        // the verdict is the thing being removed, and the next submit judges it anyway.
        WritePendingNow();

        CancelDebounce();
        _focusRegistration?.Dispose();
        _focusRegistration = null;
        GC.SuppressFinalize(this);
    }
}
