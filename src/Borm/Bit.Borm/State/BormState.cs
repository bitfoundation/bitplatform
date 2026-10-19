using System.Collections;
using System.Globalization;
using System.Reflection;

namespace Bit.Borm;

/// <summary>
/// Runtime state for a form instance: holds the data, validation results, touched/dirty tracking,
/// wizard position and submission state. The UI layer binds to this and reacts to <see cref="StateChanged"/>.
/// </summary>
public sealed class BormState : IDisposable
{
    private readonly BormValidator _validator = new();
    private readonly Dictionary<string, List<BormValidationMessage>> _messages = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _touched = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _dirty = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, object?> _initialValues = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<BormSelectOption>> _loadedOptions = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _optionsInFlight = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CancellationTokenSource> _fieldValidationCts = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _validatingFields = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CancellationTokenSource> _optionsCts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _conversionErrors = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Exception> _optionsErrors = new(StringComparer.OrdinalIgnoreCase);

    private readonly bool _hasClearOnHide;
    private readonly bool _hasComputed;
    private readonly bool _hasChangeHandlers;
    private readonly bool _hasRevalidateOn;
    private readonly bool _hasNormalizers;
    private readonly bool _hasOptionDependencies;

    // Handlers write values, and those writes are changes too. Bounded so a pair that answer each other
    // settles rather than recursing, exactly as a cycle of computed formulas does.
    private int _changeHandlerDepth;

    // Nothing that happens while the constructor runs is the user changing the form, so handlers stay
    // silent through defaults, seeding and the first computed pass.
    private readonly bool _initialised;
    private CancellationTokenSource? _formValidationCts;
    private bool _readOnly;
    private bool _disabled;
    private bool _disposed;

    public BormState(BormDefinition definition, IBormDataAccessor data, IServiceProvider? services = null)
    {
        Definition = definition;
        Data = data;
        Services = services;
        // Checked once: these sweeps run after every value change, and most schemas use neither.
        _hasClearOnHide = definition.AllFields().Any(f => f.ClearOnHide);
        _hasComputed = definition.AllFields().Any(f => f.Computed is not null);
        _hasChangeHandlers = definition.AllFields().Any(f => f.OnChanged is not null);
        _hasRevalidateOn = definition.AllFields().Any(f => f.RevalidateOn.Count > 0);
        _hasNormalizers = definition.AllFields().Any(f => f.Normalize is not null);
        _hasOptionDependencies = definition.AllFields()
            .Any(f => f.OptionsProvider is not null && f.OptionsDependencies.Count > 0);

        ApplyDefaults();
        SeedRequiredArrayItems();
        RecomputeValues(changedPath: null);
        CaptureInitialValues();

        _initialised = true;

        // Seeding the form is not the user changing it. Without this a schema with any computed field
        // reports IsFormDirty the instant it is constructed, so an "undo" button bound to it is enabled
        // on a form nobody has touched.
        _dirty.Clear();
    }

    /// <summary>The schema being rendered.</summary>
    public BormDefinition Definition { get; }

    /// <summary>The data store (typed model or dictionary).</summary>
    public IBormDataAccessor Data { get; }

    /// <summary>Optional service provider for validators that need DI.</summary>
    public IServiceProvider? Services { get; }

    /// <summary>Optional external validator merged with built-in rules (set by integrations).</summary>
    public BormExternalValidator? ExternalValidator { get; set; }

    /// <summary>
    /// Makes every field read-only at once — the "review before you submit" or "view an existing
    /// record" mode, without needing a second schema.
    /// </summary>
    public bool ReadOnly
    {
        get => _readOnly;
        set
        {
            if (_readOnly == value) return;
            _readOnly = value;
            NotifyChanged();
        }
    }

    /// <summary>
    /// Disables every control at once — while a save is in flight, or while the record is locked by
    /// someone else. Distinct from <see cref="ReadOnly"/> on purpose: a read-only form can still be
    /// read, tabbed through and copied from, which is what a review screen wants; a disabled one is
    /// skipped by keyboard navigation entirely, which is only right when the form is genuinely
    /// inoperable.
    /// </summary>
    public bool Disabled
    {
        get => _disabled;
        set
        {
            if (_disabled == value) return;
            _disabled = value;
            NotifyChanged();
        }
    }

    /// <summary>When a field revalidates before the form has been submitted. Defaults to <see cref="BormValidationTrigger.OnChange"/>.</summary>
    public BormValidationTrigger ValidationTrigger { get; set; } = BormValidationTrigger.OnChange;

    /// <summary>
    /// When a field revalidates once the form has been submitted at least once, or the field already
    /// shows an error. Defaults to <see cref="BormValidationTrigger.OnChange"/> so a corrected
    /// field clears its error immediately.
    /// </summary>
    public BormValidationTrigger RevalidationTrigger { get; set; } = BormValidationTrigger.OnChange;

    /// <summary>
    /// When true, a field shows only its first error at a time instead of every rule it currently
    /// breaks — React Hook Form's <c>criteriaMode: "firstError"</c>, which is its default.
    /// </summary>
    /// <remarks>
    /// This is about what the user reads, not about what runs: every rule is still evaluated, so
    /// <see cref="IsValid"/> and the submit decision are unchanged. A password that is too short,
    /// missing a digit and missing a symbol otherwise stacks three complaints under one box, which
    /// reads as three problems rather than one field to go and fix. Warnings are unaffected — they
    /// sit alongside the error rather than competing with it. Off by default, because showing
    /// everything at once is what this library has always done and telling the user only half of
    /// what is wrong is a choice, not an improvement.
    /// </remarks>
    public bool SingleErrorPerField { get; set; }

    /// <summary>
    /// What every DOM id this form renders starts with — <c>borm_</c> unless changed. Ids are derived
    /// from field paths, so two forms on one page binding the same schema render the same ids, and a
    /// label, an <c>aria-describedby</c> or an error-summary link in one of them points into the other.
    /// Give each a prefix of its own (for example through <c>BormView.IdPrefix</c>) to keep them
    /// apart. Set it before the form first renders.
    /// </summary>
    public string IdPrefix
    {
        get => _idPrefix;
        set => _idPrefix = string.IsNullOrEmpty(value) ? "borm_" : value;
    }

    private string _idPrefix = "borm_";

    /// <summary>Index of the active wizard step (ignored for non-wizard forms).</summary>
    public int CurrentStepIndex { get; private set; }

    /// <summary>
    /// The furthest step the user has reached. Steps up to it have been validated on the way past, so
    /// they can be returned to freely — including forwards.
    /// </summary>
    /// <remarks>
    /// Without this, going back to step 1 of 4 makes steps 2 and 3 unreachable except by pressing Next
    /// through them again, which is the single most common complaint about hand-rolled wizards: the
    /// user came back to fix one answer and is now made to walk the whole form.
    /// </remarks>
    public int FurthestStepIndex
    {
        // Never behind the current step: a condition that hides the step the user is on can move them
        // forward through ClampStep, and where they are standing is by definition somewhere they have
        // reached.
        get => Math.Max(_furthestStepIndex, CurrentStepIndex);
        private set => _furthestStepIndex = value;
    }

    private int _furthestStepIndex;

    /// <summary>Whether a step may be jumped to directly — it has been reached at least once.</summary>
    public bool IsStepReachable(int index)
        => index >= 0 && index < Definition.Steps.Count
           && index <= FurthestStepIndex
           && IsStepVisible(Definition.Steps[index]);

    /// <summary>Number of times submission has been attempted.</summary>
    public int SubmitCount { get; private set; }

    /// <summary>True while an async submit/validation is in flight.</summary>
    public bool IsValidating { get; private set; }

    /// <summary>True while the consumer's submit handler is running. Used to block double submits.</summary>
    public bool IsSubmitting { get; private set; }

    /// <summary>True once the form has been submitted at least once.</summary>
    public bool IsSubmitted => SubmitCount > 0;

    /// <summary>
    /// True when the last submit passed validation <em>and</em> the handler it dispatched to returned
    /// without throwing — React Hook Form's <c>isSubmitSuccessful</c>.
    /// </summary>
    /// <remarks>
    /// This is what a "Saved." confirmation binds to. <see cref="IsValid"/> cannot do the job: it goes
    /// back to true the moment the user corrects the last error, long before anything has been saved,
    /// and it says nothing at all about whether the save then failed. Reset by the next submit attempt
    /// and by <see cref="Reset()"/>.
    /// </remarks>
    public bool IsSubmitSuccessful { get; private set; }

    /// <summary>
    /// True when no error-severity message is currently recorded.
    /// </summary>
    /// <remarks>
    /// This reports what validation has <em>found</em>, not what it would find: a form that has never
    /// been validated has no messages and so reports valid, even with a required field left blank.
    /// Check <see cref="HasValidated"/> before treating it as a verdict — binding a submit button's
    /// <c>disabled</c> to <c>IsValid</c> alone would leave the button enabled on an empty form and then
    /// disable it the moment the user's first mistake is caught, which is exactly backwards.
    /// Prefer letting the user submit and reporting what is wrong.
    /// </remarks>
    public bool IsValid => !HasErrors;

    /// <summary>
    /// True once a full validation pass has run at least once, so <see cref="IsValid"/> reflects a real
    /// verdict rather than the absence of one. Reset by <see cref="Reset()"/>.
    /// </summary>
    public bool HasValidated { get; private set; }

    /// <summary>Raised whenever state changes and the UI should re-render.</summary>
    public event Action? StateChanged;

    /// <summary>Raised when a specific field value changes, with its path.</summary>
    public event Action<string>? FieldChanged;

    // ---------------------------------------------------------------- values

    public object? GetValue(string path) => Data.GetValue(path);

    public T? GetValue<T>(string path)
    {
        var v = Data.GetValue(path);
        if (v is null) return default;
        if (v is T t) return t;
        return BormValueConverter.TryCoerce(v, typeof(T), out var converted) && converted is T c ? c : default;
    }

    /// <summary>Sets a value, marks the field dirty and touched, and notifies listeners.</summary>
    public void SetValue(string path, object? value) => Write(path, value, markTouched: true);

    /// <summary>
    /// Sets a value without marking the field touched — for programmatic updates (computed values,
    /// prefill) that should not make the user's untouched field start showing errors.
    /// </summary>
    public void SetValueQuietly(string path, object? value) => Write(path, value, markTouched: false);

    /// <summary>
    /// Writes several values as one change: listeners are notified once at the end rather than once per
    /// field. Prefilling a form from a saved record otherwise costs one re-render per value, and any
    /// intermediate state — half the answers applied — is briefly visible to a computed field or a
    /// condition watching from the outside.
    /// </summary>
    /// <param name="values">Path/value pairs to write.</param>
    /// <param name="markTouched">
    /// Whether the fields count as touched. False (the default) is right for prefilling: the user has
    /// not visited these fields, so they should not start out covered in errors.
    /// </param>
    public void SetValues(IEnumerable<KeyValuePair<string, object?>> values, bool markTouched = false)
    {
        ArgumentNullException.ThrowIfNull(values);
        Batch(() =>
        {
            foreach (var (path, value) in values) Write(path, value, markTouched);
        });
    }

    /// <summary>
    /// Runs <paramref name="action"/> with change notifications held back, raising a single one at the
    /// end. Nesting is safe; only the outermost batch notifies. An exception still releases the batch,
    /// so a failure part-way through cannot leave the form permanently silent.
    /// </summary>
    public void Batch(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        _batchDepth++;
        try
        {
            action();
        }
        finally
        {
            _batchDepth--;
            if (_batchDepth == 0 && _batchChanged)
            {
                _batchChanged = false;
                NotifyChanged();
            }
        }
    }

    private int _batchDepth;
    private bool _batchChanged;

    private void Write(string path, object? value, bool markTouched)
    {
        // Text a number field could not read is refused here exactly as a typed model's accessor refuses
        // it. A dictionary store takes anything, so without this "1.5" in an integer box was stored as a
        // string, passed every rule and reached the submit handler as text in a numeric field.
        var rejected = IsUnreadableNumber(path, value);
        if (!rejected) Data.SetValue(path, value);
        RecordConversionResult(path, value, rejected || Data.LastWriteFailed);

        TrackDirty(path);
        if (markTouched) _touched.Add(path);

        // Side effects first, so a listener sees the form in its settled state. The schema's own change
        // handler runs last: it may write a field the sweeps would otherwise clear straight after it (a
        // city chosen for the country just picked, wiped by the cascade that country starts), and it
        // reads totals that are only up to date once the formulas have run.
        ClearHiddenValues(path);
        RecomputeValues(path);
        InvalidateDependentOptions(path);
        RunChangeHandler(path);
        // The answer just given may have hidden the step the user is standing on…
        ClampStep();
        // …or a control still holding text the model refused.
        DropHiddenConversionErrors();
        FieldChanged?.Invoke(path);
        NotifyChanged();
    }

    /// <summary>
    /// Runs the schema's own change handler for <paramref name="path"/>, if it has one.
    /// </summary>
    /// <remarks>
    /// A handler writes values, and those writes come back through <see cref="Write"/> and can reach
    /// handlers of their own. The depth is bounded so two fields that answer each other settle instead
    /// of recursing, on the same principle as a cycle of computed formulas — and a handler is never run
    /// while the form is still being constructed, because applying a default is not a change the user
    /// made.
    /// </remarks>
    private void RunChangeHandler(string path)
    {
        const int maxDepth = 4;
        if (!_hasChangeHandlers || !_initialised || _changeHandlerDepth >= maxDepth) return;
        if (Definition.FindByPath(path)?.OnChanged is not { } handler) return;

        _changeHandlerDepth++;
        try
        {
            handler(new BormChangeContext(this, path, BormPath.Parent(path)));
        }
        finally
        {
            _changeHandlerDepth--;
        }
    }

    /// <summary>
    /// Refreshes every computed field that reads <paramref name="changedPath"/>. Passing null
    /// recomputes them all, which is what the constructor needs.
    /// </summary>
    /// <remarks>
    /// A computed field may itself feed another, so each update cascades — bounded by
    /// <paramref name="depth"/> so a schema whose formulas reference each other in a cycle settles
    /// instead of recursing forever.
    /// </remarks>
    private void RecomputeValues(string? changedPath, int depth = 0)
    {
        const int maxCascade = 8;
        if (!_hasComputed || depth > maxCascade) return;

        // Materialised before the loop: writing a computed value runs the same side-effect sweeps a
        // typed-in one does, and those mutate the data the walk is reading.
        foreach (var (field, path) in EnumerateFieldPaths().ToList())
        {
            if (field.Computed is null) continue;

            var scope = BormPath.Parent(path);
            if (changedPath is not null && !ReadsPath(field.ComputedDependencies, changedPath, scope)) continue;
            // A formula that reads its own output would otherwise re-trigger itself forever.
            if (string.Equals(path, changedPath, StringComparison.OrdinalIgnoreCase)) continue;

            var next = field.Computed(new BormComputedContext(Data, path, scope));
            if (SameValue(Data.GetValue(path), next)) continue;

            Data.SetValue(path, next);
            TrackDirty(path);

            // A derived value is a change to the form like any other. Without these a field whose
            // VisibleWhen reads a total was never cleared when the total hid it, a cascading select
            // that depended on one never reloaded, and OnFieldChanged never heard about it at all —
            // the three things every hand-written change handler does, skipped for exactly the values
            // the form computed for itself. The handler runs after the cascade, as it does in Write.
            ClearHiddenValues(path);
            InvalidateDependentOptions(path);
            RecomputeValues(path, depth + 1);
            RunChangeHandler(path);
            FieldChanged?.Invoke(path);
        }
    }

    /// <summary>
    /// Whether two values are the same answer. Numbers compare by value whatever their CLR type: a
    /// formula returning <c>5</c> for a <c>decimal</c> property is read back as <c>5m</c>, and
    /// <see cref="object.Equals(object?, object?)"/> calling those different made every recompute a
    /// change — re-firing handlers and emptying every select that cascades off the total.
    /// </summary>
    private static bool SameValue(object? a, object? b)
    {
        if (Equals(a, b)) return true;
        if (!BormNumber.IsNumber(a) || !BormNumber.IsNumber(b)) return false;
        var ca = BormNumber.Canonical(a);
        return ca is not null && ca == BormNumber.Canonical(b);
    }

    /// <summary>
    /// Whether <paramref name="value"/> is text a numeric field cannot read. Only a field with no CLR
    /// type of its own or a numeric one is judged: a typed model's accessor refuses such text already,
    /// and a field whose property is a string holds whatever it is given.
    /// </summary>
    private bool IsUnreadableNumber(string path, object? value)
    {
        if (value is not string { Length: > 0 } text) return false;
        if (Definition.FindByPath(path) is not { Type: BormFieldType.Integer or BormFieldType.Number or BormFieldType.Range } field)
            return false;

        if (field.ValueType is { } declared)
        {
            var type = Nullable.GetUnderlyingType(declared) ?? declared;
            if (type.IsEnum || Type.GetTypeCode(type) is < TypeCode.SByte or > TypeCode.Decimal) return false;
        }

        // Read the way the converter reads a number box with no CLR type behind it.
        return field.Type == BormFieldType.Integer
            ? !long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
            : !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
    }

    /// <summary>
    /// Whether a declared dependency list covers <paramref name="path"/>. Dependencies are matched
    /// both absolutely and relative to <paramref name="scope"/>, so a formula on an array item can
    /// name its siblings (<c>"Quantity"</c>) without knowing which row it will end up on. An empty
    /// list means "depends on everything", matching how conditions declare their dependencies.
    /// </summary>
    private static bool ReadsPath(IList<string> dependencies, string path, string scope)
        => dependencies.Count == 0 || DeclaresPath(dependencies, path, scope);

    /// <summary>
    /// Whether a declared dependency list names <paramref name="path"/> — matched both absolutely and
    /// relative to <paramref name="scope"/>, and by prefix in both directions: naming a container covers
    /// everything inside it, and replacing a container changes every dependency inside it. Unlike
    /// <see cref="ReadsPath"/> an empty list matches nothing, which is what "these options never need
    /// reloading" means.
    /// </summary>
    private static bool DeclaresPath(IList<string> dependencies, string path, string scope)
        => CoversPath(dependencies, path, scope) || LiesWithin(dependencies, path, scope);

    /// <summary>
    /// Whether <paramref name="path"/> is at or beneath one of <paramref name="dependencies"/> — the
    /// value named, or something inside it, was written.
    /// </summary>
    private static bool CoversPath(IEnumerable<string> dependencies, string path, string scope)
    {
        foreach (var dependency in dependencies)
        {
            if (dependency.Length == 0) continue;
            if (BormPath.IsAtOrUnder(path, dependency)) return true;
            if (scope.Length > 0 && BormPath.IsAtOrUnder(path, BormPath.Combine(scope, dependency)))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Whether one of <paramref name="dependencies"/> lies beneath <paramref name="path"/> — the object
    /// or list holding the value named was replaced, cleared or reset as a whole. Without this,
    /// resetting <c>Address</c> left the city's choices loaded for the country it had just put back,
    /// and hiding a <c>Shipping</c> group left a cost computed from its country at its old figure.
    /// </summary>
    private static bool LiesWithin(IEnumerable<string> dependencies, string path, string scope)
    {
        foreach (var dependency in dependencies)
        {
            if (dependency.Length == 0) continue;
            if (BormPath.IsAtOrUnder(dependency, path)) return true;
            if (scope.Length > 0 && BormPath.IsAtOrUnder(BormPath.Combine(scope, dependency), path))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Turns a rejected write into a validation message. Without this the model silently keeps its
    /// previous value and the user's entry disappears on the next render with no explanation.
    /// </summary>
    private void RecordConversionResult(string path, object? attempted, bool failed)
    {
        if (failed)
        {
            _conversionErrors[path] = BormValueConverter.ToInvariantString(attempted);
            // Whatever else was reported about this field described a value the model never took, and
            // typing a second bad entry must not stack a second copy of the same complaint.
            RemoveMessagesFor(path);
            AddMessage(ConversionMessage(path));
        }
        else if (_conversionErrors.Remove(path))
        {
            RemoveMessagesFor(path);
        }
    }

    private BormValidationMessage ConversionMessage(string path)
        => new(path, Text(BormMessageKeys.Conversion, _conversionErrors[path]));

    /// <summary>
    /// Resolves a piece of the library's own text (see <see cref="BormMessageKeys"/>) through the
    /// registered <see cref="IBormMessageProvider"/>, falling back to the English defaults. The
    /// built-in components render everything through this, so a single provider translates the whole
    /// form — buttons, placeholders and repeater labels included, not only the validation messages.
    /// </summary>
    public string Text(string key, params object?[] args) => Messages.Get(key, args);

    // Resolved once. A repeater asks for four labels per row per render, and a container lookup for
    // each of them is pure waste — the registration cannot change for the life of a form anyway.
    private IBormMessageProvider? _messageProvider;

    private IBormMessageProvider Messages
        => _messageProvider ??= Services?.GetService(typeof(IBormMessageProvider)) as IBormMessageProvider
                                ?? BormDefaultMessageProvider.Instance;

    /// <summary>
    /// Re-attaches conversion messages after a validation pass has rebuilt the message set, so a value
    /// the model could never accept keeps reporting itself. Pass <paramref name="under"/> when only the
    /// messages at and beneath that path were rebuilt.
    /// </summary>
    private void ReapplyConversionErrors(string? under = null)
    {
        if (_conversionErrors.Count == 0) return;
        if (under is not null && !_conversionErrors.Keys.Any(k => BormPath.IsAtOrUnder(k, under))) return;

        DropHiddenConversionErrors();
        foreach (var path in _conversionErrors.Keys)
        {
            if (under is not null && !BormPath.IsAtOrUnder(path, under)) continue;
            AddMessage(ConversionMessage(path));
        }
    }

    /// <summary>
    /// Forgets the rejected entries of controls that are no longer on the page — hidden by a condition,
    /// or on a wizard step a condition has taken away.
    /// </summary>
    /// <remarks>
    /// The text the model refused lived only in the control, and the control went when the field was
    /// hidden: shown again, it renders what the model holds. Kept, the complaint blocked every submit
    /// from a field the user could neither see nor correct — exactly what the validator refuses to do
    /// with a hidden field's own rules — and then described text that was no longer in the box.
    /// </remarks>
    private void DropHiddenConversionErrors()
    {
        if (_conversionErrors.Count == 0) return;

        var hidden = HiddenFieldPaths();
        hidden.AddRange(HiddenStepFields());
        if (hidden.Count == 0) return;

        foreach (var path in _conversionErrors.Keys.Where(p => IsHidden(p, hidden)).ToList())
        {
            _conversionErrors.Remove(path);
            RemoveMessagesFor(path);
        }
    }

    /// <summary>
    /// Records whether <paramref name="path"/> still holds the value it started with. Dirtiness is a
    /// comparison, not a flag: a field the user typed into and then put back is not a change, and an
    /// "undo" or "you have unsaved work" prompt bound to <see cref="IsFormDirty"/> must not fire for it.
    /// This is how <c>dirtyFields</c> behaves in React Hook Form and TanStack Form.
    /// </summary>
    private void TrackDirty(string path)
    {
        if (_initialValues.TryGetValue(path, out var initial) && MatchesInitial(initial, Data.GetValue(path)))
            _dirty.Remove(path);
        else
            _dirty.Add(path);
    }

    /// <summary>
    /// Whether a value equals the one captured as the baseline. A collection's baseline is a snapshot
    /// of its elements, so a repeater row added and then removed again — and a multi-select box
    /// unticked and ticked again — leave the form clean.
    /// </summary>
    private static bool MatchesInitial(object? initial, object? current)
    {
        if (initial is List<object?> snapshot)
        {
            // Nothing there and nothing expected is a match. A control that writes a fresh empty list
            // where the model held null has not changed the user's answer.
            if (current is null) return snapshot.Count == 0;
            if (current is string || current is not IEnumerable live) return false;

            var i = 0;
            foreach (var item in live)
            {
                if (i >= snapshot.Count || !Equals(snapshot[i], item)) return false;
                i++;
            }
            return i == snapshot.Count;
        }

        if (ReferenceEquals(initial, current)) return true;
        return initial is not null && current is not null && initial.Equals(current);
    }

    public bool IsTouched(string path) => _touched.Contains(path);
    public bool IsDirty(string path) => _dirty.Contains(path);
    public bool IsFormDirty => _dirty.Count > 0;

    /// <summary>Every field path the user has interacted with.</summary>
    public IReadOnlyCollection<string> TouchedFields => _touched;

    /// <summary>Every field path whose value has changed since the form was created or last reset.</summary>
    public IReadOnlyCollection<string> DirtyFields => _dirty;

    public void MarkTouched(string path)
    {
        if (_touched.Add(path)) NotifyChanged();
    }

    /// <summary>
    /// Marks every field touched, so validation messages become visible everywhere. Called on submit
    /// so a user who submits an untouched form still sees what is missing.
    /// </summary>
    public void MarkAllTouched()
    {
        foreach (var path in EnumerateValuePaths())
            _touched.Add(path);
        foreach (var key in _messages.Keys)
            _touched.Add(key);
        NotifyChanged();
    }

    // ---------------------------------------------------------------- conditional state

    /// <summary>
    /// Whether a field is currently visible given the data. Pass <paramref name="path"/> so a condition
    /// written on an array item's template is evaluated against that row; omitting it evaluates against
    /// the root, which is all a top-level field ever needs.
    /// </summary>
    public bool IsVisible(BormFieldDefinition field, string? path = null)
        => field.VisibleWhen is null || field.VisibleWhen.Evaluate(ScopeFor(path));

    /// <summary>
    /// Whether a field is currently disabled given the data. Read-only fields are deliberately *not*
    /// disabled — a disabled control is skipped by keyboard navigation and unreadable to screen
    /// readers, so read-only is rendered with the <c>readonly</c> attribute instead. See <see cref="IsReadOnly"/>.
    /// </summary>
    public bool IsDisabled(BormFieldDefinition field, string? path = null)
        => Disabled || (field.DisabledWhen is not null && field.DisabledWhen.Evaluate(ScopeFor(path)));

    /// <summary>
    /// Whether a field is read-only: in its own right, because a condition says so, or because the
    /// whole form is. Pass <paramref name="path"/> so a condition written on an array item's template
    /// is evaluated against that row, exactly as <see cref="IsVisible"/> and <see cref="IsDisabled"/> do.
    /// </summary>
    public bool IsReadOnly(BormFieldDefinition field, string? path = null)
        => ReadOnly || field.ReadOnly
           || (field.ReadOnlyWhen is not null && field.ReadOnlyWhen.Evaluate(ScopeFor(path)));

    /// <summary>Whether a field is required right now, taking <see cref="BormFieldDefinition.RequiredWhen"/> into account.</summary>
    public bool IsRequired(BormFieldDefinition field, string? path = null)
        => field.Required || (field.RequiredWhen is not null && field.RequiredWhen.Evaluate(ScopeFor(path)));

    /// <summary>
    /// Whether <paramref name="path"/> still addresses a field the user could be looking at: every row
    /// it passes through still exists, and neither the field nor any object or repeater it sits in is
    /// hidden. A control holding text it has not written yet asks this before writing it on its way out.
    /// </summary>
    internal bool IsLive(BormFieldDefinition field, string path)
    {
        if (!BormPath.IsValid(path)) return false;

        var prefix = string.Empty;
        foreach (var segment in BormPath.Parse(path))
        {
            if (segment.IsIndex)
            {
                // The row this path runs through has been removed (or the list shortened under it).
                if (prefix.Length == 0 || segment.Index >= ArrayCount(prefix)) return false;
                prefix = BormPath.Combine(prefix, segment.Index);
                continue;
            }

            prefix = BormPath.Combine(prefix, segment.Name!);
            if (prefix.Length < path.Length && Definition.FindByPath(prefix) is { IsContainer: true } container
                && !IsVisible(container, prefix))
                return false;
        }

        return IsVisible(field, path);
    }

    /// <summary>Data scoped to the container that owns <paramref name="path"/>, or the root when it has none.</summary>
    private IBormDataReader ScopeFor(string? path)
        => path is null ? Data : BormScopedDataReader.ForOwnerOf(Data, path);

    /// <summary>Whether a wizard step is currently visible.</summary>
    public bool IsStepVisible(BormStep step)
        => step.VisibleWhen is null || step.VisibleWhen.Evaluate(Data);

    /// <summary>
    /// Clears the value of every field marked <see cref="BormFieldDefinition.ClearOnHide"/> that
    /// has just become invisible, so a hidden branch never contributes stale data to the model.
    /// Only fields whose condition actually reads <paramref name="changedPath"/> are re-examined.
    /// </summary>
    private void ClearHiddenValues(string changedPath)
    {
        if (!_hasClearOnHide) return;

        // Materialised: clearing a hidden container changes the live array counts the walk reads.
        List<string>? cleared = null;
        foreach (var (field, path) in EnumerateFieldPaths().ToList())
        {
            if (!field.ClearOnHide || field.VisibleWhen is null) continue;
            if (!DependsOn(field.VisibleWhen, changedPath, BormPath.Parent(path))) continue;
            if (field.VisibleWhen.Evaluate(ScopeFor(path))) continue;
            if (Data.GetValue(path) is null) continue;

            Data.SetValue(path, null);
            RemoveMessagesUnder(path);
            _touched.Remove(path);
            // Emptying a field is a change to the data like any other, so the form is now dirty even
            // though the user never typed in this box.
            TrackDirty(path);
            (cleared ??= []).Add(path);
        }

        // …and being a change like any other means the rest of the engine has to hear about it. The
        // sweeps run after the loop, not inside it, because they re-enter this method: a cleared value
        // can hide a second field, and that one is cleared by the recursive call rather than by a walk
        // whose list is already stale. Emptied for the user by a condition or emptied by the user
        // themselves, a total that reads this field must be recomputed, a select that cascades off it
        // must reload, and an autosave watching FieldChanged has to see it go.
        if (cleared is null) return;
        foreach (var path in cleared)
        {
            // Re-entrant, and it terminates: a field is only cleared while it still holds something,
            // so a chain of conditions settles at the first one that has nothing left to empty.
            ClearHiddenValues(path);
            RecomputeValues(path);
            InvalidateDependentOptions(path);
            FieldChanged?.Invoke(path);
        }
    }

    /// <summary>
    /// Whether a condition reads <paramref name="path"/>. Dependencies are matched both absolutely and
    /// relative to <paramref name="scope"/>, mirroring how the condition itself is evaluated, and in
    /// both directions, as <see cref="DeclaresPath"/> explains. A condition that declares no
    /// dependencies — a raw predicate, say — is assumed to depend on everything, matching
    /// <see cref="IBormCondition.Dependencies"/>.
    /// </summary>
    private static bool DependsOn(IBormCondition condition, string path, string scope = "")
    {
        var dependencies = condition.Dependencies as IList<string> ?? condition.Dependencies.ToList();
        if (dependencies.Count == 0) return true;
        return DeclaresPath(dependencies, path, scope);
    }

    // ---------------------------------------------------------------- options

    /// <summary>
    /// The options to render for a field: those loaded by its
    /// <see cref="BormFieldDefinition.OptionsProvider"/> if it has one, otherwise the static
    /// <see cref="BormFieldDefinition.Options"/> from the schema.
    /// </summary>
    public IReadOnlyList<BormSelectOption> OptionsFor(BormFieldDefinition field, string path)
    {
        if (field.OptionsProvider is not null)
            return _loadedOptions.TryGetValue(path, out var loaded) ? loaded : Array.Empty<BormSelectOption>();

        // The schema's own list is handed back as-is when it already is a read-only list (the default),
        // because this sits on the render path and copying it per render would be pure waste.
        return field.Options as IReadOnlyList<BormSelectOption> ?? field.Options.ToList();
    }

    /// <summary>True while a field's <see cref="BormFieldDefinition.OptionsProvider"/> is running.</summary>
    public bool IsLoadingOptions(string path) => _optionsInFlight.Contains(path);

    /// <summary>
    /// Moves on whenever any field's loaded options change — a load starting, landing or failing, a
    /// cache dropped because a dependency changed. A control that skips re-rendering when nothing it
    /// shows has changed must include this in what it compares: an empty list replaced by "please load
    /// again" looks exactly like the empty list it was, and a select that decided it had nothing to
    /// redraw never asked for its new choices.
    /// </summary>
    internal int OptionsVersion { get; private set; }

    /// <summary>
    /// The exception a field's options provider last failed with, or null when it has not failed. A
    /// lookup that goes over the network fails for ordinary reasons, and losing the whole form to it
    /// would be out of proportion — so the failure is recorded here, announced through
    /// <see cref="OptionsLoadFailed"/>, and shown in place of the choices.
    /// </summary>
    public Exception? OptionsError(string path)
        => _optionsErrors.TryGetValue(path, out var error) ? error : null;

    /// <summary>
    /// Raised with the field path and the exception when an options provider throws. Subscribe to log
    /// it; the form itself carries on with an empty list and retries on the next
    /// <see cref="InvalidateOptions"/>.
    /// </summary>
    public event Action<string, Exception>? OptionsLoadFailed;

    /// <summary>
    /// Runs a field's options provider unless its results are already cached. Renderers call this on
    /// first render and after a dependency changes; it is a no-op for fields with static options.
    /// </summary>
    public async ValueTask EnsureOptionsAsync(BormFieldDefinition field, string path)
    {
        if (field.OptionsProvider is null || _loadedOptions.ContainsKey(path) || !_optionsInFlight.Add(path))
            return;

        var cts = new CancellationTokenSource();
        _optionsCts[path] = cts;
        _optionsErrors.Remove(path);
        OptionsVersion++;

        NotifyChanged();
        try
        {
            // Scoped like every other context the form hands out, so a provider on a repeater's
            // template reading ctx.Value("Country") gets this row's country — the same thing its
            // dependsOn: "Country" already means. An absolute path still resolves from the root.
            var ctx = new BormOptionsContext(field, path, ScopeFor(path), Services, cts.Token);
            var options = await field.OptionsProvider(ctx);
            if (!cts.IsCancellationRequested) _loadedOptions[path] = options;
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // A newer load (or disposal) superseded this one; the cache stays empty so it retries.
            // Only when it really was this load that was cancelled: a provider throwing a cancellation
            // of its own — a timed-out HTTP call, a memoised task that was once cancelled — has failed,
            // and is recorded below like any other failure. Swallowed here, it left nothing to say the
            // field had failed, so the control asked again on the very next render, and again, forever.
        }
        catch (Exception ex)
        {
            // A lookup that goes over the network fails for ordinary reasons — a timeout, a 503. Letting
            // it escape would take down the component that asked for it (renderers call this from
            // OnParametersSetAsync), so it is recorded and shown instead. The cache stays empty, so
            // invalidating the field retries.
            if (!cts.IsCancellationRequested)
            {
                _optionsErrors[path] = ex;
                OptionsLoadFailed?.Invoke(path, ex);
            }
        }
        finally
        {
            // Only the run that still owns the path may take its in-flight flag down. One that was
            // cancelled had its flag removed by the cancellation, and the flag now standing belongs to
            // the load that replaced it — clearing it here let a third load start alongside the second.
            if (_optionsCts.TryGetValue(path, out var current) && ReferenceEquals(current, cts))
            {
                _optionsCts.Remove(path);
                _optionsInFlight.Remove(path);
                cts.Dispose();
            }
            OptionsVersion++;
            NotifyChanged();
        }
    }

    /// <summary>Drops cached options for a field so the next render reloads them.</summary>
    public void InvalidateOptions(string path)
    {
        CancelOptionsLoad(path);
        var cleared = _optionsErrors.Remove(path);
        if (_loadedOptions.Remove(path) || cleared)
        {
            OptionsVersion++;
            NotifyChanged();
        }
    }

    /// <summary>Aborts an in-flight options load, so a superseded lookup stops doing work.</summary>
    private void CancelOptionsLoad(string path)
    {
        if (!_optionsCts.Remove(path, out var cts)) return;
        cts.Cancel();
        cts.Dispose();
        _optionsInFlight.Remove(path);
        OptionsVersion++;
    }

    /// <param name="changedPath">The path whose value changed.</param>
    /// <param name="rowsMoved">
    /// True when <paramref name="changedPath"/> is a list whose rows were added, removed or reordered
    /// rather than replaced. Each row's own state has already followed it (see
    /// <see cref="RemapArrayIndices"/>), so a select inside a row that cascades off a sibling has
    /// nothing to reload; only something depending on the list itself does.
    /// </param>
    private void InvalidateDependentOptions(string changedPath, bool rowsMoved = false)
    {
        if (!_hasOptionDependencies) return;

        // Two passes: the walk only collects, so it never sees the data change underneath it, and a
        // form where nothing depends on this path — which is almost every write — allocates nothing.
        List<(string Path, bool KeepValue)>? targets = null;
        foreach (var (field, path) in EnumerateFieldPaths())
        {
            if (field.OptionsProvider is null) continue;
            // Matched the same way conditions and computed dependencies are: relative to the object that
            // owns the field, so a cascading select inside a repeater row can name its sibling
            // ("Country") and still reload, and by prefix, so naming a container covers its members.
            var scope = BormPath.Parent(path);
            if (CoversPath(field.OptionsDependencies, changedPath, scope))
            {
                (targets ??= []).Add((path, false));
            }
            else if (!rowsMoved && LiesWithin(field.OptionsDependencies, changedPath, scope))
            {
                // The container holding the dependency was replaced, so the cached choices describe a
                // value that may be gone. The answer goes too — unless it sits in that same container,
                // in which case it arrived (or was put back) together with its dependency and belongs
                // to it: a prefilled address keeps the city it was saved with.
                (targets ??= []).Add((path, BormPath.IsAtOrUnder(path, changedPath)));
            }
        }
        if (targets is null) return;

        List<string>? cleared = null;
        foreach (var (path, keepValue) in targets)
        {
            CancelOptionsLoad(path);
            _loadedOptions.Remove(path);
            _optionsErrors.Remove(path);
            OptionsVersion++;
            // The previously selected option may no longer exist in the new list. This is judged on the
            // dependency, not on whether the choices happen to be cached: a value written before the
            // control ever rendered — a prefill, a restored draft — was still chosen for the old
            // dependency, and leaving it because no list had been fetched yet is how a city survives the
            // country it belongs to.
            if (keepValue || Data.GetValue(path) is null) continue;

            Data.SetValue(path, null);
            TrackDirty(path);
            // Whatever was being said about this field described the answer that has just been taken
            // away, so it goes with it.
            RemoveMessagesUnder(path);
            (cleared ??= []).Add(path);
        }

        // …and the rest of the engine has to hear about it, exactly as it does when a condition empties
        // a field. Without this a third level of a cascade never reloaded (country → region → city left
        // the city holding a value from the country the user had just abandoned), a total that read the
        // cleared field kept its old figure, a branch it was keeping open stayed open, and an autosave
        // watching FieldChanged never saw the value go. Run after the loop because each of these
        // re-enters this method; it terminates because a field is only cleared while it still holds
        // something, and its cached options are dropped on the way past.
        if (cleared is null) return;
        foreach (var path in cleared)
        {
            InvalidateDependentOptions(path);
            ClearHiddenValues(path);
            RecomputeValues(path);
            FieldChanged?.Invoke(path);
        }
    }

    // ---------------------------------------------------------------- validation

    /// <summary>Records a submit attempt (so validation messages become visible) and returns the count.</summary>
    public int RegisterSubmitAttempt()
    {
        SubmitCount++;
        return SubmitCount;
    }

    /// <summary>All current validation messages across the form.</summary>
    public IEnumerable<BormValidationMessage> AllMessages => _messages.Values.SelectMany(x => x);

    /// <summary>
    /// All current messages in the order their fields appear in the schema — the order an error
    /// summary should list them so "fix the first one" walks the user down the page.
    /// </summary>
    public IReadOnlyList<BormValidationMessage> OrderedMessages()
    {
        // Ordering walks the whole schema, including every live array row. A valid form is the usual
        // case and has nothing to order, so it never pays for the walk.
        if (_messages.Count == 0) return Array.Empty<BormValidationMessage>();

        var order = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var i = 0;
        foreach (var (_, path) in EnumerateFieldPaths())
            order.TryAdd(path, i++);

        // A form-level message belongs to no control, so it cannot be "the first one to go and fix" in
        // the positional sense — it leads instead, which is where a reader looking for what is wrong
        // with the form as a whole expects to find it.
        return AllMessages
            .OrderBy(m => m.FieldPath.Length == 0 ? -1 : order.TryGetValue(m.FieldPath, out var idx) ? idx : int.MaxValue)
            .ThenBy(m => m.FieldPath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Messages for a specific field path.</summary>
    public IReadOnlyList<BormValidationMessage> MessagesFor(string path)
        => _messages.TryGetValue(path, out var list) ? list : Array.Empty<BormValidationMessage>();

    /// <summary>True if there is at least one error-severity message.</summary>
    /// <remarks>
    /// Written as a loop rather than a LINQ query on purpose: every render of every button reads it
    /// through <c>IsValid</c>, and a nested <c>SelectMany</c> allocates two iterators and a closure each
    /// time to answer a question that is almost always "no, and here is the first one".
    /// </remarks>
    public bool HasErrors
    {
        get
        {
            foreach (var list in _messages.Values)
                for (var i = 0; i < list.Count; i++)
                    if (list[i].Severity == BormValidationSeverity.Error) return true;
            return false;
        }
    }

    /// <summary>
    /// Everything known about one field in a single read: whether the user has been there, whether the
    /// value has moved from its baseline, and what is currently wrong with it.
    /// </summary>
    /// <remarks>
    /// The pieces are all separately available (<see cref="IsTouched"/>, <see cref="IsDirty"/>,
    /// <see cref="MessagesFor"/>); this is the aggregate React Hook Form spells <c>getFieldState</c>,
    /// and it exists because a custom renderer asking all three questions per render otherwise reads
    /// the state three times and has to keep the answers in step itself.
    /// </remarks>
    public BormFieldState GetFieldState(string path)
    {
        var messages = MessagesFor(path);
        var invalid = false;
        for (var i = 0; i < messages.Count; i++)
        {
            if (messages[i].Severity != BormValidationSeverity.Error) continue;
            invalid = true;
            break;
        }
        return new BormFieldState(IsTouched(path), IsDirty(path), invalid, messages, IsValidatingField(path));
    }

    /// <summary>Validates the entire form and stores the results.</summary>
    public async ValueTask<bool> ValidateAsync(bool includeAsync = true)
    {
        // A newer full validation always wins over one still in flight, so a slow async rule can never
        // overwrite results computed from more recent data.
        _formValidationCts?.Cancel();
        _formValidationCts?.Dispose();
        var cts = _formValidationCts = new CancellationTokenSource();
        // Taken now, while the source is alive. A newer run disposes this one's source, and reading
        // cts.Token after that throws ObjectDisposedException instead of reporting the cancellation.
        var token = cts.Token;

        IsValidating = true;
        NotifyChanged();
        try
        {
            var messages = await _validator.ValidateAsync(Definition, Data, Services, null, includeAsync, token);
            var merged = await MergeExternal(messages);
            token.ThrowIfCancellationRequested();
            ReplaceAllMessages(DropHiddenStepMessages(merged));
            ReapplyConversionErrors();
            HasValidated = true;
            return !HasErrors;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Superseded (or disposed) before a verdict was reached. Whatever messages are on record
            // describe an older run — or none at all on a form never validated — so reporting them as
            // a pass would let a submit go ahead on data nobody has checked. The run that replaced this
            // one publishes the real verdict. Only when it really was this run that was cancelled: a
            // rule throwing a cancellation of its own — a timed-out HTTP call — has failed, and fails
            // like any other rule that throws. Swallowed here, the submit reported "invalid" with no
            // message anywhere and the button looked as though it did nothing.
            return false;
        }
        finally
        {
            if (ReferenceEquals(_formValidationCts, cts))
            {
                IsValidating = false;
                _formValidationCts = null;
                cts.Dispose();
            }
            NotifyChanged();
        }
    }

    /// <summary>Validates the fields of the current wizard step.</summary>
    public async ValueTask<bool> ValidateStepAsync(bool includeAsync = true)
    {
        if (!Definition.IsWizard) return await ValidateAsync(includeAsync);

        // Shares the form-level token with ValidateAsync, so a second "Next" (or a submit) supersedes a
        // run still waiting on an async rule instead of letting the older verdict land last.
        _formValidationCts?.Cancel();
        _formValidationCts?.Dispose();
        var cts = _formValidationCts = new CancellationTokenSource();
        // Taken now: see ValidateAsync — the source may be disposed by a newer run before this one ends.
        var token = cts.Token;

        IsValidating = true;
        NotifyChanged();
        try
        {
            // Through CurrentStep, so a step a condition has just hidden is never the one being validated.
            var step = CurrentStep!;

            // Each step field is resolved as a path rather than a top-level name, so a step can own a
            // nested field ("Address.City") just as easily as a root one.
            var messages = new List<BormValidationMessage>();
            foreach (var path in step.Fields)
            {
                if (Definition.FindByPath(path) is not { } field) continue;
                messages.AddRange(await _validator.ValidateFieldAsync(field, path, Data, Services, includeAsync, token));
            }

            // External validators (FluentValidation and friends) see the whole model, so their results are
            // filtered down to the fields this step owns — otherwise a later step's errors would block it.
            if (ExternalValidator is not null)
            {
                var external = await ExternalValidator(Definition, Data, Services);
                if (external.Count > 0)
                {
                    var hidden = HiddenFieldPaths();
                    messages.AddRange(external.Where(m =>
                        step.Fields.Any(f => BormPath.IsAtOrUnder(m.FieldPath, f)) && !IsHidden(m.FieldPath, hidden)));
                }
            }

            token.ThrowIfCancellationRequested();

            // Replace only messages for fields in this step, and mark them touched so the errors show.
            foreach (var path in step.Fields)
            {
                RemoveMessagesUnder(path);
                _touched.Add(path);
            }
            foreach (var m in messages)
            {
                AddMessage(m);
                // A step field may be an object or array, so touch the exact path each message landed on.
                _touched.Add(m.FieldPath);
            }

            // A value the model could never accept must block the step too, even though no rule produced
            // it — unless its control has since been hidden, taking the rejected text with it.
            DropHiddenConversionErrors();
            var blocked = false;
            foreach (var path in _conversionErrors.Keys.Where(p => step.Fields.Any(f => BormPath.IsAtOrUnder(p, f))))
            {
                AddMessage(ConversionMessage(path));
                _touched.Add(path);
                blocked = true;
            }

            return !blocked && !messages.Any(m => m.Severity == BormValidationSeverity.Error);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Superseded: the run that replaced this one decides whether the step may be left. A rule's
            // own cancellation is a failure, not a supersession — see ValidateAsync.
            return false;
        }
        finally
        {
            if (ReferenceEquals(_formValidationCts, cts))
            {
                IsValidating = false;
                _formValidationCts = null;
                cts.Dispose();
            }
            NotifyChanged();
        }
    }

    /// <summary>
    /// Validates the single field at <paramref name="path"/>, resolving its definition from the schema
    /// — React Hook Form's <c>trigger(name)</c>. Returns false when the path names no field in the
    /// schema, so a caller checking one answer from outside the form does not have to go and find the
    /// definition first.
    /// </summary>
    public async ValueTask<bool> ValidateFieldAsync(string path, bool includeAsync = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (Definition.FindByPath(path) is not { } field) return false;

        await ValidateFieldAsync(field, path, includeAsync);
        return true;
    }

    /// <summary>Validates a single field and refreshes only its messages.</summary>
    public async ValueTask ValidateFieldAsync(BormFieldDefinition field, string path, bool includeAsync = true)
    {
        // Supersede any in-flight validation of the same field so results always reflect the latest value.
        if (_fieldValidationCts.TryGetValue(path, out var previous))
        {
            previous.Cancel();
            previous.Dispose();
        }
        var cts = new CancellationTokenSource();
        _fieldValidationCts[path] = cts;

        // Only when there is genuinely something to wait for. Every rule this field has is synchronous
        // on most fields, so flagging those would flicker a spinner on and off once per keystroke to
        // report work that never left the thread.
        var awaited = includeAsync && HasAsyncRules(field);
        if (awaited && _validatingFields.Add(path)) NotifyChanged();

        try
        {
            var messages = await _validator.ValidateFieldAsync(field, path, Data, Services, includeAsync, cts.Token);
            if (cts.IsCancellationRequested) return;

            RemoveMessagesUnder(path);
            foreach (var m in messages) AddMessage(m);
            // Everything under the path was just cleared, so every rejected entry under it — a child of
            // an object or a row being revalidated as a whole — has to report itself again, not only
            // one standing at the path itself.
            ReapplyConversionErrors(path);
            NotifyChanged();
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // Superseded. A rule's own cancellation is a failure, not a supersession — see ValidateAsync.
        }
        finally
        {
            if (_fieldValidationCts.TryGetValue(path, out var current) && ReferenceEquals(current, cts))
            {
                _fieldValidationCts.Remove(path);
                cts.Dispose();
                // Cleared only by the run that owns the field: a superseded run finishing late must not
                // announce that the newer one, still in flight, has finished too. Cleared whether or not
                // this run was itself asynchronous: a keystroke's synchronous check supersedes the blur's
                // remote one, and the flag that one raised is now this run's to take down — otherwise
                // the "checking…" spinner stays up for good.
                if (_validatingFields.Remove(path)) NotifyChanged();
            }
        }
    }

    /// <summary>Whether a field has any rule that actually goes away and comes back.</summary>
    private static bool HasAsyncRules(BormFieldDefinition field)
    {
        for (var i = 0; i < field.Validators.Count; i++)
            if (field.Validators[i].IsAsync) return true;
        return false;
    }

    /// <summary>
    /// Whether an asynchronous rule is currently running for <paramref name="path"/> — what a control
    /// showing "checking that username…" binds to. React Hook Form spells the set of these
    /// <c>validatingFields</c>; TanStack Form spells it <c>field.state.meta.isValidating</c>.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="IsValidating"/>, which is about the form: a remote uniqueness check on
    /// one box should put a spinner beside that box, not declare the whole form busy and grey out its
    /// submit button. Only fields that actually have an async rule are ever reported, so a form of
    /// ordinary synchronous rules never flickers.
    /// </remarks>
    public bool IsValidatingField(string path) => _validatingFields.Contains(path);

    /// <summary>Every field path with an asynchronous rule currently in flight.</summary>
    public IReadOnlyCollection<string> ValidatingFields => _validatingFields;

    /// <summary>
    /// Revalidates every field that declared <paramref name="changedPath"/> in its
    /// <see cref="BormFieldDefinition.RevalidateOn"/> list — the other half of a cross-field rule.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A rule that reads two values lives on one of them, so only one of the two changes ever runs it.
    /// The confirm-password case is the whole story: the user mistypes the confirmation and is told the
    /// two do not match, then fixes the <em>password</em> to agree with what they typed — and the
    /// message under the confirmation box is now wrong, and stays wrong, because nothing revalidated a
    /// field the user did not touch. React Hook Form's <c>deps</c> and TanStack Form's
    /// <c>onChangeListenTo</c> exist for exactly this.
    /// </para>
    /// <para>
    /// A dependent that has nothing to say is left alone: a field the user has never visited on a form
    /// that has never been submitted must not start showing errors because a different field changed.
    /// The point is to correct a verdict already on screen, not to bring forward one that is not.
    /// </para>
    /// </remarks>
    public async ValueTask ValidateDependentsAsync(string changedPath, bool includeAsync = false)
    {
        if (!_hasRevalidateOn || string.IsNullOrEmpty(changedPath)) return;

        // Materialised before the loop: validating writes messages, and a rule is free to read the
        // live arrays the walk is enumerating.
        List<(BormFieldDefinition Field, string Path)>? dependents = null;
        foreach (var (field, path) in EnumerateFieldPaths())
        {
            if (field.RevalidateOn.Count == 0) continue;
            // A field is never its own dependent; its own write already validated it.
            if (string.Equals(path, changedPath, StringComparison.OrdinalIgnoreCase)) continue;
            if (!DeclaresPath(field.RevalidateOn, changedPath, BormPath.Parent(path))) continue;
            // Nothing is currently being claimed about this field, so there is nothing to correct.
            if (!IsSubmitted && !IsTouched(path) && MessagesFor(path).Count == 0) continue;

            (dependents ??= []).Add((field, path));
        }

        if (dependents is null) return;
        foreach (var (field, path) in dependents)
            await ValidateFieldAsync(field, path, includeAsync);
    }

    /// <summary>
    /// Runs the field's <see cref="BormFieldDefinition.Normalize"/> over the value at
    /// <paramref name="path"/>, writing the result back when it differs. Returns true if anything
    /// changed.
    /// </summary>
    /// <remarks>
    /// The write goes through the ordinary path, so tidying a value runs the same sweeps typing one
    /// does — a total that reads it recomputes, a condition that watches it re-evaluates, an autosave
    /// hears about it. It is written <em>quietly</em>: normalising is the form tidying up after the
    /// user, not the user visiting a field.
    /// </remarks>
    public bool NormalizeField(BormFieldDefinition field, string path)
    {
        if (field.Normalize is not { } normalize) return false;

        var current = Data.GetValue(path);
        var tidied = normalize(current);
        if (Equals(current, tidied)) return false;

        SetValueQuietly(path, tidied);
        return true;
    }

    /// <summary>
    /// Runs every declared normalizer across the form — including one per row of a repeater. Called
    /// before a submit validates, so a field the user never visited is still tidied.
    /// </summary>
    public bool NormalizeAll()
    {
        if (!_hasNormalizers) return false;

        var changed = false;
        // Materialised: normalising writes values, and a write runs sweeps that mutate the live arrays
        // this walk is reading.
        foreach (var (field, path) in EnumerateFieldPaths().ToList())
            changed |= NormalizeField(field, path);
        return changed;
    }

    /// <summary>
    /// Whether a change (or blur) on <paramref name="path"/> should trigger validation, given the
    /// configured triggers. A field that already shows an error always revalidates eagerly so the
    /// error disappears as soon as it is fixed.
    /// </summary>
    public bool ShouldValidate(string path, BormValidationTrigger trigger)
    {
        var effective = IsSubmitted || MessagesFor(path).Count > 0 ? RevalidationTrigger : ValidationTrigger;
        return effective switch
        {
            BormValidationTrigger.OnChange => trigger is BormValidationTrigger.OnChange or BormValidationTrigger.OnBlur,
            BormValidationTrigger.OnBlur => trigger is BormValidationTrigger.OnBlur,
            _ => false
        };
    }

    // ---------------------------------------------------------------- focus

    private readonly Dictionary<string, Func<ValueTask<bool>>> _focusTargets = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a way to move focus to the control rendering <paramref name="path"/>. Input components
    /// call this so the state can honour "take me to the first error" without knowing anything about
    /// the DOM. The callback reports whether focus actually landed. Dispose the returned token to
    /// unregister.
    /// </summary>
    public IDisposable RegisterFocusTarget(string path, Func<ValueTask<bool>> focus)
    {
        _focusTargets[path] = focus;
        return new FocusRegistration(this, path, focus);
    }

    /// <summary>
    /// Moves focus to the control at <paramref name="path"/>. Returns false when nothing is rendering
    /// it — a field on another wizard step, one a condition has hidden, or a control with no focusable
    /// element of its own.
    /// </summary>
    /// <remarks>
    /// A path no control renders by itself — an object, or one row of a repeater — sends focus to the
    /// first control inside it that will take it. A message about "the address" or "line 2" as a whole
    /// is fixed somewhere in there, and the error summary's link to it, and focus-the-first-error after
    /// a failed submit, otherwise went nowhere at all.
    /// </remarks>
    public async ValueTask<bool> FocusAsync(string path)
    {
        if (_focusTargets.TryGetValue(path, out var focus)) return await focus();
        if (string.IsNullOrEmpty(path) || _focusTargets.Count == 0) return false;

        foreach (var (_, candidate) in EnumerateFieldPaths().ToList())
        {
            if (candidate.Length <= path.Length || !BormPath.IsAtOrUnder(candidate, path)) continue;
            if (_focusTargets.TryGetValue(candidate, out var inner) && await inner()) return true;
        }
        return false;
    }

    private sealed class FocusRegistration(BormState state, string path, Func<ValueTask<bool>> focus) : IDisposable
    {
        public void Dispose()
        {
            // Only remove our own registration: a field re-created at the same path during a re-render
            // registers before the old instance is disposed.
            if (state._focusTargets.TryGetValue(path, out var current) && ReferenceEquals(current, focus))
                state._focusTargets.Remove(path);
        }
    }

    /// <summary>Adds a message for a path (used by server-side validation results).</summary>
    public void SetServerError(string path, string message)
    {
        AddMessage(new BormValidationMessage(path, message));
        _touched.Add(path);
        NotifyChanged();
    }

    /// <summary>
    /// Replaces the form's messages with those returned by a server round-trip, so a failed POST can
    /// surface its errors on the right fields in one call.
    /// </summary>
    public void SetServerErrors(IEnumerable<BormValidationMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        ReplaceAllMessages(messages.ToList());
        // A value the model could never accept is not something the server has an opinion about — it
        // never reached the server — so replacing the message set must not take its complaint with it,
        // leaving the box showing text the model does not hold and nothing at all to say why.
        ReapplyConversionErrors();
        foreach (var m in _messages.Keys) _touched.Add(m);
        NotifyChanged();
    }

    public void ClearMessages()
    {
        _messages.Clear();
        NotifyChanged();
    }

    /// <summary>Clears the messages for a single field and everything nested beneath it.</summary>
    public void ClearMessages(string path)
    {
        RemoveMessagesUnder(path);
        NotifyChanged();
    }

    // ---------------------------------------------------------------- submission

    /// <summary>
    /// Runs the full submit cycle: marks every field touched, validates, and invokes
    /// <paramref name="onValid"/> or <paramref name="onInvalid"/>. Concurrent calls are ignored while a
    /// submit is in flight, which is what stops a double-click from posting twice.
    /// </summary>
    /// <returns>
    /// Whether the form passed validation. A call ignored because a submit was already in flight returns
    /// false without validating anything or raising either callback, and leaves the messages as the
    /// earlier attempt left them; check <see cref="IsSubmitting"/> before calling to tell the two apart.
    /// </returns>
    public async Task<bool> SubmitAsync(
        Func<BormState, Task>? onValid = null,
        Func<BormState, Task>? onInvalid = null)
    {
        if (IsSubmitting) return false;

        IsSubmitting = true;
        IsSubmitSuccessful = false;
        RegisterSubmitAttempt();
        // Tidy first, judge second. A field the user never visited never blurred, so this is the only
        // chance its normalizer gets — and a value that reaches the handler should be the one that
        // would have been stored.
        NormalizeAll();
        MarkAllTouched();
        NotifyChanged();
        try
        {
            var valid = await ValidateAsync();
            if (valid)
            {
                if (onValid is not null) await onValid(this);
                // Set only once the handler has returned: a save that threw did not succeed, and a
                // "Saved" banner bound to this must not appear over the top of the failure.
                IsSubmitSuccessful = true;
            }
            else if (onInvalid is not null)
            {
                await onInvalid(this);
            }
            return valid;
        }
        finally
        {
            IsSubmitting = false;
            NotifyChanged();
        }
    }

    // ---------------------------------------------------------------- reset

    /// <summary>
    /// Restores the values captured when the state was created, and clears validation, touched/dirty
    /// tracking, the submit count and the wizard position.
    /// </summary>
    /// <remarks>
    /// Values are restored, not deep-cloned: array elements that already existed when the state was
    /// created are put back by reference, so edits made *inside* one of those objects are not rolled
    /// back. Items added after construction are removed.
    /// </remarks>
    public void Reset()
    {
        foreach (var (path, value) in _initialValues)
            RestoreInitial(path, value);

        ClearTracking();
        NotifyChanged();
    }

    /// <summary>
    /// Rebases the form onto <paramref name="values"/>: they are written, become the new baseline for
    /// <see cref="Reset()"/> and <see cref="IsFormDirty"/>, and everything the previous session
    /// accumulated — messages, touched and dirty flags, the submit count, the wizard position — is
    /// cleared. This is React Hook Form's <c>reset(values)</c>, and it is what an edit form needs after
    /// a save round-trip returns the stored record: <see cref="Reset()"/> would put back the values the
    /// form was constructed with, which are now the wrong ones.
    /// </summary>
    /// <remarks>
    /// Only the paths named are written; anything else keeps the value it currently holds and is
    /// baselined at that value. Pass every field to replace the form's contents wholesale.
    /// </remarks>
    public void Reset(IEnumerable<KeyValuePair<string, object?>> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        foreach (var (path, value) in values)
            Data.SetValue(path, value);

        // Computed fields are seeded against the new data before it is captured, exactly as they are
        // when the form is constructed — otherwise every one of them would be dirty from the outset.
        RecomputeValues(changedPath: null);

        _initialValues.Clear();
        CaptureInitialValues();

        ClearTracking();
        NotifyChanged();
    }

    /// <summary>
    /// Every value the schema binds to, as a flat path/value map — the shape
    /// <see cref="Reset(IEnumerable{KeyValuePair{string, object?}})"/> takes back, so the pair is a
    /// complete save-and-restore for a draft: stash a snapshot while the user is halfway through, hand
    /// it back when they return.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Live array elements are walked, so a repeater contributes one entry per row per field
    /// (<c>Lines[0].Product</c>, <c>Lines[1].Product</c>, …) rather than one opaque list. Presentational
    /// fields and object containers hold no value of their own and are skipped, exactly as they are
    /// when the form captures its baseline.
    /// </para>
    /// <para>
    /// Values are the objects the model holds, not copies: this is a map of what is there, not a deep
    /// clone of it. Serialise it if it has to outlive the objects.
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<string, object?> Snapshot()
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in EnumerateValuePaths())
            values[path] = Data.GetValue(path);
        return values;
    }

    /// <summary>
    /// Drops everything accumulated since the form opened — messages, touched/dirty flags, conversion
    /// failures, cached options, the submit count and the wizard position — without touching the data.
    /// </summary>
    private void ClearTracking()
    {
        _messages.Clear();
        _touched.Clear();
        _dirty.Clear();
        _conversionErrors.Clear();

        // Cancel before clearing: a load still in flight would otherwise land in the cache after the
        // reset and repopulate a select the user has just cleared.
        foreach (var path in _optionsCts.Keys.ToList()) CancelOptionsLoad(path);
        _loadedOptions.Clear();
        _optionsErrors.Clear();
        OptionsVersion++;

        // Likewise a validation still waiting on an async rule: it judged the data as it was before
        // the reset, and landing afterwards it would put an error on a form that has just been cleared.
        _formValidationCts?.Cancel();
        _formValidationCts?.Dispose();
        _formValidationCts = null;
        IsValidating = false;
        foreach (var cts in _fieldValidationCts.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }
        _fieldValidationCts.Clear();
        _validatingFields.Clear();

        SubmitCount = 0;
        HasValidated = false;
        IsSubmitSuccessful = false;
        CurrentStepIndex = 0;
        FurthestStepIndex = 0;
        ClampStep();
    }

    /// <summary>
    /// Puts one field — and anything nested beneath it — back to the value it started with, clearing
    /// its messages and its touched/dirty state. This is the "undo just this answer" that a long form
    /// needs and that <see cref="Reset()"/> is far too blunt for; React Hook Form spells it
    /// <c>resetField</c>.
    /// </summary>
    /// <remarks>
    /// A path the form has no baseline for — a field inside a row added since construction — is
    /// emptied, because "the value it started with" is nothing.
    /// </remarks>
    public void ResetField(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        var restored = false;
        foreach (var (key, initial) in _initialValues.Where(kv => BormPath.IsAtOrUnder(kv.Key, path)))
        {
            RestoreInitial(key, initial);
            restored = true;
        }

        // Nothing under this path was ever captured, so its starting value was nothing. Note the test is
        // "no baseline anywhere beneath", not "none at this exact path": an object field holds no value
        // of its own, so emptying it would throw away the children that were just put back.
        if (!restored && Data.GetValue(path) is not null)
            Data.SetValue(path, null);

        RemoveMessagesUnder(path);
        RemovePathsUnder(_touched, path);
        RemovePathsUnder(_dirty, path);
        foreach (var key in _conversionErrors.Keys.Where(k => BormPath.IsAtOrUnder(k, path)).ToList())
            _conversionErrors.Remove(key);

        // Putting one answer back is a change to the form, so it runs the same sweeps a typed-in value
        // does. Without them, undoing the country left the city's options loaded for the country the
        // user had just abandoned, a branch this answer was keeping open stayed open, and nothing
        // watching FieldChanged — an autosave, a live preview — heard about it at all.
        ClearHiddenValues(path);
        RecomputeValues(path);
        InvalidateDependentOptions(path);
        ClampStep();
        FieldChanged?.Invoke(path);
        NotifyChanged();
    }

    /// <summary>Puts a single captured baseline back, restoring collection contents element by element.</summary>
    private void RestoreInitial(string path, object? initial)
    {
        if (initial is List<object?> snapshot)
        {
            // Refilling the live list in place keeps the model's own collection instance, which
            // anything else holding a reference to it is relying on.
            var current = Data.GetValue(path);
            if (current is IList { IsFixedSize: false, IsReadOnly: false } live)
            {
                live.Clear();
                foreach (var item in snapshot) live.Add(item);
                return;
            }

            // No live collection to refill — a multi-select the user emptied, a property left null, or
            // an array, which cannot be cleared in place. Writing the snapshot lets the accessor build
            // one of the model's own type; returning here instead is how a reset used to leave the old
            // values in place. Null stays null only where null is what is there now: an empty array
            // property is put back as an empty array, not as a null nobody expects.
            Data.SetValue(path, snapshot.Count == 0 && current is null ? null : new List<object?>(snapshot));
            return;
        }
        Data.SetValue(path, initial);
    }

    /// <summary>
    /// Re-captures the current values as the baseline for <see cref="Reset()"/> and clears dirty
    /// tracking — call this after a successful save so the form is no longer reported as dirty.
    /// </summary>
    public void AcceptChanges()
    {
        _initialValues.Clear();
        CaptureInitialValues();
        _dirty.Clear();
        NotifyChanged();
    }

    private void CaptureInitialValues()
    {
        foreach (var (field, path) in EnumerateFieldPaths())
        {
            if (field.IsPresentational || field.Type == BormFieldType.Object) continue;

            var value = Data.GetValue(path);
            _initialValues[path] = HoldsManyValues(field, value)
                ? value is IEnumerable items and not string ? items.Cast<object?>().ToList() : new List<object?>()
                : value;
        }
    }

    /// <summary>
    /// Whether a field's baseline has to be a snapshot of its elements rather than the value itself.
    /// </summary>
    /// <remarks>
    /// A control that holds several values writes a brand-new collection every time one of them is
    /// toggled — a multi-select and a tag list both do — so keeping the reference as the baseline
    /// reports the field dirty from the first click and never clean again, whatever the user does. An
    /// "undo" button, an unsaved-changes prompt and a disabled save button all read that.
    /// </remarks>
    private static bool HoldsManyValues(BormFieldDefinition field, object? value)
    {
        // What is actually there is the most reliable evidence there is.
        if (value is IEnumerable and not string) return true;
        if (value is not null) return false;

        // Nothing there yet, so go by the declared shape — otherwise a multi-select that opens empty
        // would baseline as null and never compare equal to the empty list the control writes back.
        if (field.Type is not (BormFieldType.Array or BormFieldType.Tags or BormFieldType.MultiSelect))
            return false;

        // A [Flags] multi-select is the exception: it renders as a set of boxes but stores one
        // combined value, which compares perfectly well as itself.
        var declared = field.ValueType is { } t ? Nullable.GetUnderlyingType(t) ?? t : null;
        return declared is null || !declared.IsEnum;
    }

    // ---------------------------------------------------------------- wizard

    public BormStep? CurrentStep
    {
        get
        {
            if (!Definition.IsWizard) return null;
            ClampStep();
            return Definition.Steps[CurrentStepIndex];
        }
    }

    /// <summary>
    /// Moves off a step that a condition has just hidden. Without this the wizard would render — and
    /// validate — a step the schema says does not apply, with no way for the user to leave it.
    /// The nearest earlier visible step is preferred, so the user lands on ground they have already seen.
    /// </summary>
    private void ClampStep()
    {
        if (!Definition.IsWizard) return;
        if (CurrentStepIndex >= 0 && CurrentStepIndex < Definition.Steps.Count
            && IsStepVisible(Definition.Steps[CurrentStepIndex])) return;

        for (var i = Math.Min(CurrentStepIndex, Definition.Steps.Count) - 1; i >= 0; i--)
        {
            if (IsStepVisible(Definition.Steps[i])) { CurrentStepIndex = i; return; }
        }
        for (var i = Math.Max(CurrentStepIndex + 1, 0); i < Definition.Steps.Count; i++)
        {
            if (IsStepVisible(Definition.Steps[i])) { CurrentStepIndex = i; return; }
        }
        CurrentStepIndex = 0;
    }

    /// <summary>
    /// Drops messages belonging to a step the schema currently hides. A full validation walks every
    /// field, so without this a branch of the wizard the user was never shown could report errors that
    /// block submission and appear on no reachable page.
    /// </summary>
    private IReadOnlyList<BormValidationMessage> DropHiddenStepMessages(IReadOnlyList<BormValidationMessage> messages)
    {
        var hidden = HiddenStepFields();
        if (hidden.Count == 0) return messages;

        return messages.Where(m => !hidden.Any(f => BormPath.IsAtOrUnder(m.FieldPath, f))).ToList();
    }

    /// <summary>
    /// The fields named only by wizard steps a condition currently hides. A field named by a visible
    /// step as well is not among them: two steps may legitimately share one field.
    /// </summary>
    private List<string> HiddenStepFields()
    {
        if (!Definition.IsWizard) return [];

        var hidden = Definition.Steps.Where(s => !IsStepVisible(s)).SelectMany(s => s.Fields).ToList();
        if (hidden.Count == 0) return hidden;

        var shown = Definition.Steps.Where(IsStepVisible).SelectMany(s => s.Fields).ToHashSet(StringComparer.OrdinalIgnoreCase);
        hidden.RemoveAll(shown.Contains);
        return hidden;
    }

    /// <summary>
    /// The index of the first visible wizard step that shows <paramref name="path"/>, or -1 when no
    /// visible step does (or the form is not a wizard).
    /// </summary>
    internal int VisibleStepContaining(string path)
    {
        for (var i = 0; i < Definition.Steps.Count; i++)
        {
            var step = Definition.Steps[i];
            if (IsStepVisible(step) && StepContains(step, path)) return i;
        }
        return -1;
    }

    /// <summary>Whether <paramref name="step"/> shows <paramref name="path"/>, directly or inside a field it names.</summary>
    internal static bool StepContains(BormStep step, string path)
    {
        foreach (var field in step.Fields)
            if (BormPath.IsAtOrUnder(path, field)) return true;
        return false;
    }

    public bool IsFirstStep => !Definition.IsWizard || CurrentStepIndex <= FirstVisibleStepIndex();
    public bool IsLastStep => !Definition.IsWizard || CurrentStepIndex >= LastVisibleStepIndex();

    /// <summary>The steps that are currently visible, in schema order.</summary>
    public IReadOnlyList<BormStep> VisibleSteps => Definition.Steps.Where(IsStepVisible).ToList();

    /// <summary>
    /// The 1-based position of the current step among the visible ones — what a stepper should show,
    /// so numbering stays contiguous when a conditional step is skipped.
    /// </summary>
    public int CurrentStepNumber
    {
        get
        {
            var n = 0;
            for (var i = 0; i <= CurrentStepIndex && i < Definition.Steps.Count; i++)
                if (IsStepVisible(Definition.Steps[i])) n++;
            return n;
        }
    }

    /// <summary>Advances to the next visible step after validating the current one.</summary>
    public async ValueTask<bool> NextStepAsync()
    {
        if (!Definition.IsWizard) return false;
        if (!await ValidateStepAsync()) return false;

        for (var i = CurrentStepIndex + 1; i < Definition.Steps.Count; i++)
        {
            if (IsStepVisible(Definition.Steps[i]))
            {
                CurrentStepIndex = i;
                FurthestStepIndex = Math.Max(FurthestStepIndex, i);
                NotifyChanged();
                return true;
            }
        }
        return false;
    }

    /// <summary>Moves to the previous visible step (no validation).</summary>
    public void PreviousStep()
    {
        if (!Definition.IsWizard) return;
        for (var i = CurrentStepIndex - 1; i >= 0; i--)
        {
            if (IsStepVisible(Definition.Steps[i]))
            {
                CurrentStepIndex = i;
                NotifyChanged();
                return;
            }
        }
    }

    /// <summary>
    /// Jumps to a step by index. Hidden steps and out-of-range indices are ignored. Jumping forward
    /// past a step the user has not reached is allowed here — the caller has asked for it explicitly —
    /// but it skips that step's validation, which is why the stepper only offers steps up to
    /// <see cref="FurthestStepIndex"/>.
    /// </summary>
    public void GoToStep(int index)
    {
        if (!Definition.IsWizard || index < 0 || index >= Definition.Steps.Count) return;
        if (!IsStepVisible(Definition.Steps[index])) return;
        if (index == CurrentStepIndex) return;

        CurrentStepIndex = index;
        FurthestStepIndex = Math.Max(FurthestStepIndex, index);
        NotifyChanged();
    }

    /// <summary>Jumps to a step by its <see cref="BormStep.Id"/>.</summary>
    public void GoToStep(string id)
    {
        for (var i = 0; i < Definition.Steps.Count; i++)
        {
            if (string.Equals(Definition.Steps[i].Id, id, StringComparison.OrdinalIgnoreCase))
            {
                GoToStep(i);
                return;
            }
        }
    }

    // ---------------------------------------------------------------- arrays

    /// <summary>Appends a new item to an array field and returns its index.</summary>
    public int AddArrayItem(BormFieldDefinition arrayField, string arrayPath)
        => InsertArrayItem(arrayField, arrayPath, int.MaxValue);

    /// <summary>
    /// Inserts a new item at <paramref name="index"/> (clamped into range) and returns the index it
    /// landed at. Messages below the insertion point are re-indexed so they stay with their item.
    /// </summary>
    public int InsertArrayItem(BormFieldDefinition arrayField, string arrayPath, int index)
        => InsertItem(arrayField, arrayPath, index, notify: true);

    private int InsertItem(BormFieldDefinition arrayField, string arrayPath, int index, bool notify)
    {
        var list = EnsureList(arrayPath);
        var target = Math.Clamp(index, 0, list.Count);
        var item = CreateItem(arrayField, arrayPath);

        if (!EditList(arrayPath, list, l => l.Insert(target, item))) return -1;

        RemapArrayIndices(arrayPath, i => i >= target ? i + 1 : i);
        TrackDirty(arrayPath);
        // The row was created a line ago, so every value in it is a placeholder the default may replace.
        ApplyItemDefaults(arrayField, BormPath.Combine(arrayPath, target), overwrite: true);
        // Recompute, the ClearOnHide sweep, the cascading-options sweep and the list's own rules (the
        // collection-size rule may now pass) all run once, together, in NotifyArrayChanged below.
        if (notify) NotifyArrayChanged(arrayPath);
        return target;
    }

    /// <summary>
    /// Settles the form after an array's contents changed, and announces it. A repeater operation
    /// changes the value of the field the list binds to as surely as typing does, so it runs the same
    /// sweeps a typed-in value runs and anything watching <see cref="FieldChanged"/> — an autosave, a
    /// preview, a page-level dirty prompt — hears about it.
    /// </summary>
    /// <remarks>
    /// Only the array's own path is raised: the rows beneath it moved, but which row is "the field that
    /// changed" is not a question with an answer. The sweeps matter as much as the event —
    /// <c>VisibleWhen("Lines", IsNotEmpty)</c> with <c>ClearOnHide</c> never fired when the last line
    /// was deleted, and a select loading its choices from the list never reloaded, because emptying a
    /// repeater was the one kind of change that told only half the engine about itself.
    /// </remarks>
    private void NotifyArrayChanged(string arrayPath, bool revalidate = true)
    {
        ClearHiddenValues(arrayPath);
        RecomputeValues(arrayPath);
        InvalidateDependentOptions(arrayPath, rowsMoved: true);
        // A row appearing or disappearing can satisfy — or break — the condition on a wizard step.
        ClampStep();
        if (revalidate) RevalidateArrayRules(arrayPath);
        FieldChanged?.Invoke(arrayPath);
        NotifyChanged();
    }

    /// <summary>
    /// Re-judges the rules declared on the list itself — its size, its uniqueness — after rows were
    /// added or removed, replacing only the messages on the list's own path.
    /// </summary>
    /// <remarks>
    /// Dropping them instead erased verdicts that were still true: "items must be unique" vanished when
    /// an unrelated row was added while the duplicates stayed, and nothing put it back before the next
    /// submit. Rows are not judged here, so a row the user has only just added does not open covered in
    /// errors, and a list nobody has been told anything about is left alone, exactly as
    /// <see cref="ValidateDependentsAsync"/> leaves a field with nothing on screen to correct.
    /// </remarks>
    private void RevalidateArrayRules(string arrayPath)
    {
        if (!IsSubmitted && !IsTouched(arrayPath) && MessagesFor(arrayPath).Count == 0) return;
        if (Definition.FindByPath(arrayPath) is not { } field) return;

        // Only what can be answered now. A rule that does not finish synchronously cannot be waited for
        // from here, and its verdict is left as it stands rather than guessed at.
        var pending = _validator.ValidateOwnRulesAsync(field, arrayPath, Data, Services);
        if (!pending.IsCompletedSuccessfully) return;

        RemoveMessagesFor(arrayPath);
        foreach (var m in pending.Result)
            if (string.Equals(m.FieldPath, arrayPath, StringComparison.OrdinalIgnoreCase)) AddMessage(m);
        if (_conversionErrors.ContainsKey(arrayPath)) AddMessage(ConversionMessage(arrayPath));
    }

    /// <summary>
    /// Inserts a copy of the item at <paramref name="index"/> directly after it and returns the new
    /// index, or -1 if the row could not be copied. This is the "add another one like that" a repeater
    /// of anything non-trivial always ends up needing.
    /// </summary>
    /// <remarks>
    /// Values are copied field by field through the schema rather than by cloning the object, so the
    /// new row is genuinely independent — a shallow copy would leave the two rows sharing the same
    /// nested objects and editing one would silently edit the other.
    /// </remarks>
    public int DuplicateArrayItem(BormFieldDefinition arrayField, string arrayPath, int index)
    {
        if (Data.GetValue(arrayPath) is not IList list || index < 0 || index >= list.Count) return -1;

        var source = BormPath.Combine(arrayPath, index);
        // Snapshot before inserting: the insert shifts the source row's own path.
        var values = ItemValuePaths(arrayField, source)
            .Select(p => (Relative: p[source.Length..], Value: Data.GetValue(p)))
            .ToList();

        // Quietly: the row is announced once it holds the copy, not while it is still the empty one the
        // insert created — a listener that reacted to the first event would save a blank line.
        var target = InsertItem(arrayField, arrayPath, index + 1, notify: false);
        if (target < 0) return -1;

        var targetPath = BormPath.Combine(arrayPath, target);
        foreach (var (relative, value) in values)
        {
            // A multi-select or tag list in the row holds a collection of its own. Handing the copy the
            // same instance would leave the two rows sharing it, so it gets its elements instead — the
            // accessor builds a collection of the model's own type from them.
            var copy = value is IList items and not Array ? items.Cast<object?>().ToList() : value;
            Data.SetValue(targetPath + relative, copy);
        }

        TrackDirty(arrayPath);
        // Recompute, the ClearOnHide sweep, the cascading-options sweep and the list's own rules all
        // run once, together, in NotifyArrayChanged below — judged on the row holding its copy.
        NotifyArrayChanged(arrayPath);
        return target;
    }

    /// <summary>Every value-bearing path inside one repeater row, in schema order.</summary>
    private IEnumerable<string> ItemValuePaths(BormFieldDefinition arrayField, string itemPath)
    {
        if (arrayField.ItemTemplate is not { } template) yield break;

        if (template.Type == BormFieldType.Object)
        {
            foreach (var child in template.Children)
                foreach (var path in Leaves(child, BormPath.Combine(itemPath, child.Name)))
                    yield return path;
        }
        else
        {
            foreach (var path in Leaves(template, itemPath))
                yield return path;
        }

        IEnumerable<string> Leaves(BormFieldDefinition field, string path)
        {
            if (field.Type == BormFieldType.Object)
            {
                foreach (var child in field.Children)
                    foreach (var p in Leaves(child, BormPath.Combine(path, child.Name)))
                        yield return p;
            }
            else if (field.Type == BormFieldType.Array && field.ItemTemplate is { } nested)
            {
                // A nested list is walked element by element rather than assigned across. Assigning it
                // would hand both rows the same list instance, and editing one would edit the other.
                var count = ArrayCount(path);
                for (var i = 0; i < count; i++)
                {
                    var nestedItem = BormPath.Combine(path, i);
                    if (nested.Type == BormFieldType.Object)
                    {
                        foreach (var child in nested.Children)
                            foreach (var p in Leaves(child, BormPath.Combine(nestedItem, child.Name)))
                                yield return p;
                    }
                    else
                    {
                        foreach (var p in Leaves(nested, nestedItem)) yield return p;
                    }
                }
            }
            else
            {
                yield return path;
            }
        }
    }

    /// <summary>Removes an item from an array field.</summary>
    public void RemoveArrayItem(string arrayPath, int index)
    {
        if (Data.GetValue(arrayPath) is not IList list || index < 0 || index >= list.Count) return;
        if (!EditList(arrayPath, list, l => l.RemoveAt(index))) return;

        // The removed row's state goes with it, and everything after the hole shifts up, taking its
        // own along — otherwise the item that moves into this slot inherits the removed item's errors,
        // touched flags and loaded choices.
        RemapArrayIndices(arrayPath, i => i == index ? null : i > index ? i - 1 : i);

        TrackDirty(arrayPath);
        // Recompute, the ClearOnHide sweep, the cascading-options sweep and the list's own rules all
        // run once, together, in NotifyArrayChanged below.
        NotifyArrayChanged(arrayPath);
    }

    /// <summary>Moves an array item from one index to another.</summary>
    public void MoveArrayItem(string arrayPath, int from, int to)
    {
        if (Data.GetValue(arrayPath) is not IList list) return;
        if (from < 0 || from >= list.Count || to < 0 || to >= list.Count || from == to) return;

        var moved = EditList(arrayPath, list, l =>
        {
            var item = l[from];
            l.RemoveAt(from);
            l.Insert(to, item);
        });
        if (!moved) return;

        // Indices shifted, so everything keyed by them is re-keyed to follow its item — the row the
        // user just moved keeps the error it already showed instead of appearing to be fixed.
        RemapArrayIndices(arrayPath, index => MapMovedIndex(index, from, to));
        TrackDirty(arrayPath);
        // Recompute, the ClearOnHide sweep and the cascading-options sweep all run once, together, in
        // NotifyArrayChanged below. Reordering changes neither the size nor the set of values, so the
        // list's own rules have nothing new to say.
        NotifyArrayChanged(arrayPath, revalidate: false);
    }

    /// <summary>
    /// Exchanges two array items, taking their messages and touched/dirty state with them. Distinct
    /// from <see cref="MoveArrayItem"/>, which shuffles everything between the two positions along by
    /// one; a swap leaves every other row exactly where it was, which is what a drag-and-drop reorder
    /// of two rows means and what React Hook Form's <c>useFieldArray</c> spells <c>swap</c>.
    /// </summary>
    public void SwapArrayItems(string arrayPath, int first, int second)
    {
        if (Data.GetValue(arrayPath) is not IList list) return;
        if (first < 0 || first >= list.Count || second < 0 || second >= list.Count || first == second) return;

        var swapped = EditList(arrayPath, list, l =>
        {
            (l[first], l[second]) = (l[second], l[first]);
        });
        if (!swapped) return;

        RemapArrayIndices(arrayPath, index => index == first ? second : index == second ? first : index);
        TrackDirty(arrayPath);
        // Recompute, the ClearOnHide sweep and the cascading-options sweep all run once, together, in
        // NotifyArrayChanged below. A swap is a reorder: the list's own rules have nothing new to say.
        NotifyArrayChanged(arrayPath, revalidate: false);
    }

    /// <summary>
    /// Empties an array field, discarding every row's messages and touched/dirty state along with it.
    /// Removing rows one at a time costs a re-render and a re-index per row, and the caller has to walk
    /// backwards to avoid the indices moving underneath them.
    /// </summary>
    public void ClearArrayItems(string arrayPath)
    {
        if (Data.GetValue(arrayPath) is not IList list || list.Count == 0) return;
        // A collection the model will not let go of keeps its rows rather than the form throwing.
        if (!EditList(arrayPath, list, l => l.Clear())) return;

        RemapArrayIndices(arrayPath, _ => null);
        TrackDirty(arrayPath);
        // Recompute, the ClearOnHide sweep, the cascading-options sweep and the list's own rules (the
        // collection-size rule is judged against the field itself) all run once, together, in
        // NotifyArrayChanged below.
        NotifyArrayChanged(arrayPath);
    }

    public int ArrayCount(string arrayPath) => Data.GetValue(arrayPath) switch
    {
        null => 0,
        ICollection c => c.Count,
        IEnumerable e and not string => e.Cast<object?>().Count(),
        _ => 0
    };

    /// <summary>Whether another item may be added, given <see cref="BormFieldDefinition.MaxItems"/>.</summary>
    public bool CanAddArrayItem(BormFieldDefinition arrayField, string arrayPath)
        => arrayField.MaxItems is not { } max || ArrayCount(arrayPath) < max;

    /// <summary>Whether an item may be removed, given <see cref="BormFieldDefinition.MinItems"/>.</summary>
    public bool CanRemoveArrayItem(BormFieldDefinition arrayField, string arrayPath)
        => arrayField.MinItems is not { } min || ArrayCount(arrayPath) > min;

    // ---------------------------------------------------------------- internals

    private int FirstVisibleStepIndex()
    {
        for (var i = 0; i < Definition.Steps.Count; i++)
            if (IsStepVisible(Definition.Steps[i])) return i;
        return 0;
    }

    private int LastVisibleStepIndex()
    {
        for (var i = Definition.Steps.Count - 1; i >= 0; i--)
            if (IsStepVisible(Definition.Steps[i])) return i;
        return 0;
    }

    private void NotifyChanged()
    {
        if (_disposed) return;

        // Inside a batch the notification is deferred, not dropped: the outermost Batch raises exactly
        // one at the end, however many writes it contained.
        if (_batchDepth > 0)
        {
            _batchChanged = true;
            return;
        }

        StateChanged?.Invoke();
    }

    private async ValueTask<IReadOnlyList<BormValidationMessage>> MergeExternal(IReadOnlyList<BormValidationMessage> messages)
    {
        if (ExternalValidator is null) return messages;
        var external = await ExternalValidator(Definition, Data, Services);
        if (external.Count == 0) return messages;

        // The hidden set is computed once for the batch rather than once per message. Asking per
        // message walks every field path and evaluates every condition again, so a validator reporting
        // fifty failures across a two-hundred-field schema did ten thousand condition evaluations to
        // answer a question whose answer had not changed since the first one.
        var hidden = HiddenFieldPaths();
        return hidden.Count == 0
            ? messages.Concat(external).ToList()
            : messages.Concat(external.Where(m => !IsHidden(m.FieldPath, hidden))).ToList();
    }

    /// <summary>
    /// Every field path the schema is currently hiding. An external validator — a FluentValidation
    /// validator, say — sees the whole model and knows nothing about the form's conditions, so a rule
    /// on a branch the user was never shown would otherwise block submission with an error on a control
    /// that is not on the page.
    /// </summary>
    private List<string> HiddenFieldPaths()
    {
        var hidden = new List<string>();
        foreach (var (field, fieldPath) in EnumerateFieldPaths())
        {
            if (field.VisibleWhen is null) continue;
            // Already inside a hidden branch: its own condition cannot make it visible again, and
            // testing it would be one condition evaluation per descendant of every hidden container.
            if (IsHidden(fieldPath, hidden)) continue;
            if (!field.VisibleWhen.Evaluate(ScopeFor(fieldPath))) hidden.Add(fieldPath);
        }
        return hidden;
    }

    /// <summary>Whether a path lies at or under one of the currently hidden fields.</summary>
    private static bool IsHidden(string path, List<string> hidden)
    {
        if (string.IsNullOrEmpty(path)) return false;
        for (var i = 0; i < hidden.Count; i++)
            if (BormPath.IsAtOrUnder(path, hidden[i])) return true;
        return false;
    }

    private void ReplaceAllMessages(IReadOnlyList<BormValidationMessage> messages)
    {
        _messages.Clear();
        foreach (var m in messages) AddMessage(m);
    }

    /// <summary>
    /// Records a message against its field.
    /// </summary>
    /// <remarks>
    /// The list is replaced rather than appended to. A field's rendered output is diffed against a
    /// signature that treats its message list's <em>identity</em> as its content — the cheapest correct
    /// test, since a validation pass always rebuilds the list — so appending in place left the identity
    /// unchanged and the field decided it had nothing new to draw. A second
    /// <see cref="SetServerError"/> on a field that was already reporting something was recorded in the
    /// state and never appeared on the page.
    /// </remarks>
    private void AddMessage(BormValidationMessage message)
    {
        if (!_messages.TryGetValue(message.FieldPath, out var list))
        {
            _messages[message.FieldPath] = [message];
            return;
        }

        if (SingleErrorPerField && message.Severity == BormValidationSeverity.Error)
        {
            for (var i = 0; i < list.Count; i++)
                if (list[i].Severity == BormValidationSeverity.Error) return;
        }

        var next = new List<BormValidationMessage>(list.Count + 1);
        next.AddRange(list);
        next.Add(message);
        _messages[message.FieldPath] = next;
    }

    /// <summary>
    /// Moves a batch of messages onto <paramref name="key"/>, replacing whatever list is there rather
    /// than appending to it — see <see cref="AddMessage"/> for why the identity has to change.
    /// </summary>
    private void AppendMessages(string key, List<BormValidationMessage> messages)
    {
        if (!_messages.TryGetValue(key, out var existing))
        {
            _messages[key] = messages;
            return;
        }

        var next = new List<BormValidationMessage>(existing.Count + messages.Count);
        next.AddRange(existing);
        next.AddRange(messages);
        _messages[key] = next;
    }

    private void RemoveMessagesFor(string path) => _messages.Remove(path);

    private void RemoveMessagesUnder(string path)
    {
        var keys = _messages.Keys.Where(k => BormPath.IsAtOrUnder(k, path)).ToList();
        foreach (var k in keys) _messages.Remove(k);
    }

    /// <summary>
    /// Where an item ends up after a move. Everything between the old and the new position shifts one
    /// place to make room, which is what keeps the rest of the rows lined up with their own state.
    /// </summary>
    private static int MapMovedIndex(int index, int from, int to)
    {
        if (index == from) return to;
        if (from < to) return index > from && index <= to ? index - 1 : index;
        return index >= to && index < from ? index + 1 : index;
    }

    /// <summary>
    /// Re-keys everything the form keeps per path for an element of <paramref name="arrayPath"/>
    /// through <paramref name="map"/>, so it follows its row after an insert, remove, move or swap. A
    /// null from <paramref name="map"/> means the row is gone, and its state goes with it.
    /// </summary>
    /// <remarks>
    /// Messages, touched and dirty flags are the obvious part; the rest is what made a removed row
    /// haunt its successor. A rejected entry kept being re-reported at its old path — on a submit, as
    /// an error on a row that no longer existed, blocking the form for good — and a cascading select's
    /// loaded choices stayed behind, so the row that moved into the slot offered the removed row's
    /// cities. Entries are lifted out before any are put back, so two rows swapping places cannot
    /// collide mid-flight. Baselines are deliberately left where they are: <see cref="Reset()"/>
    /// restores them position by position into the original rows.
    /// </remarks>
    private void RemapArrayIndices(string arrayPath, Func<int, int?> map)
    {
        RemapMessages(arrayPath, map);
        RemapKeys(_touched, arrayPath, map);
        RemapKeys(_dirty, arrayPath, map);
        RemapKeys(_conversionErrors, arrayPath, map);
        RemapKeys(_loadedOptions, arrayPath, map);
        RemapKeys(_optionsErrors, arrayPath, map);
        OptionsVersion++;

        // Work still in flight was started for whichever row stood at its path at the time, and its
        // answer would land on whichever row stands there now. It is abandoned instead: a moved row
        // reloads its choices under its new path the next time it renders.
        foreach (var path in _optionsCts.Keys.Where(Moves).ToList())
            CancelOptionsLoad(path);
        foreach (var path in _fieldValidationCts.Keys.Where(Moves).ToList())
        {
            _fieldValidationCts.Remove(path, out var cts);
            cts!.Cancel();
            cts.Dispose();
            _validatingFields.Remove(path);
        }

        bool Moves(string key) => BormPath.IndexIn(key, arrayPath) is { } index && map(index) != index;
    }

    private void RemapMessages(string arrayPath, Func<int, int?> map)
    {
        var lifted = new List<(string Key, List<BormValidationMessage> Messages)>();
        foreach (var key in _messages.Keys.ToList())
        {
            if (BormPath.IndexIn(key, arrayPath) is not { } index) continue;
            var target = map(index);
            if (target == index) continue;

            _messages.Remove(key, out var list);
            if (target is not { } moved || BormPath.Reindex(key, arrayPath, moved) is not { } newKey) continue;

            lifted.Add((newKey, list!.Select(m => m with
            {
                FieldPath = BormPath.Reindex(m.FieldPath, arrayPath, moved) ?? m.FieldPath
            }).ToList()));
        }

        foreach (var (key, messages) in lifted)
            AppendMessages(key, messages);
    }

    private static void RemapKeys(HashSet<string> set, string arrayPath, Func<int, int?> map)
    {
        if (set.Count == 0) return;

        var lifted = new List<string>();
        foreach (var key in set.ToList())
        {
            if (BormPath.IndexIn(key, arrayPath) is not { } index) continue;
            var target = map(index);
            if (target == index) continue;

            set.Remove(key);
            if (target is { } moved && BormPath.Reindex(key, arrayPath, moved) is { } newKey) lifted.Add(newKey);
        }
        foreach (var key in lifted) set.Add(key);
    }

    private static void RemapKeys<T>(Dictionary<string, T> entries, string arrayPath, Func<int, int?> map)
    {
        if (entries.Count == 0) return;

        var lifted = new List<(string Key, T Value)>();
        foreach (var key in entries.Keys.ToList())
        {
            if (BormPath.IndexIn(key, arrayPath) is not { } index) continue;
            var target = map(index);
            if (target == index) continue;

            entries.Remove(key, out var value);
            if (target is { } moved && BormPath.Reindex(key, arrayPath, moved) is { } newKey)
                lifted.Add((newKey, value!));
        }
        foreach (var (key, value) in lifted) entries[key] = value;
    }

    /// <summary>
    /// Applies a structural edit to the collection at <paramref name="arrayPath"/>, returning false
    /// when the model would not take it.
    /// </summary>
    /// <remarks>
    /// A list is edited in place, keeping the model's own instance. A fixed-size collection — a
    /// <c>T[]</c> property — can be neither grown nor shrunk, so the edit is made to a copy that is
    /// written back, and the accessor converts it to the property's own type. Without this, adding a
    /// row to an array property silently did nothing, and removing or moving one threw.
    /// </remarks>
    private bool EditList(string arrayPath, IList list, Action<IList> edit)
    {
        if (!list.IsFixedSize && !list.IsReadOnly)
        {
            try
            {
                edit(list);
                return true;
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or InvalidCastException)
            {
                return false;
            }
        }

        var copy = new List<object?>(list.Count + 1);
        foreach (var item in list) copy.Add(item);
        edit(copy);
        Data.SetValue(arrayPath, copy);
        return !Data.LastWriteFailed;
    }

    /// <summary>Drops every entry in a path set at or beneath <paramref name="path"/>.</summary>
    private static void RemovePathsUnder(HashSet<string> set, string path)
    {
        if (set.Count == 0) return;
        foreach (var key in set.Where(k => BormPath.IsAtOrUnder(k, path)).ToList())
            set.Remove(key);
    }

    private void ApplyDefaults()
    {
        foreach (var field in Definition.Fields)
            ApplyDefault(field, field.Name);
    }

    /// <summary>
    /// Creates the rows a repeater marked <c>SeedMinItems()</c> is required to have, so a form whose
    /// schema says "at least one line" opens with a line rather than with an error about not having one.
    /// </summary>
    /// <remarks>
    /// Opt-in, not automatic: on an edit form loaded from storage, "this record has no lines" can be
    /// the truth, and inventing one would be the library editing the user's data on their behalf.
    /// </remarks>
    private void SeedRequiredArrayItems()
    {
        foreach (var (field, path) in EnumerateFieldPaths().ToList())
        {
            if (field.Type != BormFieldType.Array || field.MinItems is not { } min || min <= 0) continue;
            if (!field.Attributes.TryGetValue("seedMinItems", out var seed) || seed is not true) continue;

            for (var i = ArrayCount(path); i < min; i++)
                if (InsertArrayItem(field, path, int.MaxValue) < 0) break;
        }
    }

    /// <summary>
    /// Applies a field's declared default. <paramref name="overwrite"/> distinguishes the two cases
    /// that look identical from here: data the form was handed, where only a missing value may be
    /// filled in, and a row the form has just created, where every value is a placeholder. Without the
    /// distinction a default of 1 could never reach an <c>int</c> — a fresh one already reads as 0, and
    /// 0 is indistinguishable from a deliberate answer.
    /// </summary>
    private void ApplyDefault(BormFieldDefinition field, string path, bool overwrite = false)
    {
        // A heading has a name so the schema can address it, not because the model has a property of
        // that name. Writing to it would fail silently on a typed model and litter a dictionary one.
        if (field.IsPresentational) return;

        if (field.DefaultValue is not null && (overwrite || Data.GetValue(path) is null))
            Data.SetValue(path, CopyDefault(field.DefaultValue));
        else if (Data is BormDictionaryDataAccessor && !Data.TryGetValue(path, out _))
            // A dictionary only has the keys someone wrote, and a condition inside a row or an object
            // resolves "Kind" against that container first only when the container *has* a Kind —
            // otherwise it falls back to the root. A fresh row with nothing typed in yet therefore
            // answered with the root's Kind, showing and requiring fields the same row on a typed model
            // keeps hidden. Every field the schema declares is given its key, so the container owns it.
            Data.SetValue(path, null);

        if (field.Type == BormFieldType.Object)
        {
            foreach (var child in field.Children)
                ApplyDefault(child, BormPath.Combine(path, child.Name), overwrite);
        }
        else if (field.Type == BormFieldType.Array && field.ItemTemplate is not null && !overwrite)
        {
            // Existing rows are completed, never rewritten: a model loaded from storage keeps its data.
            var count = ArrayCount(path);
            for (var i = 0; i < count; i++)
                ApplyItemDefaults(field, BormPath.Combine(path, i), overwrite: false);
        }
    }

    /// <summary>
    /// A private copy of a declared default that holds other values — a list, an array, an object read
    /// from JSON. The default belongs to the schema, which is shared by every form built from it and
    /// every row it seeds; handing out the instance itself let a repeater edit it in place, so the next
    /// form opened with the last user's rows, and adding a nested item to one row added it to all.
    /// </summary>
    private static object? CopyDefault(object? value)
    {
        switch (value)
        {
            case null or string:
                return value;
            case IDictionary<string, object?> map:
            {
                var copy = BormDictionaryDataAccessor.NewObject();
                foreach (var (key, item) in map) copy[key] = CopyDefault(item);
                return copy;
            }
            case Array array:
            {
                var copy = (Array)array.Clone();
                for (var i = 0; i < copy.Length; i++) copy.SetValue(CopyDefault(copy.GetValue(i)), i);
                return copy;
            }
            case IList list:
            {
                IList copy;
                try
                {
                    // The default's own collection type where it can be built empty and added to, so a
                    // typed model is handed what it declared; anything else becomes a plain list, which
                    // the accessor converts on the way in.
                    copy = Activator.CreateInstance(list.GetType()) as IList is { IsFixedSize: false, IsReadOnly: false } same
                        ? same
                        : new List<object?>(list.Count);
                    foreach (var item in list) copy.Add(CopyDefault(item));
                }
                catch (Exception ex) when (ex is MissingMethodException or MemberAccessException or TargetInvocationException
                                               or ArgumentException or InvalidCastException or NotSupportedException)
                {
                    copy = new List<object?>(list.Count);
                    foreach (var item in list) copy.Add(CopyDefault(item));
                }
                return copy;
            }
            default:
                return value;
        }
    }

    /// <summary>
    /// Seeds a repeater row from its template's default values, so "quantity starts at 1" holds for
    /// every row the user adds and not only for the ones the form was created with.
    /// </summary>
    private void ApplyItemDefaults(BormFieldDefinition arrayField, string itemPath, bool overwrite)
    {
        if (arrayField.ItemTemplate is not { } template) return;

        if (template.Type == BormFieldType.Object)
        {
            foreach (var child in template.Children)
                ApplyDefault(child, BormPath.Combine(itemPath, child.Name), overwrite);
        }
        else
        {
            ApplyDefault(template, itemPath, overwrite);
        }
    }

    /// <summary>
    /// Walks the schema yielding every field paired with its absolute path, descending into objects and
    /// into the live elements of arrays.
    /// </summary>
    private IEnumerable<(BormFieldDefinition Field, string Path)> EnumerateFieldPaths()
    {
        foreach (var field in Definition.Fields)
            foreach (var pair in Descend(field, field.Name))
                yield return pair;

        IEnumerable<(BormFieldDefinition, string)> Descend(BormFieldDefinition field, string path)
        {
            yield return (field, path);

            if (field.Type == BormFieldType.Object)
            {
                foreach (var child in field.Children)
                    foreach (var pair in Descend(child, BormPath.Combine(path, child.Name)))
                        yield return pair;
            }
            else if (field.Type == BormFieldType.Array && field.ItemTemplate is { } template)
            {
                var count = ArrayCount(path);
                for (var i = 0; i < count; i++)
                {
                    var itemPath = BormPath.Combine(path, i);
                    if (template.Type == BormFieldType.Object)
                    {
                        foreach (var child in template.Children)
                            foreach (var pair in Descend(child, BormPath.Combine(itemPath, child.Name)))
                                yield return pair;
                    }
                    else
                    {
                        foreach (var pair in Descend(template, itemPath))
                            yield return pair;
                    }
                }
            }
        }
    }

    private IEnumerable<string> EnumerateValuePaths()
        => EnumerateFieldPaths()
            .Where(p => p.Field.Type != BormFieldType.Object && !p.Field.IsPresentational)
            .Select(p => p.Path);

    private IList EnsureList(string arrayPath)
    {
        if (Data.GetValue(arrayPath) is IList existing) return existing;

        IList newList;
        if (Data is BormDictionaryDataAccessor)
        {
            newList = new List<object?>();
        }
        else
        {
            var elementType = Data.GetElementType(arrayPath) ?? typeof(object);
            newList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
        }
        Data.SetValue(arrayPath, newList);
        // Re-read in case the accessor wrapped/converted the value.
        return Data.GetValue(arrayPath) as IList ?? newList;
    }

    private object? CreateItem(BormFieldDefinition arrayField, string arrayPath)
    {
        var template = arrayField.ItemTemplate;
        var elementType = Data.GetElementType(arrayPath);

        if (Data is BormDictionaryDataAccessor || elementType is null || elementType == typeof(object))
            return template?.Type == BormFieldType.Object ? BormDictionaryDataAccessor.NewObject() : null;

        if (elementType == typeof(string)) return null;
        var underlying = Nullable.GetUnderlyingType(elementType) ?? elementType;
        try { return underlying.IsValueType ? Activator.CreateInstance(underlying) : Activator.CreateInstance(elementType); }
        catch (Exception ex) when (ex is MissingMethodException or MemberAccessException or TargetInvocationException) { return null; }
    }

    /// <summary>Cancels any in-flight validation and detaches listeners.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _formValidationCts?.Cancel();
        _formValidationCts?.Dispose();
        _formValidationCts = null;

        foreach (var cts in _fieldValidationCts.Values.Concat(_optionsCts.Values))
        {
            cts.Cancel();
            cts.Dispose();
        }
        _fieldValidationCts.Clear();
        _optionsCts.Clear();
        _validatingFields.Clear();
        _focusTargets.Clear();

        StateChanged = null;
        FieldChanged = null;
        OptionsLoadFailed = null;
    }
}
