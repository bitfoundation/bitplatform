using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

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

    // Paths judged since their value last changed, so "valid" can be told apart from "never checked".
    private readonly HashSet<string> _validated = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, object?> _initialValues = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<BormSelectOption>> _loadedOptions = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _optionsInFlight = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CancellationTokenSource> _fieldValidationCts = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _validatingFields = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CancellationTokenSource> _optionsCts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _conversionErrors = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Exception> _optionsErrors = new(StringComparer.OrdinalIgnoreCase);

    // For a field that searches its options: the text the current results answer, and the option the
    // user picked — whose label the box keeps showing after a later search no longer returns it.
    private readonly Dictionary<string, string?> _searchText = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, BormSelectOption> _chosen = new(StringComparer.OrdinalIgnoreCase);

    // Which fields take part in each sweep a write runs, worked out once per schema. See BormFieldIndex.
    private BormFieldIndex _index;

    // ArrayCount as a delegate, made once rather than once per sweep.
    private readonly Func<string, int> _countRows;

    private bool HasClearOnHide => _index.ClearOnHide.Length > 0;
    private bool HasComputed => _index.Computed.Length > 0;
    private bool HasChangeHandlers => _index.HasChangeHandlers;
    private bool HasRevalidateOn => _index.Revalidating.Length > 0;
    private bool HasNormalizers => _index.Normalizing.Length > 0;
    private bool HasOptionDependencies => _index.CascadingOptions.Length > 0;

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
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Services = services;
        // Worked out once: these sweeps run after every value change, and most schemas use few of them.
        _index = new BormFieldIndex(definition);
        _countRows = ArrayCount;

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

    /// <summary>
    /// Optional service provider for validators that need DI, and where the registered message provider and schema
    /// localizer are found. A state built without one takes the services of the <see cref="BormView"/> it is shown in.
    /// </summary>
    public IServiceProvider? Services { get; private set; }

    /// <summary>
    /// Takes <paramref name="services"/> when the state was built without any - one a page made with
    /// <c>new BormState(definition, data)</c> - so the app's message provider, translations and schema localizer apply
    /// to it as they do to a state the view builds. A state given services of its own keeps them.
    /// </summary>
    internal void AdoptServices(IServiceProvider services)
    {
        if (Services is not null) return;
        Services = services;
        // Anything looked up before had nothing to look in.
        _messageProvider = null;
        _schemaLocalizer = null;
        _schemaLocalizerResolved = false;
    }

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
    /// Whether a field's asynchronous rules still run when one of its synchronous rules has already
    /// found an error. False by default: a username that does not match its pattern is not sent to the
    /// server to ask whether it is taken. See <see cref="BormValidator.RunAsyncRulesOnInvalidFields"/>.
    /// </summary>
    public bool RunAsyncRulesOnInvalidFields
    {
        get => _validator.RunAsyncRulesOnInvalidFields;
        set => _validator.RunAsyncRulesOnInvalidFields = value;
    }

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
           // Only a wizard gates its steps; tabs and sections are all there to be moved between.
           && (!Definition.IsWizard || index <= FurthestStepIndex)
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
    public void SetValue(string path, object? value)
    {
        using var step = Step(typing: true);
        Write(path, value, markTouched: true);
    }

    /// <summary>
    /// Sets a value without marking the field touched — for programmatic updates (computed values,
    /// prefill) that should not make the user's untouched field start showing errors.
    /// </summary>
    public void SetValueQuietly(string path, object? value)
    {
        using var step = Step(typing: true);
        Write(path, value, markTouched: false);
    }

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
        // One change, undone as one.
        using var step = Step();
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
        var rejected = IsUnreadableEntry(path, value);
        // Copied first: a store may refill a list in place, and the instance would then remember nothing.
        var before = Recording && !rejected ? ForHistory(Data.GetValue(path)) : null;
        if (!rejected) Data.SetValue(path, value);
        if (Recording && !rejected && !Data.LastWriteFailed) RecordValue(path, before, Data.GetValue(path));
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
        // Not while undoing either: what the handler did then is in the history and is put back with the rest.
        if (!HasChangeHandlers || !_initialised || _restoring || _replaying || _changeHandlerDepth >= maxDepth) return;
        if (FindField(path)?.OnChanged is not { } handler) return;

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
        if (!HasComputed || depth > maxCascade) return;
        if (changedPath is null)
        {
            RecomputeAll(maxCascade);
            return;
        }

        // Materialised before the loop: writing a computed value runs the same side-effect sweeps a
        // typed-in one does, and those mutate the data the walk is reading.
        foreach (var (field, path) in PathsOf(_index.Computed))
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
    /// Evaluates every computed field — when the form is created, or rebased onto new values — by
    /// passes over all of them in schema order, repeated while any value still moves.
    /// </summary>
    /// <remarks>
    /// Cascading from each value as it changed, the way a single edit does, walked every formula again
    /// per formula that moved: seeding a two-hundred-line invoice whose lines each compute a total
    /// walked the whole invoice two hundred times. A pass already reaches every formula, so a second
    /// pass is all a value computed late in the first one needs, and a set of formulas feeding each
    /// other in a cycle settles in at most <paramref name="maxPasses"/> of them. What a changed value
    /// sets off — clearing a field it hides, reloading choices it drives, a change handler — runs once
    /// per changed field, after the values have settled, so each of those sees the final figures.
    /// </remarks>
    private void RecomputeAll(int maxPasses)
    {
        HashSet<string>? changed = null;
        // Every value a formula moved, in the order it moved, and where in that list each formula reading its
        // own output stood when it last ran. Such a formula runs again only for something else it reads that
        // moved since - never for its own value, as a single edit never re-runs it for that either - or
        // "{Counter} + 1" was applied once per pass.
        List<string>? moves = null;
        Dictionary<string, int>? ranAt = null;
        for (var pass = 0; pass <= maxPasses; pass++)
        {
            var moved = false;
            foreach (var (field, path) in PathsOf(_index.Computed))
            {
                var scope = BormPath.Parent(path);
                if (ReadsPath(field.ComputedDependencies, path, scope))
                {
                    ranAt ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    if (ranAt.TryGetValue(path, out var from) && !MovedSince(from, field, path, scope)) continue;
                    ranAt[path] = moves?.Count ?? 0;
                }

                var next = field.Computed!(new BormComputedContext(Data, path, scope));
                if (SameValue(Data.GetValue(path), next)) continue;

                Data.SetValue(path, next);
                TrackDirty(path);
                (changed ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase)).Add(path);
                (moves ??= []).Add(path);
                moved = true;
            }
            if (!moved) break;
        }

        if (changed is null) return;
        foreach (var path in changed)
        {
            ClearHiddenValues(path);
            InvalidateDependentOptions(path);
            RunChangeHandler(path);
            FieldChanged?.Invoke(path);
        }

        bool MovedSince(int from, BormFieldDefinition field, string path, string scope)
        {
            for (var i = from; i < (moves?.Count ?? 0); i++)
            {
                if (!string.Equals(moves![i], path, StringComparison.OrdinalIgnoreCase) && ReadsPath(field.ComputedDependencies, moves[i], scope))
                    return true;
            }
            return false;
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
    /// Whether <paramref name="value"/> is text a numeric, month or week field cannot read. Only a field
    /// with no CLR type of its own (or, for a number, a numeric one) is judged: a typed model's accessor
    /// refuses such text already, and a field whose property is a string otherwise holds whatever it
    /// is given.
    /// </summary>
    private bool IsUnreadableEntry(string path, object? value)
    {
        if (value is not string { Length: > 0 } text) return false;
        if (FindField(path) is not { Type: BormFieldType.Integer or BormFieldType.Number or BormFieldType.Range
                or BormFieldType.Month or BormFieldType.Week } field)
            return false;

        // A month or a week held as text — no CLR type, or a string property — is the picker's own
        // form or nothing; a browser without a picker shows a plain text box, and anything typed into
        // it would otherwise be stored as though it were a period.
        if (field.Type is BormFieldType.Month or BormFieldType.Week)
            return (field.ValueType is null || field.ValueType == typeof(string)) && !BormPeriod.TryParse(text.Trim(), field.Type, out _);

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
        => new(path, Text(BormMessageKeys.Conversion, _conversionErrors[path]))
        {
            Code = BormMessageKeys.Conversion,
            Source = BormValidationSource.Conversion
        };

    /// <summary>
    /// Resolves a piece of the library's own text (see <see cref="BormMessageKeys"/>) through the
    /// registered <see cref="IBormMessageProvider"/>, falling back to the English defaults. The
    /// built-in components render everything through this, so a single provider translates the whole
    /// form — buttons, placeholders and repeater labels included, not only the validation messages.
    /// It answers in <see cref="Language"/> when the form has one and the provider can answer in a language of
    /// its choosing (<see cref="IBormLocalizedMessageProvider"/>, as <see cref="BormTranslatedMessageProvider"/> is).
    /// </summary>
    public string Text(string key, params object?[] args) => BormMessageText.Get(Messages, _language, key, args);

    // ---------------------------------------------------------------- schema text

    private IBormSchemaLocalizer? _schemaLocalizer;
    private bool _schemaLocalizerResolved;

    /// <summary>
    /// A piece of the schema's own text, translated: what the registered <see cref="IBormSchemaLocalizer"/>
    /// says for <paramref name="key"/>, else what the document's <see cref="BormDefinition.Translations"/>
    /// say for the current UI culture, else <paramref name="text"/> as written. The built-in components
    /// render every label, help text, placeholder, option and title through this; a custom renderer
    /// should too.
    /// </summary>
    public string? Localize(string key, string? text)
    {
        if (!_schemaLocalizerResolved)
        {
            _schemaLocalizer = Services?.GetService(typeof(IBormSchemaLocalizer)) as IBormSchemaLocalizer;
            _schemaLocalizerResolved = true;
        }

        if (_schemaLocalizer?.Localize(key, text) is { } localized) return localized;
        if (Definition.Translations.Count > 0)
        {
            if (_language is { } language)
            {
                // By name alone, so it needs no culture data: de-CH, then de.
                for (var name = language; ; name = name[..name.LastIndexOf('-')])
                {
                    if (Definition.Translations.TryGetValue(name, out var table) && table.TryGetValue(key, out var translated))
                        return translated;
                    if (name.LastIndexOf('-') <= 0) break;
                }
            }
            else
            {
                for (var culture = CultureInfo.CurrentUICulture; ; culture = culture.Parent)
                {
                    if (Definition.Translations.TryGetValue(culture.Name, out var table) && table.TryGetValue(key, out var translated))
                        return translated;
                    if (culture.Name.Length == 0) break;
                }
            }
        }
        return text;
    }

    private string? _language;

    /// <summary>
    /// The language this form's own text is read out in - a culture name, <c>de</c> or <c>pt-BR</c> - whatever
    /// the page's culture is. Null (the default) follows <see cref="CultureInfo.CurrentUICulture"/>.
    /// </summary>
    /// <remarks>
    /// For a form shown in a language other than the page's - a public form in the visitor's language - and for
    /// an application with no culture data to switch to: under <c>InvariantGlobalization</c>, common on
    /// WebAssembly, the UI culture is always the invariant one and no translation would ever apply. The
    /// document's <see cref="BormDefinition.Translations"/> are looked up by this name, then by the name with
    /// its last part dropped (<c>de-CH</c>, then <c>de</c>). <see cref="BormView"/> marks the form with it as its
    /// <c>lang</c>, so a screen reader reads it in that language. A registered <see cref="IBormSchemaLocalizer"/>
    /// is asked as before, and finds its culture its own way. The library's own words - validation messages,
    /// buttons, announcements - follow it when the message provider can answer in a language of its choosing:
    /// <c>services.AddBormTranslations()</c> registers one. A rule's message already on screen is rewritten in
    /// the new language when its field is next validated.
    /// </remarks>
    public string? Language
    {
        get => _language;
        set
        {
            var language = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            if (string.Equals(language, _language, StringComparison.OrdinalIgnoreCase)) return;
            _language = language;
            _validator.Language = language;
            // An entry the model refused is described by the library alone, so it can be said again in the new
            // language now; a rule's verdict is rewritten when its field is next validated.
            foreach (var path in _conversionErrors.Keys)
            {
                if (!_messages.TryGetValue(path, out var list)) continue;
                var next = new List<BormValidationMessage>(list.Count);
                foreach (var message in list) next.Add(message.Source == BormValidationSource.Conversion ? ConversionMessage(path) : message);
                _messages[path] = next;
            }
            NotifyChanged();
        }
    }

    /// <summary>The language the schema's text is read out in right now: <see cref="Language"/>, else the UI culture's.</summary>
    internal string TextLanguage => _language ?? CultureInfo.CurrentUICulture.Name;

    /// <summary>Whether anything could translate the schema's text — so a form that has nothing to translate pays nothing for it.</summary>
    private bool Translates
    {
        get
        {
            if (!_schemaLocalizerResolved) Localize(string.Empty, null);
            return _schemaLocalizer is not null || Definition.Translations.Count > 0;
        }
    }

    /// <summary>
    /// The key a field's texts are translated under: its <see cref="BormFieldDefinition.TextKey"/>, or its
    /// path with the rows taken out (<c>Lines[2].Product</c> is <c>Lines.Product</c>).
    /// </summary>
    public static string TextKeyOf(BormFieldDefinition field, string path)
    {
        if (field.TextKey is { Length: > 0 } key) return key;
        if (path.IndexOf('[') < 0) return path;

        var sb = new System.Text.StringBuilder(path.Length);
        for (var i = 0; i < path.Length; i++)
        {
            if (path[i] != '[') { sb.Append(path[i]); continue; }
            var close = path.IndexOf(']', i);
            if (close < 0) break;
            i = close;
        }
        return sb.ToString();
    }

    /// <summary>The field's label, translated, or null when it has none.</summary>
    public string? LabelOf(BormFieldDefinition field, string path) => SchemaText(field, path, ".label", field.Label);

    /// <summary>The field's help text, translated.</summary>
    public string? HelpOf(BormFieldDefinition field, string path) => SchemaText(field, path, ".help", field.HelpText);

    /// <summary>The field's placeholder, translated.</summary>
    public string? PlaceholderOf(BormFieldDefinition field, string path) => SchemaText(field, path, ".placeholder", field.Placeholder);

    /// <summary>The text before the field's input, translated.</summary>
    public string? PrefixOf(BormFieldDefinition field, string path) => SchemaText(field, path, ".prefix", field.Prefix);

    /// <summary>The text after the field's input, translated.</summary>
    public string? SuffixOf(BormFieldDefinition field, string path) => SchemaText(field, path, ".suffix", field.Suffix);

    /// <summary>A step's title, translated, falling back to its id.</summary>
    public string StepTitleOf(BormStep step) => Localize($"steps.{step.Id}.title", step.Title) ?? step.Id;

    /// <summary>A step's description, translated.</summary>
    public string? StepDescriptionOf(BormStep step) => Localize($"steps.{step.Id}.description", step.Description);

    /// <summary>The form's title, translated.</summary>
    public string? FormTitle => Localize("form.title", Definition.Title);

    /// <summary>The form's description, translated.</summary>
    public string? FormDescription => Localize("form.description", Definition.Description);

    private string? SchemaText(BormFieldDefinition field, string path, string suffix, string? text)
        => Translates ? Localize(TextKeyOf(field, path) + suffix, text) : text;

    // A field's own options with their labels translated, per culture: the options sit on the render path
    // of every choice control, and translating every label of every render would be the cost of a form
    // that had nothing to translate at all.
    private readonly Dictionary<BormFieldDefinition, (string Culture, IReadOnlyList<BormSelectOption> Options)> _localizedOptions = [];

    private IReadOnlyList<BormSelectOption> LocalizedOptions(BormFieldDefinition field, string path, IReadOnlyList<BormSelectOption> options)
    {
        if (options.Count == 0 || !Translates) return options;

        var culture = TextLanguage;
        if (_localizedOptions.TryGetValue(field, out var cached) && cached.Culture == culture) return cached.Options;

        var key = TextKeyOf(field, path) + ".options.";
        var localized = options
            .Select(o => Localize(key + o.Value, o.Label) is { } label && !ReferenceEquals(label, o.Label) ? o with { Label = label } : o)
            .ToList();
        _localizedOptions[field] = (culture, localized);
        return localized;
    }

    // Resolved once. A repeater asks for four labels per row per render, and a container lookup for
    // each of them is pure waste — the registration cannot change for the life of a form anyway.
    private IBormMessageProvider? _messageProvider;

    private IBormMessageProvider Messages
        => _messageProvider ??= BormMessageText.Provider(Services);

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
    /// <param name="path">The path whose value changed.</param>
    /// <param name="rowsFollowed">
    /// True when <paramref name="path"/> is a list whose rows were added, removed or reordered rather
    /// than replaced: the rows' own state has followed them (see <see cref="RemapArrayIndices"/>), and
    /// what they hold is as it was.
    /// </param>
    private void TrackDirty(string path, bool rowsFollowed = false)
    {
        if (_initialValues.TryGetValue(path, out var initial) && MatchesInitial(initial, Data.GetValue(path)))
            _dirty.Remove(path);
        else
            _dirty.Add(path);

        // Every change of value passes through here, and a value that moved is one no verdict has been
        // given on yet — nor has the object or list holding it, whose own rules may read it.
        ForgetValidated(path, beneath: !rowsFollowed);
    }

    private void ForgetValidated(string path, bool beneath = true)
    {
        if (_validated.Count == 0) return;
        if (beneath) RemovePathsUnder(_validated, path);
        else _validated.Remove(path);
        for (var parent = BormPath.Parent(path); parent.Length > 0; parent = BormPath.Parent(parent))
            _validated.Remove(parent);
    }

    /// <summary>Records <paramref name="path"/>, and every field beneath it, as judged by a pass that just finished.</summary>
    private void MarkValidated(string path)
    {
        _validated.Add(path);
        if (FindField(path) is not { IsContainer: true }) return;
        foreach (var (_, candidate) in EnumerateFieldPaths())
            if (BormPath.IsAtOrUnder(candidate, path)) _validated.Add(candidate);
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
        => ReadOnly || field.ReadOnly || (IsSubmitting && ReadOnlyWhileSubmitting)
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
            if (prefix.Length < path.Length && FindField(prefix) is { IsContainer: true } container
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
        if (!HasClearOnHide) return;

        // Materialised: clearing a hidden container changes the live array counts the walk reads.
        List<string>? cleared = null;
        foreach (var (field, path) in PathsOf(_index.ClearOnHide))
        {
            // The index only lists fields that have a condition.
            var condition = field.VisibleWhen!;
            if (!DependsOn(condition, changedPath, BormPath.Parent(path))) continue;
            if (condition.Evaluate(ScopeFor(path))) continue;
            if (Data.GetValue(path) is not { } hidden) continue;

            // Part of the change that hid it: undoing that change shows the field with its answer back.
            RecordValue(path, hidden, null);
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
        // because this sits on the render path and copying it per render would be pure waste — unless
        // its labels are to be translated, which happens once per culture.
        return LocalizedOptions(field, path, field.Options as IReadOnlyList<BormSelectOption> ?? field.Options.ToList());
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
        if (field.OptionsProvider is null || _loadedOptions.ContainsKey(path)) return;

        // A search has nothing to ask until enough has been typed — except, when the field opens holding
        // a value, for the one option behind it, so the box shows a label rather than a bare id.
        string? search = null;
        if (field.SearchMinLength is { } minLength)
        {
            var typed = _searchText.TryGetValue(path, out var text) ? text : null;
            var resolving = typed is null && Data.GetValue(path) is not null;
            // Nothing typed yet is an empty search, which a minimum of zero runs ("the top results").
            search = resolving ? null : typed ?? string.Empty;
            if (!resolving && search!.Length < minLength)
            {
                _loadedOptions[path] = Array.Empty<BormSelectOption>();
                OptionsVersion++;
                return;
            }
        }

        if (!_optionsInFlight.Add(path)) return;

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
            var ctx = new BormOptionsContext(field, path, ScopeFor(path), Services, cts.Token)
            {
                SearchText = search,
                CurrentValue = Data.GetValue(path)
            };
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

    /// <summary>
    /// Runs a field's search (see <see cref="BormFieldDefinition.SearchMinLength"/>) for
    /// <paramref name="text"/>, replacing the options it offers. A search still running for earlier text
    /// is cancelled, and its answer never lands on top of this one. The built-in combobox calls this as
    /// the user types; a custom control can too.
    /// </summary>
    public ValueTask SearchOptionsAsync(BormFieldDefinition field, string path, string? text)
    {
        ArgumentNullException.ThrowIfNull(field);
        if (field.SearchMinLength is null) return EnsureOptionsAsync(field, path);

        var term = string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
        if (_searchText.TryGetValue(path, out var current) && current == term && _loadedOptions.ContainsKey(path))
            return ValueTask.CompletedTask;

        _searchText[path] = term;
        CancelOptionsLoad(path);
        _loadedOptions.Remove(path);
        _optionsErrors.Remove(path);
        OptionsVersion++;
        return EnsureOptionsAsync(field, path);
    }

    /// <summary>The text a search field's current results answer, or null before anything was typed.</summary>
    public string? SearchText(string path) => _searchText.TryGetValue(path, out var text) ? text : null;

    /// <summary>Remembers the option the user picked from a search, so its label outlives the results it came from.</summary>
    internal void RememberChoice(string path, BormSelectOption option) => _chosen[path] = option;

    /// <summary>The option last picked for a search field, when its value is still the one held.</summary>
    internal BormSelectOption? ChosenOption(string path, string value)
        => _chosen.TryGetValue(path, out var option) && string.Equals(option.Value, value, StringComparison.Ordinal) ? option : null;

    /// <summary>
    /// Turns what a posted combobox sent - the label the user saw and typed, which is all a plain form can post - into
    /// the value it stands for, as the interactive control does on its way out of the box. Text that names no option
    /// is left as it is, for the field's rules to judge.
    /// </summary>
    internal async Task ResolvePostedChoicesAsync()
    {
        foreach (var (field, path) in EnumerateFieldPaths().ToList())
        {
            if (field.Type != BormFieldType.Combobox || Data.GetValue(path) is not string { Length: > 0 } text) continue;

            if (field.SearchMinLength is not null) await SearchOptionsAsync(field, path, text);
            else await EnsureOptionsAsync(field, path);

            var options = OptionsFor(field, path);
            if (options.Any(o => string.Equals(o.Value, text, StringComparison.Ordinal))) continue;
            if (options.FirstOrDefault(o => !o.Disabled && string.Equals(o.Label, text, StringComparison.OrdinalIgnoreCase)) is { } chosen)
            {
                RememberChoice(path, chosen);
                SetValueQuietly(path, chosen.Value);
            }
        }
    }

    // ---------------------------------------------------------------- uploads

    private readonly Dictionary<string, List<BormUpload>> _uploads = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The files chosen for the field at <paramref name="path"/> whose uploads are on their way, done or failed.</summary>
    public IReadOnlyList<BormUpload> UploadsFor(string path) => _uploads.TryGetValue(path, out var uploads) ? uploads : [];

    /// <summary>Whether any file is still on its way.</summary>
    public bool IsUploading => _uploads.Values.Any(uploads => uploads.Any(u => u.Status == BormUploadStatus.Uploading));

    /// <summary>
    /// Uploads files chosen for a field with an <see cref="BormFieldDefinition.Uploader"/>: judged by the field's
    /// file rules first - a file they refuse is not sent - then each on its way at once. A field taking one file
    /// replaces the one it had, which it no longer holds from then on, whether or not the new one arrives; one
    /// taking several adds to them, the files it held before included. A submit made meanwhile waits for them.
    /// </summary>
    public async Task UploadAsync(BormFieldDefinition field, string path, IReadOnlyList<Microsoft.AspNetCore.Components.Forms.IBrowserFile> files)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(files);
        if (field.Uploader is not { } uploader || files.Count == 0) return;

        var context = new BormValidationContext(path, field.Multiple ? files.ToList() : files[0], ScopeFor(path), Services, field)
        {
            Language = _language
        };
        foreach (var rule in field.Validators.OfType<BormFileRule>())
        {
            var verdict = await rule.ValidateAsync(context);
            if (verdict.IsValid || verdict.Message is null) continue;

            RemoveMessagesUnder(path);
            AddMessage(new BormValidationMessage(path, verdict.Message, verdict.Severity) { Code = rule.Key });
            _touched.Add(path);
            NotifyChanged();
            return;
        }

        if (!_uploads.TryGetValue(path, out var uploads)) _uploads[path] = uploads = [];
        if (!field.Multiple)
        {
            foreach (var replaced in uploads) replaced.Cancel();
            uploads.Clear();
            // The file being replaced goes now, with its row: kept until the new one arrived, it was still sent
            // when that one failed or was stopped, while the field showed only the file that had not made it.
            if (Data.GetValue(path) is not null) SetValue(path, null);
        }

        var started = new List<Task>(files.Count);
        foreach (var file in files)
        {
            var upload = new BormUpload(file);
            uploads.Add(upload);
            started.Add(RunUploadAsync(field, path, uploader, upload));
        }
        NotifyChanged();
        await TrackEntryAsync(Task.WhenAll(started));
    }

    /// <summary>Tries a failed upload again.</summary>
    public Task RetryUploadAsync(BormFieldDefinition field, string path, BormUpload upload)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(upload);
        if (field.Uploader is not { } uploader || upload.Status is BormUploadStatus.Uploading or BormUploadStatus.Done) return Task.CompletedTask;
        return TrackEntryAsync(RunUploadAsync(field, path, uploader, upload));
    }

    /// <summary>Stops an upload, or takes back a finished one, and the field no longer holds it.</summary>
    public void RemoveUpload(BormFieldDefinition field, string path, BormUpload upload)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(upload);
        upload.Cancel();
        if (_uploads.TryGetValue(path, out var uploads) && uploads.Remove(upload) && upload.Status == BormUploadStatus.Done)
            Unstore(field, path, upload.Result!);
        NotifyChanged();
    }

    /// <summary>Takes back a file the field held before anything was uploaded here - one of an edit form's saved files.</summary>
    public void RemoveStoredUpload(BormFieldDefinition field, string path, string result)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(result);
        Unstore(field, path, result);
        NotifyChanged();
    }

    /// <summary>
    /// What the field holds for files sent before - the uploader's results - that no upload chosen here
    /// produced: an edit form's saved files, which a field taking several keeps beside the new ones.
    /// </summary>
    public IReadOnlyList<string> StoredUploadsFor(string path)
    {
        var stored = StoredResults(path);
        foreach (var upload in UploadsFor(path))
            if (upload.Status == BormUploadStatus.Done) stored.Remove(upload.Result!);
        return stored;
    }

    /// <summary>Stops every upload of the field and forgets them - the field emptied.</summary>
    internal void ForgetUploads(string path)
    {
        if (!_uploads.Remove(path, out var uploads)) return;
        foreach (var upload in uploads) upload.Cancel();
    }

    private async Task RunUploadAsync(BormFieldDefinition field, string path, BormFileUploader uploader, BormUpload upload)
    {
        var token = upload.Start();
        try
        {
            var result = await uploader(new BormUploadContext(upload, this, field, path, token));
            token.ThrowIfCancellationRequested();
            upload.Finish(result);
            if (_uploads.TryGetValue(path, out var uploads) && uploads.Contains(upload)) Store(field, path, result);
            // Judged as an answer the user just gave: "a file is required" goes as the file arrives.
            if (IsSubmitted || IsTouched(path)) await ValidateFieldAsync(field, path, includeAsync: false);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            upload.Stopped();
            if (_uploads.TryGetValue(path, out var uploads)) uploads.Remove(upload);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !token.IsCancellationRequested)
        {
            // Whatever the uploader throws - a refused request, a dropped connection - is the file's to report,
            // not the form's to crash on: it says so beside the file, and can be tried again.
            upload.Fail(Text(BormMessageKeys.UploadFailed, upload.Name), ex);
        }
        NotifyChanged();
    }

    // A file that arrived is added to what the field holds, and one taken back is taken out of it - never the
    // value rebuilt from this page's uploads alone, which threw away every file the field held when it opened.
    private void Store(BormFieldDefinition field, string path, string result)
    {
        if (!field.Multiple)
        {
            SetValue(path, result);
            return;
        }
        var results = StoredResults(path);
        results.Add(result);
        SetValue(path, results);
    }

    private void Unstore(BormFieldDefinition field, string path, string result)
    {
        var results = StoredResults(path);
        if (!results.Remove(result)) return;
        SetValue(path, field.Multiple ? results : null);
    }

    private List<string> StoredResults(string path) => Data.GetValue(path) switch
    {
        string { Length: > 0 } one => [one],
        string => [],
        IEnumerable many => many.OfType<string>().Where(s => s.Length > 0).ToList(),
        _ => []
    };

    // Entries a control is still turning into a value: a search field's text, committed before its results
    // arrived. A submit made meanwhile judged, and sent, the value from before — the click lands while the
    // lookup that would have changed it is still out.
    private readonly List<Task> _pendingEntries = [];

    /// <summary>
    /// Tells the form an entry is still being turned into a value, so a submit or a wizard's Next made
    /// meanwhile waits for it rather than judging the value from before. The built-in search combobox does this
    /// for text committed before its results arrived; a control of your own whose commit awaits something - a
    /// lookup, a parse on the server - passes that work here and awaits what it returns.
    /// </summary>
    public async Task TrackEntryAsync(Task entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _pendingEntries.Add(entry);
        try
        {
            await entry;
        }
        finally
        {
            _pendingEntries.Remove(entry);
        }
    }

    /// <summary>
    /// Waits for every entry still being committed, including any made while waiting. A failure is not this
    /// caller's to report: the control that made the entry is awaiting it too.
    /// </summary>
    private async Task SettleEntriesAsync()
    {
        while (_pendingEntries.Count > 0)
        {
            await Task.WhenAll(_pendingEntries.ToArray())
                .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);
            _pendingEntries.RemoveAll(entry => entry.IsCompleted);
        }
    }

    /// <summary>
    /// Refuses an entry a field cannot take — a search field answered with text that names no result —
    /// the way an entry a number field cannot read is refused: the model keeps its last value, and the
    /// field says why.
    /// </summary>
    internal void RefuseEntry(string path, string text)
    {
        RecordConversionResult(path, text, failed: true);
        _touched.Add(path);
        NotifyChanged();
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
        if (!HasOptionDependencies) return;

        // Two passes: the walk only collects, so it never sees the data change underneath it, and a
        // form where nothing depends on this path — which is almost every write — allocates nothing.
        List<(string Path, bool KeepValue)>? targets = null;
        foreach (var (field, path) in PathsOf(_index.CascadingOptions))
        {
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
            // A search typed against the old dependency is not a search for the new one.
            _searchText.Remove(path);
            OptionsVersion++;
            // The previously selected option may no longer exist in the new list. This is judged on the
            // dependency, not on whether the choices happen to be cached: a value written before the
            // control ever rendered — a prefill, a restored draft — was still chosen for the old
            // dependency, and leaving it because no list had been fetched yet is how a city survives the
            // country it belongs to.
            // A draft being restored hands over the answer together with its dependency, and so does undo or
            // redo: the step being replayed holds the answer as it stood beside that dependency - the city
            // the clearing took away is the next thing undo puts back, and emptying it again here, with
            // nothing recorded, left it gone for good.
            if (keepValue || _restoring || _replaying || Data.GetValue(path) is not { } stale) continue;

            RecordValue(path, stale, null);
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
        return new BormFieldState(IsTouched(path), IsDirty(path), invalid, messages, IsValidatingField(path))
        {
            IsValidated = _validated.Contains(path)
        };
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
            // The validator says which fields it judged, from the paths it built anyway: walking the schema
            // again built every row's paths a second time, a third of what a full validation allocated.
            // A field a condition hides was not judged, and does not read as validated.
            var judged = new List<string>(Math.Max(_validated.Count, 16));
            var messages = await _validator.ValidateAsync(Definition, Data, Services, null, includeAsync, judged, token);
            var merged = await MergeExternal(messages);
            token.ThrowIfCancellationRequested();
            ReplaceAllMessages(DropHiddenStepMessages(merged));
            ReapplyConversionErrors();
            HasValidated = true;
            _validated.Clear();
            foreach (var path in judged) _validated.Add(path);
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
                if (FindField(path) is not { } field) continue;
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
                        step.Fields.Any(f => BormPath.IsAtOrUnder(m.FieldPath, f)) && !IsHidden(m.FieldPath, hidden)).Select(AsExternal));
                }
            }

            token.ThrowIfCancellationRequested();

            // Replace only messages for fields in this step, and mark them touched so the errors show.
            foreach (var path in step.Fields)
            {
                RemoveMessagesUnder(path);
                _touched.Add(path);
                MarkValidated(path);
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
        if (FindField(path) is not { } field) return false;

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
            // Only once every rule has had its say: a run that skipped the async ones (a keystroke)
            // has not judged a field that has any.
            if (includeAsync || !HasAsyncRules(field)) MarkValidated(path);
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
        if (!HasRevalidateOn || string.IsNullOrEmpty(changedPath)) return;

        // Materialised before the loop: validating writes messages, and a rule is free to read the
        // live arrays the walk is enumerating.
        List<(BormFieldDefinition Field, string Path)>? dependents = null;
        foreach (var (field, path) in PathsOf(_index.Revalidating))
        {
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
        if (field.Normalize is null && field.ParsedMask is null) return false;

        var current = Data.GetValue(path);
        var tidied = field.Normalize is { } normalize ? normalize(current) : current;
        // Then into the mask's shape. A value that does not fit is left as typed, for the rule to report
        // next to what the user wrote rather than next to a rewrite of it.
        if (field.ParsedMask is { } mask && tidied is string text)
        {
            if (text.Trim().Length == 0) tidied = null;
            else if (field.MaskStoresRaw ? mask.TryUnmask(text, out var raw) : mask.TryFormat(text, out raw)) tidied = raw;
        }
        if (Equals(current, tidied)) return false;

        // Tidying is part of the answer just given: undone with it, not as a change of its own.
        using (Step(tidying: true)) SetValueQuietly(path, tidied);
        return true;
    }

    /// <summary>
    /// Runs every declared normalizer across the form — including one per row of a repeater. Called
    /// before a submit validates, so a field the user never visited is still tidied.
    /// </summary>
    public bool NormalizeAll()
    {
        if (!HasNormalizers) return false;

        var changed = false;
        // Materialised: normalising writes values, and a write runs sweeps that mutate the live arrays
        // this walk is reading.
        foreach (var (field, path) in PathsOf(_index.Normalizing))
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
        // A row on a page of a paged list that is not showing has no control to focus until its page is.
        if (await RevealAsync(path) && _focusTargets.TryGetValue(path, out focus)) return await focus();
        if (string.IsNullOrEmpty(path) || _focusTargets.Count == 0) return false;

        foreach (var (_, candidate) in EnumerateFieldPaths().ToList())
        {
            if (candidate.Length <= path.Length || !BormPath.IsAtOrUnder(candidate, path)) continue;
            if (_focusTargets.TryGetValue(candidate, out var inner) && await inner()) return true;
        }
        return false;
    }

    private readonly Dictionary<string, Func<int, Task<bool>>> _rowRevealers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers how the list at <paramref name="arrayPath"/> brings one of its rows into view - a paged repeater
    /// turning to the row's page - so focusing a field in it works wherever the row is. The callback reports
    /// whether it had to show anything. Dispose the returned token to unregister.
    /// </summary>
    public IDisposable RegisterRowRevealer(string arrayPath, Func<int, Task<bool>> reveal)
    {
        _rowRevealers[arrayPath] = reveal;
        return new RevealerRegistration(this, arrayPath, reveal);
    }

    /// <summary>
    /// Brings <paramref name="path"/> into view in every paged list it runs through, outermost first: a list
    /// inside a row of another one only exists once the outer row's page is showing.
    /// </summary>
    private async Task<bool> RevealAsync(string path)
    {
        if (_rowRevealers.Count == 0 || string.IsNullOrEmpty(path)) return false;

        var revealed = false;
        for (var depth = 0; depth < 8; depth++)
        {
            var moved = false;
            foreach (var (arrayPath, reveal) in _rowRevealers.OrderBy(r => r.Key.Length).ToList())
            {
                if (!path.StartsWith(arrayPath + "[", StringComparison.OrdinalIgnoreCase)) continue;
                var close = path.IndexOf(']', arrayPath.Length);
                if (close < 0 || !int.TryParse(path.AsSpan(arrayPath.Length + 1, close - arrayPath.Length - 1), out var row)) continue;
                if (!await reveal(row)) continue;

                revealed = moved = true;
                // The list's own rows - and any list inside them - registered as that page rendered.
                break;
            }
            if (!moved) break;
        }
        return revealed;
    }

    private sealed class RevealerRegistration(BormState state, string arrayPath, Func<int, Task<bool>> reveal) : IDisposable
    {
        public void Dispose()
        {
            if (state._rowRevealers.TryGetValue(arrayPath, out var current) && ReferenceEquals(current, reveal))
                state._rowRevealers.Remove(arrayPath);
        }
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

    // How many errors SetServerError and SetServerErrors have recorded, ever: a submit compares it before and
    // after its handler to tell the errors the handler reported from those the user made while it ran.
    private int _serverErrorsRecorded;

    /// <summary>Adds a message for a path (used by server-side validation results).</summary>
    public void SetServerError(string path, string message)
    {
        AddMessage(new BormValidationMessage(path, message) { Source = BormValidationSource.Server });
        _serverErrorsRecorded++;
        _touched.Add(path);
        NotifyChanged();
    }

    /// <summary>
    /// Replaces the form's messages with those returned by a server round-trip, so a failed POST can
    /// surface its errors on the right fields in one call.
    /// </summary>
    /// <remarks>
    /// The messages stand until the field they are on is next validated — the user correcting it, or
    /// the next submit — at which point the field's own rules speak for it again; the server is asked
    /// again on that next submit. A message on a path the form does not show still blocks
    /// <see cref="IsValid"/>, and is listed by the error summary.
    /// </remarks>
    public void SetServerErrors(IEnumerable<BormValidationMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var recorded = messages.Select(m => m.Source == BormValidationSource.Rule ? m with { Source = BormValidationSource.Server } : m).ToList();
        ReplaceAllMessages(recorded);
        _serverErrorsRecorded += recorded.Count(m => m.Severity == BormValidationSeverity.Error);
        // A value the model could never accept is not something the server has an opinion about — it
        // never reached the server — so replacing the message set must not take its complaint with it,
        // leaving the box showing text the model does not hold and nothing at all to say why.
        ReapplyConversionErrors();
        foreach (var m in _messages.Keys) _touched.Add(m);
        NotifyChanged();
    }

    /// <summary>
    /// Replaces the form's messages with the errors of a failed request, in the shape ASP.NET Core's
    /// <c>ValidationProblemDetails.Errors</c> — and most other validation responses — use: each key a
    /// field, each value what is wrong with it. Pass <c>problem.Errors</c> as it arrives.
    /// </summary>
    /// <param name="errors">Field keys and their messages.</param>
    /// <param name="mapPath">
    /// Turns a key into the path of the field it belongs to, for an API whose names are not the form's
    /// — a prefix to drop, a property renamed on the way. Return null (or empty) to put the messages on
    /// the form as a whole. Without it, a key is taken as a path as it is, minus the <c>$.</c>
    /// System.Text.Json puts in front of the ones it reports; <c>""</c> and <c>$</c> mean the whole
    /// request. Paths match the form's without regard to case, so <c>lines[0].product</c> from a
    /// camel-cased API lands on <c>Lines[0].Product</c>.
    /// </param>
    public void SetServerErrors(IEnumerable<KeyValuePair<string, string[]>> errors, Func<string, string?>? mapPath = null)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var messages = new List<BormValidationMessage>();
        foreach (var (key, texts) in errors)
        {
            var path = mapPath is null ? DefaultServerPath(key) : mapPath(key) ?? string.Empty;
            foreach (var text in texts ?? [])
                if (!string.IsNullOrEmpty(text))
                    messages.Add(new BormValidationMessage(path, text) { Source = BormValidationSource.Server });
        }
        SetServerErrors(messages);

        static string DefaultServerPath(string key)
        {
            var path = key.Trim();
            if (path is "$") return string.Empty;
            if (path.StartsWith("$.", StringComparison.Ordinal)) path = path[2..];
            return path;
        }
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

    private CancellationTokenSource? _submitCts;

    /// <summary>
    /// How the last submit ended — <see cref="BormSubmitOutcome.None"/> before the first one and after
    /// <see cref="Reset()"/>. What a "Saved." banner, a "could not save, try again" message and a
    /// retry button all bind to, each to the case that is theirs.
    /// </summary>
    public BormSubmitOutcome SubmitOutcome { get; private set; }

    /// <summary>
    /// The exception the last submit's handler threw, when <see cref="SubmitOutcome"/> is
    /// <see cref="BormSubmitOutcome.Failed"/>; null otherwise.
    /// </summary>
    public Exception? SubmitError { get; private set; }

    /// <summary>
    /// When true, every field is read-only while the submit handler runs, so what the user types into
    /// the form during a slow save cannot leave the form showing values other than the ones saved.
    /// Off by default: a form that keeps working while it saves is what most people expect.
    /// </summary>
    public bool ReadOnlyWhileSubmitting
    {
        get => _readOnlyWhileSubmitting;
        set
        {
            if (_readOnlyWhileSubmitting == value) return;
            _readOnlyWhileSubmitting = value;
            NotifyChanged();
        }
    }

    private bool _readOnlyWhileSubmitting;

    /// <summary>
    /// Asks a submit in flight to stop: the token its handler was given is signalled. A handler that
    /// honours it ends the submit as <see cref="BormSubmitOutcome.Cancelled"/>; one that has already
    /// sent its request is free to finish, since whether a request that reached the server took effect
    /// is the server's to say. Does nothing when no submit is running.
    /// </summary>
    public void CancelSubmit() => _submitCts?.Cancel();

    /// <summary>
    /// The token of the submit in flight — what a handler wired through <c>BormView.OnValidSubmit</c>,
    /// which is handed only the state, passes on to its request. <see cref="CancellationToken.None"/>
    /// while no submit is running.
    /// </summary>
    public CancellationToken SubmitCancellationToken => _submitCts?.Token ?? CancellationToken.None;

    // The overload every call was written against before there was one taking a token, so it wins a call that
    // fits both - SubmitAsync(null, null). SubmitAsync(null, s => ...) fits only this one, on a compiler that does
    // not know the priority too: the other's onInvalid takes a token as well.
    /// <summary>
    /// Runs the full submit cycle: marks every field touched, validates, and invokes
    /// <paramref name="onValid"/> or <paramref name="onInvalid"/>. Concurrent calls are ignored while a
    /// submit is in flight, which is what stops a double-click from posting twice.
    /// </summary>
    /// <returns>
    /// Whether the submit got through: the form passed validation and the handler returned without
    /// throwing, being cancelled or recording errors of its own (see <see cref="SubmitOutcome"/>). A call
    /// ignored because a submit was already in flight returns false without validating anything or
    /// raising either callback, and leaves the messages as the earlier attempt left them; check
    /// <see cref="IsSubmitting"/> before calling to tell the two apart.
    /// </returns>
    [OverloadResolutionPriority(1)]
    public Task<bool> SubmitAsync(
        Func<BormState, Task>? onValid = null,
        Func<BormState, Task>? onInvalid = null)
        => SubmitAsync(onValid is null ? null : (state, _) => onValid(state), onInvalid is null ? null : (state, _) => onInvalid(state));

    // For a compiler that does not read the priority - C# 12, the default for .NET 8: SubmitAsync(null) fits this one
    // with no parameter left to its default, which settles it there too.
    /// <inheritdoc cref="SubmitAsync(Func{BormState, Task}, Func{BormState, Task})"/>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public Task<bool> SubmitAsync(Func<BormState, Task>? onValid) => SubmitAsync(onValid, null);

    /// <summary>
    /// Runs the full submit cycle, handing each handler a token that <see cref="CancelSubmit"/> and
    /// disposing the form signal — the one to pass on to the request the handler makes.
    /// </summary>
    /// <remarks>
    /// <para>The cycle, in order:</para>
    /// <list type="number">
    /// <item>Every normalizer runs, so the values judged and sent are the tidied ones.</item>
    /// <item>Every field is marked touched and the whole form is validated, async rules included.</item>
    /// <item>Invalid: <paramref name="onInvalid"/> runs, and the outcome is <see cref="BormSubmitOutcome.Invalid"/>.</item>
    /// <item>
    /// Valid: <paramref name="onValid"/> runs. If it records errors — <see cref="SetServerError"/> or
    /// <see cref="SetServerErrors(IEnumerable{BormValidationMessage})"/> with what a rejected request said —
    /// the outcome is <see cref="BormSubmitOutcome.Rejected"/>; if it throws, <see cref="BormSubmitOutcome.Failed"/>,
    /// with the exception in <see cref="SubmitError"/> and rethrown; if it stops because the token was
    /// cancelled, <see cref="BormSubmitOutcome.Cancelled"/>; otherwise <see cref="BormSubmitOutcome.Succeeded"/>.
    /// An error the user makes while it runs - text a number box cannot read, typed into a form that is not
    /// read-only while it saves - is the user's, not the handler's, and leaves a successful save successful.
    /// </item>
    /// </list>
    /// <para>
    /// Nothing about a failure is retried behind the application's back, and the user's values are left
    /// exactly as they were, so "try again" is simply submitting again.
    /// </para>
    /// </remarks>
    public async Task<bool> SubmitAsync(
        Func<BormState, CancellationToken, Task>? onValid,
        Func<BormState, CancellationToken, Task>? onInvalid = null)
    {
        if (IsSubmitting) return false;

        IsSubmitting = true;
        IsSubmitSuccessful = false;
        SubmitOutcome = BormSubmitOutcome.None;
        SubmitError = null;
        // An answer still being committed is part of what is submitted. Waited for while the form already says
        // it is submitting, so a second press is refused rather than racing the first.
        await SettleEntriesAsync();
        RegisterSubmitAttempt();
        // Tidy first, judge second. A field the user never visited never blurred, so this is the only
        // chance its normalizer gets — and a value that reaches the handler should be the one that
        // would have been stored.
        NormalizeAll();
        MarkAllTouched();
        NotifyChanged();

        var cts = _submitCts = new CancellationTokenSource();
        try
        {
            var valid = await ValidateAsync();
            if (!valid)
            {
                SubmitOutcome = BormSubmitOutcome.Invalid;
                if (onInvalid is not null) await onInvalid(this, cts.Token);
                return false;
            }

            var serverErrors = _serverErrorsRecorded;
            if (onValid is not null)
            {
                try
                {
                    await onValid(this, cts.Token);
                }
                catch (OperationCanceledException) when (cts.IsCancellationRequested)
                {
                    SubmitOutcome = BormSubmitOutcome.Cancelled;
                    return false;
                }
                catch (Exception ex)
                {
                    SubmitOutcome = BormSubmitOutcome.Failed;
                    SubmitError = ex;
                    throw;
                }
            }

            // A handler that posted the form and was told what was wrong with it put those errors on the
            // fields; that is a submit that did not go through, whatever the client-side rules thought. Only
            // what it recorded counts: the form's own errors now include whatever the user typed while it
            // ran, and a save that went through did not stop having gone through because of them.
            if (_serverErrorsRecorded != serverErrors)
            {
                SubmitOutcome = BormSubmitOutcome.Rejected;
                return false;
            }

            // Set only once the handler has returned: a save that threw did not succeed, and a
            // "Saved" banner bound to this must not appear over the top of the failure.
            SubmitOutcome = BormSubmitOutcome.Succeeded;
            IsSubmitSuccessful = true;
            return true;
        }
        finally
        {
            if (ReferenceEquals(_submitCts, cts)) _submitCts = null;
            cts.Dispose();
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
        // What there was to undo was the session just thrown away.
        _undo.Clear();
        _redo.Clear();
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
        _undo.Clear();
        _redo.Clear();
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
    /// <see cref="Snapshot()"/>, leaving out what <paramref name="options"/> asks — the values of fields
    /// the form is hiding, which a hidden branch keeps unless it is <c>ClearOnHide</c>, and sensitive
    /// values, which must not be written anywhere that outlives the page.
    /// </summary>
    /// <remarks>
    /// This is how a payload gets the semantics a schema-driven form needs: a hidden answer is
    /// <em>retained</em> while the user can still toggle back to it, <em>cleared</em> when the field
    /// is <c>ClearOnHide</c>, and <em>excluded</em> from what is sent when the snapshot says so.
    /// </remarks>
    public IReadOnlyDictionary<string, object?> Snapshot(BormSnapshotOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string>? hidden = null;
        if (options.ExcludeHidden)
        {
            hidden = HiddenFieldPaths();
            hidden.AddRange(HiddenStepFields());
        }

        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (field, path) in EnumerateFieldPaths())
        {
            if (field.Type == BormFieldType.Object || field.IsPresentational) continue;
            if (options.ExcludeSensitive && field.IsSensitive) continue;
            if (hidden is not null && IsHidden(path, hidden)) continue;
            values[path] = Data.GetValue(path);
        }
        return values;
    }

    // ---------------------------------------------------------------- drafts (see BormDraft)

    /// <summary>Every field of the schema with its live path, in schema order — what a draft walks.</summary>
    internal IEnumerable<(BormFieldDefinition Field, string Path)> FieldPaths() => EnumerateFieldPaths();

    /// <summary>Whether the paths a draft restore writes are being written right now.</summary>
    private bool _restoring;

    /// <summary>
    /// Runs a draft restore. Change handlers stay silent — the draft already holds whatever they would
    /// write, and running them would overwrite answers it restores — and a cascading select keeps the
    /// answer it is given rather than having it emptied by the dependency it arrived with.
    /// </summary>
    internal void Restoring(Action restore)
    {
        _restoring = true;
        try
        {
            Batch(restore);
            // The draft is where this session starts: there is nothing before it to go back to.
            _undo.Clear();
            _redo.Clear();
        }
        finally
        {
            _restoring = false;
        }
    }

    /// <summary>
    /// Whether every row <paramref name="path"/> runs through exists. Visibility is not asked about: a
    /// draft keeps the answers of a branch the user had hidden, exactly as the form itself does.
    /// </summary>
    internal bool IsLiveForRestore(string path)
    {
        var prefix = string.Empty;
        foreach (var segment in BormPath.Parse(path))
        {
            if (segment.IsIndex)
            {
                if (prefix.Length == 0 || segment.Index >= ArrayCount(prefix)) return false;
                prefix = BormPath.Combine(prefix, segment.Index);
            }
            else
            {
                prefix = BormPath.Combine(prefix, segment.Name!);
            }
        }
        return true;
    }

    /// <summary>
    /// Writes one restored value as an unsaved change, or reports that the field can no longer hold it —
    /// without leaving a complaint on the field about an entry the user never typed.
    /// </summary>
    internal bool TryRestoreValue(string path, object? value)
    {
        Write(path, value, markTouched: false);
        if (!_conversionErrors.Remove(path)) return true;
        RemoveMessagesFor(path);
        return false;
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
        _validated.Clear();
        _conversionErrors.Clear();

        // Cancel before clearing: a load still in flight would otherwise land in the cache after the
        // reset and repopulate a select the user has just cleared.
        foreach (var path in _optionsCts.Keys.ToList()) CancelOptionsLoad(path);
        _loadedOptions.Clear();
        _optionsErrors.Clear();
        _searchText.Clear();
        _chosen.Clear();
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
        SubmitOutcome = BormSubmitOutcome.None;
        SubmitError = null;
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

        // Putting one answer back is itself a change the user may want to take back.
        using var step = Step();
        var undoable = Recording
            ? _initialValues.Keys.Where(k => BormPath.IsAtOrUnder(k, path)).Append(path)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(key => (Key: key, Before: ForHistory(Data.GetValue(key))))
                .ToList()
            : null;
        try
        {
            ResetFieldCore(path);
        }
        finally
        {
            if (undoable is not null)
                foreach (var (key, before) in undoable) RecordValue(key, before, Data.GetValue(key));
        }
    }

    private void ResetFieldCore(string path)
    {
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
        ForgetValidated(path);
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

    /// <param name="onlyMissing">
    /// Baseline only the paths that have none yet — fields a changed schema has just added — so the
    /// ones that were already there keep measuring dirtiness from where the form opened.
    /// </param>
    private void CaptureInitialValues(bool onlyMissing = false)
    {
        foreach (var (field, path) in EnumerateFieldPaths())
        {
            if (field.IsPresentational || field.Type == BormFieldType.Object) continue;
            if (onlyMissing)
            {
                if (_initialValues.ContainsKey(path)) continue;
                // Its value is its baseline from here on, so whatever moved it into place — a default,
                // a computed value — is not a change the user made.
                _dirty.Remove(path);
            }

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
            if (!Definition.HasSteps) return null;
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
        if (!Definition.HasSteps) return;
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
        // Whatever the layout: a tab or section a condition takes away hides its fields as a step does.
        if (!Definition.HasSteps) return [];

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

    /// <summary>
    /// Advances to the next visible step — after validating the current one on a wizard; tabs and
    /// sections are moved between freely and validated together on submit.
    /// </summary>
    public async ValueTask<bool> NextStepAsync()
    {
        if (!Definition.HasSteps) return false;
        await SettleEntriesAsync();
        if (Definition.IsWizard && !await ValidateStepAsync()) return false;

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
        if (!Definition.HasSteps) return;
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
        if (!Definition.HasSteps || index < 0 || index >= Definition.Steps.Count) return;
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
        => WithRows(arrayPath, () => InsertItem(arrayField, arrayPath, index, notify: true));

    // `track`: whether to judge the list's dirtiness now. A caller adding many rows judges it once at the
    // end, since the judgement compares the whole list with its baseline and per row that is quadratic.
    private int InsertItem(BormFieldDefinition arrayField, string arrayPath, int index, bool notify, bool track = true)
    {
        var list = EnsureList(arrayPath);
        var target = Math.Clamp(index, 0, list.Count);
        var item = CreateItem(arrayField, arrayPath);

        if (!EditList(arrayPath, list, l => l.Insert(target, item))) return -1;

        // Appending moves no row, so there is nothing to re-key.
        if (target < list.Count - 1 || list.IsFixedSize) RemapArrayIndices(arrayPath, i => i >= target ? i + 1 : i);
        if (track) TrackDirty(arrayPath, rowsFollowed: true);
        // The row was created a line ago, so every value in it is a placeholder the default may replace.
        ApplyItemDefaults(arrayField, BormPath.Combine(arrayPath, target), overwrite: true);
        // Recompute, the ClearOnHide sweep, the cascading-options sweep and the list's own rules (the
        // collection-size rule may now pass) all run once, together, in NotifyArrayChanged below.
        if (notify) NotifyArrayChanged(arrayPath);
        return target;
    }

    /// <summary>
    /// Makes a list hold exactly <paramref name="count"/> rows — new ones appended with their defaults,
    /// surplus ones taken off the end — and settles the form once, however many rows that was. Adding
    /// them one at a time settles it once per row, and with a total computed over the list that is a walk
    /// of every row for every row added. Returns how many rows the list holds afterwards.
    /// </summary>
    internal int SetRowCount(BormFieldDefinition arrayField, string arrayPath, int count)
    {
        var current = ArrayCount(arrayPath);
        if (count == current) return current;

        if (count > current)
        {
            for (var i = current; i < count; i++)
                if (InsertItem(arrayField, arrayPath, int.MaxValue, notify: false, track: false) < 0) break;
        }
        else if (Data.GetValue(arrayPath) is IList list)
        {
            var keep = count;
            if (!EditList(arrayPath, list, l => { while (l.Count > keep) l.RemoveAt(l.Count - 1); })) return ArrayCount(arrayPath);
            RemapArrayIndices(arrayPath, i => i >= keep ? null : i);
        }

        TrackDirty(arrayPath, rowsFollowed: true);
        NotifyArrayChanged(arrayPath);
        return ArrayCount(arrayPath);
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
        if (FindField(arrayPath) is not { } field) return;

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
        => WithRows(arrayPath, () => DuplicateItem(arrayField, arrayPath, index));

    private int DuplicateItem(BormFieldDefinition arrayField, string arrayPath, int index)
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

        TrackDirty(arrayPath, rowsFollowed: true);
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
    public void RemoveArrayItem(string arrayPath, int index) => WithRows(arrayPath, () => RemoveItem(arrayPath, index));

    private void RemoveItem(string arrayPath, int index)
    {
        if (Data.GetValue(arrayPath) is not IList list || index < 0 || index >= list.Count) return;
        if (!EditList(arrayPath, list, l => l.RemoveAt(index))) return;

        // The removed row's state goes with it, and everything after the hole shifts up, taking its
        // own along — otherwise the item that moves into this slot inherits the removed item's errors,
        // touched flags and loaded choices.
        RemapArrayIndices(arrayPath, i => i == index ? null : i > index ? i - 1 : i);

        TrackDirty(arrayPath, rowsFollowed: true);
        // Recompute, the ClearOnHide sweep, the cascading-options sweep and the list's own rules all
        // run once, together, in NotifyArrayChanged below.
        NotifyArrayChanged(arrayPath);
    }

    /// <summary>Moves an array item from one index to another.</summary>
    public void MoveArrayItem(string arrayPath, int from, int to) => WithRows(arrayPath, () => MoveItem(arrayPath, from, to));

    private void MoveItem(string arrayPath, int from, int to)
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
        TrackDirty(arrayPath, rowsFollowed: true);
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
    public void SwapArrayItems(string arrayPath, int first, int second) => WithRows(arrayPath, () => SwapItems(arrayPath, first, second));

    private void SwapItems(string arrayPath, int first, int second)
    {
        if (Data.GetValue(arrayPath) is not IList list) return;
        if (first < 0 || first >= list.Count || second < 0 || second >= list.Count || first == second) return;

        var swapped = EditList(arrayPath, list, l =>
        {
            (l[first], l[second]) = (l[second], l[first]);
        });
        if (!swapped) return;

        RemapArrayIndices(arrayPath, index => index == first ? second : index == second ? first : index);
        TrackDirty(arrayPath, rowsFollowed: true);
        // Recompute, the ClearOnHide sweep and the cascading-options sweep all run once, together, in
        // NotifyArrayChanged below. A swap is a reorder: the list's own rules have nothing new to say.
        NotifyArrayChanged(arrayPath, revalidate: false);
    }

    /// <summary>
    /// Empties an array field, discarding every row's messages and touched/dirty state along with it.
    /// Removing rows one at a time costs a re-render and a re-index per row, and the caller has to walk
    /// backwards to avoid the indices moving underneath them.
    /// </summary>
    public void ClearArrayItems(string arrayPath) => WithRows(arrayPath, () => ClearItems(arrayPath));

    private void ClearItems(string arrayPath)
    {
        if (Data.GetValue(arrayPath) is not IList list || list.Count == 0) return;
        // A collection the model will not let go of keeps its rows rather than the form throwing.
        if (!EditList(arrayPath, list, l => l.Clear())) return;

        RemapArrayIndices(arrayPath, _ => null);
        TrackDirty(arrayPath, rowsFollowed: true);
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

    // ---------------------------------------------------------------- history

    // Undo and redo. A step is everything one change made: the answer the user gave, and what that answer
    // set off - a field a condition then hid and cleared, a sibling a change handler filled in, a city an
    // abandoned country took with it. Computed values are not recorded: they follow from the answers, so
    // putting the answers back puts them back too.
    private abstract record HistoryEntry(string Path);

    // One value, before and after. A collection is recorded as a copy of its elements: a multi-select's list
    // is refilled in place, and the instance itself would say nothing about what it used to hold.
    private sealed record ValueEntry(string Path, object? Before, object? After) : HistoryEntry(Path);

    // A list's rows, before and after a structural edit - an insert, a removal, a move, a swap, a copy, a
    // clear - as the row objects themselves. Restoring a sequence of rows undoes any of them alike, and each
    // row's messages and touched state follow it by identity.
    private sealed record RowsEntry(string Path, object?[]? Before, object?[]? After) : HistoryEntry(Path);

    private sealed class HistoryStep
    {
        public List<HistoryEntry> Entries { get; } = [];

        public long At { get; set; }

        public bool Tidying { get; init; }

        // Typing into a box, rather than an action of its own - a reset, a row added, several values at once.
        public bool Typing { get; init; }
    }

    private readonly List<HistoryStep> _undo = [];
    private readonly List<HistoryStep> _redo = [];
    private HistoryStep? _openStep;
    private int _stepDepth;
    private bool _replaying;
    private int _historyLimit;

    // Two writes to the same field this close together are one change: a field that writes as the user
    // types would otherwise need one undo per character.
    private const long CoalesceMilliseconds = 1000;

    /// <summary>
    /// How many changes <see cref="UndoAsync"/> can take back. Zero, the default, keeps no history at all;
    /// <see cref="BormView"/>'s <c>ShowUndo</c> turns it on.
    /// </summary>
    /// <remarks>
    /// A change is everything one answer set off: the value, a field a condition then hid and cleared, a
    /// sibling a change handler filled in, a row added, removed, moved or copied. Two writes to the same
    /// field within a second are one change, and so is the tidying a field does when the user leaves it.
    /// What changes the model behind the form's back is not recorded, so undo is only as good as the
    /// discipline of making changes through the state. <see cref="Reset()"/> and restoring a draft start the
    /// history again.
    /// </remarks>
    public int HistoryLimit
    {
        get => _historyLimit;
        set
        {
            _historyLimit = Math.Max(0, value);
            if (_historyLimit == 0)
            {
                ClearHistory();
                return;
            }
            if (_undo.Count > _historyLimit) _undo.RemoveRange(0, _undo.Count - _historyLimit);
            if (_redo.Count > _historyLimit) _redo.RemoveRange(0, _redo.Count - _historyLimit);
        }
    }

    /// <summary>Whether there is a change to take back.</summary>
    public bool CanUndo => _undo.Count > 0 && !IsSubmitting;

    /// <summary>Whether there is a change taken back that can be made again.</summary>
    public bool CanRedo => _redo.Count > 0 && !IsSubmitting;

    /// <summary>The field the next <see cref="UndoAsync"/> takes back the change to, for a button or an announcement to name.</summary>
    public string? NextUndoPath => _undo.Count > 0 ? _undo[^1].Entries[0].Path : null;

    /// <summary>The field the next <see cref="RedoAsync"/> makes the change to again.</summary>
    public string? NextRedoPath => _redo.Count > 0 ? _redo[^1].Entries[0].Path : null;

    /// <summary>Forgets every change, so there is nothing to undo or redo.</summary>
    public void ClearHistory()
    {
        if (_undo.Count == 0 && _redo.Count == 0) return;
        _undo.Clear();
        _redo.Clear();
        NotifyChanged();
    }

    /// <summary>
    /// Takes back the last change - everything it set off with it - and judges again what it touched. True
    /// when there was one to take back.
    /// </summary>
    public Task<bool> UndoAsync() => ReplayAsync(_undo, _redo, undo: true);

    /// <summary>Makes the last change taken back again. True when there was one.</summary>
    public Task<bool> RedoAsync() => ReplayAsync(_redo, _undo, undo: false);

    private async Task<bool> ReplayAsync(List<HistoryStep> from, List<HistoryStep> to, bool undo)
    {
        if (from.Count == 0 || IsSubmitting || _stepDepth > 0) return false;

        var step = from[^1];
        from.RemoveAt(from.Count - 1);
        to.Add(step);

        _replaying = true;
        try
        {
            Batch(() =>
            {
                if (undo)
                {
                    for (var i = step.Entries.Count - 1; i >= 0; i--) Apply(step.Entries[i], before: true);
                }
                else
                {
                    foreach (var entry in step.Entries) Apply(entry, before: false);
                }
            });
        }
        finally
        {
            _replaying = false;
        }

        // Judged again as a change would be: what the user has already been told about, the field put back
        // owes an answer to.
        foreach (var path in step.Entries.Select(e => e.Path).Distinct(StringComparer.OrdinalIgnoreCase).ToList())
        {
            if (!IsSubmitted && !IsTouched(path) && MessagesFor(path).Count == 0) continue;
            if (FindField(path) is { } field) await ValidateFieldAsync(field, path, includeAsync: false);
        }
        NotifyChanged();
        return true;
    }

    private void Apply(HistoryEntry entry, bool before)
    {
        switch (entry)
        {
            case ValueEntry value:
                var recorded = before ? value.Before : value.After;
                Write(value.Path, recorded is List<object?> items ? new List<object?>(items) : recorded, markTouched: false);
                break;
            case RowsEntry rows:
                RestoreRows(rows.Path, before ? rows.Before : rows.After);
                break;
        }
    }

    /// <summary>Puts a list back to <paramref name="rows"/>, each row's state following it by identity.</summary>
    private void RestoreRows(string arrayPath, object?[]? rows)
    {
        var current = Data.GetValue(arrayPath) as IList;
        var old = current?.Cast<object?>().ToArray() ?? [];

        if (rows is null)
        {
            if (current is null) return;
            RemapArrayIndices(arrayPath, _ => null);
            Data.SetValue(arrayPath, null);
        }
        else
        {
            var list = current ?? EnsureList(arrayPath);
            if (!EditList(arrayPath, list, l =>
                {
                    l.Clear();
                    foreach (var row in rows) l.Add(row);
                }))
                return;

            var target = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
            for (var i = 0; i < rows.Length; i++)
                if (rows[i] is { } row && IsRowObject(row)) target.TryAdd(row, i);
            RemapArrayIndices(arrayPath, i => i < old.Length && old[i] is { } row && target.TryGetValue(row, out var j) ? j : null);
        }

        TrackDirty(arrayPath, rowsFollowed: true);
        NotifyArrayChanged(arrayPath);

        // A value in a list of plain values is not a row with an identity of its own: two equal strings are
        // the same answer, and which one a message belonged to cannot be told.
        static bool IsRowObject(object row) => row is not string && !row.GetType().IsValueType;
    }

    /// <summary>
    /// Opens a history step for one change made through the state's API, or joins the one already open - a
    /// change handler writing during the change that set it off belongs to that change.
    /// </summary>
    private HistoryScope Step(bool tidying = false, bool typing = false) => new(this, tidying, typing);

    private readonly struct HistoryScope : IDisposable
    {
        private readonly BormState? _state;

        public HistoryScope(BormState state, bool tidying, bool typing)
        {
            if (state._historyLimit == 0 || state._replaying || state._restoring || !state._initialised) return;
            _state = state;
            if (state._stepDepth++ == 0) state._openStep = new HistoryStep { Tidying = tidying, Typing = typing || tidying };
        }

        public void Dispose()
        {
            if (_state is null || --_state._stepDepth > 0) return;

            var step = _state._openStep!;
            _state._openStep = null;
            _state.Commit(step);
        }
    }

    private void Commit(HistoryStep step)
    {
        if (step.Entries.Count == 0) return;
        step.At = Environment.TickCount64;

        // One field written twice in quick succession, or tidied as the user left it, is one change.
        if (_undo.Count > 0 && step.Typing && step.Entries is [ValueEntry next] && _undo[^1] is { Typing: true, Entries: [ValueEntry last] } previous
            && string.Equals(last.Path, next.Path, StringComparison.OrdinalIgnoreCase)
            && (step.Tidying || step.At - previous.At < CoalesceMilliseconds))
        {
            previous.Entries[0] = last with { After = next.After };
            previous.At = step.At;
            // Typed and typed back: there is nothing left to undo.
            if (SameRecorded(last.Before, next.After)) _undo.RemoveAt(_undo.Count - 1);
            _redo.Clear();
            return;
        }

        _undo.Add(step);
        if (_undo.Count > _historyLimit) _undo.RemoveAt(0);
        _redo.Clear();
    }

    private static bool SameRecorded(object? a, object? b)
        => a is List<object?> x && b is List<object?> y ? x.SequenceEqual(y) : SameValue(a, b);

    private bool Recording => _openStep is not null;

    private void RecordValue(string path, object? before, object? after)
    {
        if (_openStep is not { } step) return;
        var recordedBefore = ForHistory(before);
        var recordedAfter = ForHistory(after);
        if (SameRecorded(recordedBefore, recordedAfter)) return;
        step.Entries.Add(new ValueEntry(path, recordedBefore, recordedAfter));
    }

    private static object? ForHistory(object? value) => value is IList list and not Array ? list.Cast<object?>().ToList() : value;

    /// <summary>
    /// Runs a structural edit of the list at <paramref name="arrayPath"/> as one change, recording its rows
    /// before and after. They are recorded where the change began, ahead of what the edit's sweeps then did -
    /// a value a condition cleared once the row went - so taking the change back runs it in reverse.
    /// </summary>
    private T WithRows<T>(string arrayPath, Func<T> edit)
    {
        using var step = Step();
        var rows = Recording && Data.GetValue(arrayPath) is IList list ? list.Cast<object?>().ToArray() : null;
        var at = _openStep?.Entries.Count ?? 0;
        try
        {
            return edit();
        }
        finally
        {
            RecordRows(arrayPath, rows, at);
        }
    }

    private void WithRows(string arrayPath, Action edit) => WithRows(arrayPath, () =>
    {
        edit();
        return 0;
    });

    private void RecordRows(string arrayPath, object?[]? before, int at)
    {
        if (_openStep is not { } step) return;
        var after = Data.GetValue(arrayPath) is IList list ? list.Cast<object?>().ToArray() : null;
        if ((before?.Length ?? 0) == (after?.Length ?? 0)
            && (before ?? []).SequenceEqual(after ?? [], ReferenceEqualityComparer.Instance)) return;
        step.Entries.Insert(Math.Min(at, step.Entries.Count), new RowsEntry(arrayPath, before, after));
    }

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
            ? messages.Concat(external.Select(AsExternal)).ToList()
            : messages.Concat(external.Where(m => !IsHidden(m.FieldPath, hidden)).Select(AsExternal)).ToList();
    }

    /// <summary>Marks a message from an external validator as such, unless it already says where it came from.</summary>
    private static BormValidationMessage AsExternal(BormValidationMessage message)
        => message.Source == BormValidationSource.Rule ? message with { Source = BormValidationSource.External } : message;

    /// <summary>
    /// Every field path the schema is currently hiding. An external validator — a FluentValidation
    /// validator, say — sees the whole model and knows nothing about the form's conditions, so a rule
    /// on a branch the user was never shown would otherwise block submission with an error on a control
    /// that is not on the page.
    /// </summary>
    private List<string> HiddenFieldPaths()
    {
        var hidden = new List<string>();
        if (_index.Conditional.Length == 0) return hidden;
        foreach (var (field, fieldPath) in PathsOf(_index.Conditional))
        {
            // Already inside a hidden branch: its own condition cannot make it visible again, and
            // testing it would be one condition evaluation per descendant of every hidden container.
            if (IsHidden(fieldPath, hidden)) continue;
            if (!field.VisibleWhen!.Evaluate(ScopeFor(fieldPath))) hidden.Add(fieldPath);
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
        RemapKeys(_validated, arrayPath, map);
        RemapKeys(_conversionErrors, arrayPath, map);
        RemapKeys(_loadedOptions, arrayPath, map);
        RemapKeys(_optionsErrors, arrayPath, map);
        RemapKeys(_searchText, arrayPath, map);
        RemapKeys(_chosen, arrayPath, map);
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
        // One iterator and one stack for the whole walk, pushed in reverse so they pop in schema order.
        // An iterator nested per field allocated one per field — a quarter of a megabyte on every full
        // validation of a two-thousand-field form, only to list paths. A list's rows are still counted
        // when the walk reaches the list, after the consumer has seen everything before it.
        var pending = new Stack<(BormFieldDefinition, string)>();
        var fields = Definition.Fields;
        for (var f = 0; f < fields.Count; f++)
        {
            pending.Push((fields[f], fields[f].Name));
            while (pending.Count > 0)
            {
                var (field, path) = pending.Pop();
                yield return (field, path);

                if (field.Type == BormFieldType.Object)
                {
                    PushChildren(pending, field, path);
                }
                else if (field.Type == BormFieldType.Array && field.ItemTemplate is { } template)
                {
                    for (var i = ArrayCount(path) - 1; i >= 0; i--)
                    {
                        var itemPath = BormPath.Combine(path, i);
                        if (template.Type == BormFieldType.Object) PushChildren(pending, template, itemPath);
                        else pending.Push((template, itemPath));
                    }
                }
            }
        }

        static void PushChildren(Stack<(BormFieldDefinition, string)> pending, BormFieldDefinition owner, string ownerPath)
        {
            for (var c = owner.Children.Count - 1; c >= 0; c--)
                pending.Push((owner.Children[c], BormPath.Combine(ownerPath, owner.Children[c].Name)));
        }
    }

    /// <summary>
    /// The live paths of the given fields — every row of a list each sits in — materialised, since the
    /// sweeps that ask write values that change the very row counts the expansion reads.
    /// </summary>
    private List<(BormFieldDefinition Field, string Path)> PathsOf(BormFieldTemplate[] templates)
    {
        var paths = new List<(BormFieldDefinition, string)>(templates.Length);
        foreach (var template in templates) template.Expand(_countRows, paths);
        return paths;
    }

    /// <summary>
    /// The definition a path resolves to — <see cref="BormDefinition.FindByPath"/>, remembered per
    /// template so a lookup on the write path is a dictionary read rather than a walk of the schema.
    /// </summary>
    internal BormFieldDefinition? FindField(string path) => _index.Find(path);

    /// <summary>
    /// How a view rendering this form moves to a step — with focus following it — when something inside
    /// it asks to. Set by <see cref="BormView"/> while it shows the form.
    /// </summary>
    internal Action<int>? StepNavigator { get; set; }

    /// <summary>The Blazor edit context provided for this form, when one is — by a view, or by hand.</summary>
    internal BormEditContextAdapter? EditContextAdapter { get; set; }

    /// <summary>
    /// What the controls post their values under when a view renders the form to be posted (its <c>FormName</c>): the
    /// name of the page's <c>[SupplyParameterFromForm]</c> model, or empty to post the bare paths. Null while nothing posts.
    /// </summary>
    internal string? PostedName { get; set; }

    /// <summary>Whether a view renders the form to be posted with no interactivity, where nothing can add a row or turn a page.</summary>
    internal bool RendersStatically { get; set; }

    /// <summary>Moves on every time <see cref="NotifyDefinitionChanged"/> runs, for views that cache what they derived from the schema.</summary>
    internal int DefinitionVersion { get; private set; }

    /// <summary>
    /// Tells the state that its <see cref="Definition"/> was changed in place — a field added, removed
    /// or reconfigured, a rule or condition swapped, a step inserted — so it can catch up without being
    /// rebuilt and losing what the user has entered.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The state works out once which fields take part in each of the sweeps a write runs — clearing
    /// hidden values, recomputing, reloading cascading choices, revalidating dependents — because those
    /// run on every keystroke. A schema edited under it is otherwise only half seen: a computed field
    /// added to a form that had none was never computed, and a <c>ClearOnHide</c> added later never
    /// cleared anything.
    /// </para>
    /// <para>
    /// Catching up means: new fields get their defaults and their baselines (so they are not dirty),
    /// computed values are evaluated again, and everything recorded against a path the schema no
    /// longer has — messages, touched and dirty flags, rejected entries, loaded choices, work in
    /// flight — is dropped, so a removed field cannot keep blocking a submit from beyond the page.
    /// The values the user entered stay; a removed field's value is left in the model, where the
    /// application can still read it. Replacing the whole <see cref="Definition"/> object is a
    /// different matter: build a new state for it (<c>BormView</c> does that by itself).
    /// </para>
    /// </remarks>
    public void NotifyDefinitionChanged()
    {
        _index = new BormFieldIndex(Definition);
        _localizedOptions.Clear();
        DefinitionVersion++;

        ApplyDefaults();
        SeedRequiredArrayItems();
        RecomputeValues(changedPath: null);
        CaptureInitialValues(onlyMissing: true);

        bool Gone(string path) => path.Length > 0 && FindField(path) is null;
        foreach (var path in _messages.Keys.Where(Gone).ToList()) _messages.Remove(path);
        _touched.RemoveWhere(Gone);
        _dirty.RemoveWhere(Gone);
        _validated.RemoveWhere(Gone);
        foreach (var path in _initialValues.Keys.Where(Gone).ToList()) _initialValues.Remove(path);
        foreach (var path in _conversionErrors.Keys.Where(Gone).ToList()) _conversionErrors.Remove(path);
        foreach (var path in _loadedOptions.Keys.Where(Gone).ToList()) _loadedOptions.Remove(path);
        foreach (var path in _optionsErrors.Keys.Where(Gone).ToList()) _optionsErrors.Remove(path);
        foreach (var path in _searchText.Keys.Where(Gone).ToList()) _searchText.Remove(path);
        foreach (var path in _chosen.Keys.Where(Gone).ToList()) _chosen.Remove(path);
        foreach (var path in _optionsCts.Keys.Where(Gone).ToList()) CancelOptionsLoad(path);
        foreach (var path in _fieldValidationCts.Keys.Where(Gone).ToList())
        {
            _fieldValidationCts.Remove(path, out var cts);
            cts!.Cancel();
            cts.Dispose();
            _validatingFields.Remove(path);
        }
        OptionsVersion++;

        ClampStep();
        NotifyChanged();
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

        // A save still running for a form that is gone is told so; the submit disposes its own source.
        _submitCts?.Cancel();

        // So is a file still on its way: nobody is left to see where it lands.
        foreach (var uploads in _uploads.Values)
            foreach (var upload in uploads) upload.Cancel();

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
