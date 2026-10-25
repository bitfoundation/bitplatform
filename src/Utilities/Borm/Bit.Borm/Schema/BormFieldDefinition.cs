namespace Bit.Borm;

/// <summary>
/// Describes a single field in a form: what it is, how it should be labelled and constrained,
/// when it is visible, and how it is validated. This is the central, UI-agnostic unit of a schema.
/// </summary>
public sealed class BormFieldDefinition
{
    public BormFieldDefinition(string name, BormFieldType type)
    {
        Name = name;
        Type = type;
    }

    /// <summary>The field key relative to its parent (not the full path). Required and unique among siblings.</summary>
    public string Name { get; set; }

    /// <summary>The logical field type.</summary>
    public BormFieldType Type { get; set; }

    /// <summary>The CLR type of the value, when known (drives parsing and array item creation).</summary>
    public Type? ValueType { get; set; }

    /// <summary>Human-readable label.</summary>
    public string? Label { get; set; }

    /// <summary>
    /// The key the field's texts are translated under (<c>{key}.label</c>, <c>{key}.help</c>, …; see
    /// <see cref="IBormSchemaLocalizer"/>). Null uses the field's path with the rows taken out —
    /// set it when two forms should share a translation, or when the path is not a key a translator
    /// should have to read. <c>x-i18n</c> in JSON Schema.
    /// </summary>
    public string? TextKey { get; set; }

    /// <summary>Placeholder text for empty inputs.</summary>
    public string? Placeholder { get; set; }

    /// <summary>Help/hint text shown beneath the field.</summary>
    public string? HelpText { get; set; }

    /// <summary>
    /// Whether the field's label is rendered. Set false for a control that is already labelled by its
    /// surroundings — a search box under a heading that says "Search" — so the label is not announced
    /// twice. The label still exists for assistive technology via <c>aria-label</c>.
    /// </summary>
    public bool ShowLabel { get; set; } = true;

    /// <summary>
    /// Static text rendered inside the control, before the input — a currency symbol, a protocol, an
    /// <c>@</c>. Presentational only: it is never part of the value.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>Static text rendered after the input — a unit such as <c>kg</c> or <c>%</c>.</summary>
    public string? Suffix { get; set; }

    /// <summary>
    /// Shows a "42 / 200" counter beneath a field that has a <see cref="MaxLength"/>, so the user can
    /// see the limit approaching instead of discovering it when typing stops working.
    /// </summary>
    public bool ShowCharacterCount { get; set; }

    /// <summary>Whether the field must have a value. Also added as a validation rule when built.</summary>
    public bool Required { get; set; }

    /// <summary>Whether the field is read-only. Read-only fields render as <c>readonly</c>, not <c>disabled</c>.</summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Whether the value is something that must not outlive the page — a card number, a national id,
    /// a one-time code. A sensitive field is left out of drafts (<see cref="BormDraft"/>) and of a
    /// snapshot that asks to exclude it, so an autosave to browser storage cannot keep it. A password
    /// is sensitive whatever this says; see <see cref="IsSensitive"/>.
    /// </summary>
    public bool Sensitive { get; set; }

    /// <summary>Whether the value is treated as sensitive: marked <see cref="Sensitive"/>, or a password.</summary>
    public bool IsSensitive => Sensitive || Type == BormFieldType.Password;

    /// <summary>Default value applied when the form initialises and the value is missing.</summary>
    public object? DefaultValue { get; set; }

    /// <summary>Sort order within the parent/group (ascending).</summary>
    public int Order { get; set; }

    /// <summary>Optional visual group name to cluster related fields.</summary>
    public string? Group { get; set; }

    /// <summary>Options for select/radio/multiselect fields.</summary>
    public IList<BormSelectOption> Options { get; set; } = new List<BormSelectOption>();

    /// <summary>
    /// Loads <see cref="Options"/> on demand, for choices that come from a service or that depend on
    /// another field's value (cascading selects). Re-runs whenever a path in
    /// <see cref="OptionsDependencies"/> changes.
    /// </summary>
    public BormOptionsProvider? OptionsProvider { get; set; }

    /// <summary>Field paths whose values <see cref="OptionsProvider"/> reads, so options reload when they change.</summary>
    public IList<string> OptionsDependencies { get; set; } = new List<string>();

    /// <summary>
    /// When set, <see cref="OptionsProvider"/> is a search rather than a list: it is asked again as the
    /// user types into the combobox, with <see cref="BormOptionsContext.SearchText"/> holding what they
    /// typed, once it is at least this many characters long — the customer lookup over a hundred
    /// thousand records that no dropdown can hold. Null for a provider that loads the whole list.
    /// </summary>
    public int? SearchMinLength { get; set; }

    /// <summary>How long the user has to pause before a search runs, in milliseconds. Only meaningful with <see cref="SearchMinLength"/>.</summary>
    public int SearchDebounceMilliseconds { get; set; } = 250;

    /// <summary>
    /// Suggested values offered through a <c>&lt;datalist&gt;</c>. Unlike <see cref="Options"/> these
    /// only propose — the field still accepts anything the user types — which is the difference between
    /// "here are the usual answers" and a closed set of choices. Maps to JSON Schema's <c>examples</c>.
    /// </summary>
    public IList<string> Suggestions { get; set; } = new List<string>();

    /// <summary>
    /// Derives this field's value from the rest of the form — an order total, a full name, a price
    /// after discount. Recomputed whenever a path in <see cref="ComputedDependencies"/> changes, and
    /// once when the form is created.
    /// </summary>
    public BormComputedValue? Computed { get; set; }

    /// <summary>Field paths <see cref="Computed"/> reads. Empty means "recompute on any change".</summary>
    public IList<string> ComputedDependencies { get; set; } = new List<string>();

    /// <summary>
    /// Other field paths whose value this field's rules read, so that changing one of them revalidates
    /// this field. The "confirm password" case: the rule lives on the confirmation box and reads the
    /// password, so fixing the <em>password</em> is what makes the confirmation's error wrong — and
    /// nothing was revalidating it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Paths resolve relative to the object that owns the field before falling back to the root,
    /// exactly as conditions, computed dependencies and cascading options do, so a rule on a repeater's
    /// item template can name its sibling and mean <em>that row</em>. Naming a container covers
    /// everything inside it.
    /// </para>
    /// <para>
    /// A dependent is only revalidated once it has something to say: a field the user has never visited
    /// on a form that has never been submitted is left alone, so typing a password does not light up a
    /// confirmation box nobody has reached yet. React Hook Form spells this <c>deps</c>; TanStack Form
    /// spells it <c>onChangeListenTo</c>.
    /// </para>
    /// </remarks>
    public IList<string> RevalidateOn { get; set; } = new List<string>();

    /// <summary>
    /// Runs when this field's value changes — "when the country changes, clear the city". Not the same
    /// thing as <see cref="Computed"/>: a computed field owns its value and overwrites whatever is
    /// there, while a handler writes a value the user is then free to change.
    /// </summary>
    /// <remarks>
    /// Not raised while the form is being constructed: applying defaults and seeding computed values is
    /// the form initialising, not the user changing something.
    /// </remarks>
    public BormChangeHandler? OnChanged { get; set; }

    /// <summary>
    /// Tidies the value once the user has finished with it — trimming, case folding, stripping
    /// formatting characters. Runs on blur and once more for every field on submit, so a value that
    /// reaches the model has been through it whether or not the user ever visited the box.
    /// </summary>
    /// <remarks>
    /// Deliberately not run per keystroke: trimming as the user types eats the space between two words
    /// the moment it is pressed. What it writes does not mark the field touched — tidying up after
    /// someone is not the same as them having been there.
    /// </remarks>
    public BormValueNormalizer? Normalize { get; set; }

    // --- Constraints (also surfaced as native input attributes) ---
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }
    public double? NumericStep { get; set; }
    public string? Pattern { get; set; }

    // --- File fields ---
    public bool Multiple { get; set; }
    public string? Accept { get; set; }

    /// <summary>Maximum size, in bytes, of each selected file. Null means no limit.</summary>
    public long? MaxFileSize { get; set; }

    // --- Input hints ---
    /// <summary>Value for the HTML <c>autocomplete</c> attribute, e.g. <c>email</c> or <c>new-password</c>.</summary>
    public string? Autocomplete { get; set; }

    /// <summary>Value for the HTML <c>inputmode</c> attribute, e.g. <c>numeric</c> or <c>tel</c>.</summary>
    public string? InputMode { get; set; }

    /// <summary>When true, the rendered control requests focus on first render.</summary>
    public bool Autofocus { get; set; }

    /// <summary>
    /// Which DOM event writes the value back. <see cref="BormUpdateTrigger.Change"/> (the default)
    /// waits for the browser's <c>change</c> event, which for a text box means blur;
    /// <see cref="BormUpdateTrigger.Input"/> updates on every keystroke, which is what makes
    /// live counters, computed values and as-you-type validation feel immediate.
    /// </summary>
    public BormUpdateTrigger UpdateOn { get; set; } = BormUpdateTrigger.Change;

    /// <summary>
    /// How long to wait after the last keystroke before writing the value, in milliseconds. Only
    /// meaningful with <see cref="BormUpdateTrigger.Input"/>; 0 (the default) writes immediately.
    /// </summary>
    public int DebounceMilliseconds { get; set; }

    /// <summary>
    /// Extra HTML attributes splatted onto the rendered control — <c>data-*</c> hooks, <c>title</c>,
    /// <c>spellcheck</c>, <c>maxlength</c> overrides. Attributes the renderer sets itself win, so this
    /// can never break the accessibility wiring.
    /// </summary>
    public IDictionary<string, object?> InputAttributes { get; set; } = new Dictionary<string, object?>();

    /// <summary>
    /// How many columns the field spans in a grid layout. Null uses the layout's default (full width).
    /// </summary>
    public int? ColumnSpan { get; set; }

    // --- Conditional behaviour ---
    /// <summary>When set and evaluates false, the field is hidden and excluded from validation.</summary>
    public IBormCondition? VisibleWhen { get; set; }

    /// <summary>When set and evaluates true, the field is disabled.</summary>
    public IBormCondition? DisabledWhen { get; set; }

    /// <summary>
    /// When set and evaluates true, the field is read-only. Evaluated in addition to
    /// <see cref="ReadOnly"/>, so a field can be locked by circumstance without a second schema.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="DisabledWhen"/> on purpose, and the distinction matters more than it
    /// looks: a disabled control is skipped by keyboard navigation and is not read out at all, so
    /// "this answer is settled — an invoice that has been sent, a field the approver owns" becomes a
    /// value a screen-reader user simply cannot reach. Read-only keeps it focusable, readable and
    /// copyable, which is what "you may look but not change" actually means.
    /// </remarks>
    public IBormCondition? ReadOnlyWhen { get; set; }

    /// <summary>
    /// When set, the field is required only while the condition holds. Evaluated in addition to
    /// <see cref="Required"/>, so a field can be conditionally mandatory without a bespoke rule.
    /// </summary>
    public IBormCondition? RequiredWhen { get; set; }

    /// <summary>
    /// When true, the field's value is cleared as soon as <see cref="VisibleWhen"/> turns false, so a
    /// hidden branch never contributes stale data to the submitted model.
    /// </summary>
    public bool ClearOnHide { get; set; }

    /// <summary>Validation rules applied to this field.</summary>
    public IList<IBormValidationRule> Validators { get; set; } = new List<IBormValidationRule>();

    // --- Composition ---
    /// <summary>Child fields for <see cref="BormFieldType.Object"/>.</summary>
    public IList<BormFieldDefinition> Children { get; set; } = new List<BormFieldDefinition>();

    /// <summary>
    /// Template describing each element of a <see cref="BormFieldType.Array"/>. For arrays of objects this
    /// is itself an <see cref="BormFieldType.Object"/> with <see cref="Children"/>; for arrays of scalars it
    /// is a simple field such as <see cref="BormFieldType.Text"/>.
    /// </summary>
    public BormFieldDefinition? ItemTemplate { get; set; }

    public int? MinItems { get; set; }
    public int? MaxItems { get; set; }

    /// <summary>Key resolving a custom renderer for <see cref="BormFieldType.Custom"/> (and overrides).</summary>
    public string? CustomRenderer { get; set; }

    /// <summary>Extra arbitrary hints/attributes for renderers (e.g. rows, css classes, icons).</summary>
    public IDictionary<string, object?> Attributes { get; set; } = new Dictionary<string, object?>();

    /// <summary>Convenience: is this a container (object or array)?</summary>
    public bool IsContainer => Type is BormFieldType.Object or BormFieldType.Array;

    /// <summary>
    /// True when the field holds no value at all. Presentational content has a name only so the schema
    /// can address it; nothing should try to read, write, default, reset or validate it, and a model
    /// bound to the form has no property of that name to receive it.
    /// </summary>
    public bool IsPresentational => Type is BormFieldType.Static;

    /// <summary>
    /// True when the field renders a set of choices. A combobox counts: it filters the same
    /// <see cref="Options"/> a dropdown lists, and it is fed by the same
    /// <see cref="OptionsProvider"/>.
    /// </summary>
    public bool IsChoice => Type is BormFieldType.Select or BormFieldType.MultiSelect
        or BormFieldType.Radio or BormFieldType.Combobox;

    /// <summary>
    /// Adds a validation rule, replacing any existing rule that reports the same
    /// <see cref="IBormValidationRule.Key"/>. This is what stops a model that carries
    /// <c>[Required]</c> and is then refined with <c>.Required("…")</c> from showing the user the same
    /// complaint twice; the later call wins, so an explicit message always overrides the generated one.
    /// Rules without a key (delegates, custom rules) always accumulate.
    /// </summary>
    public BormFieldDefinition AddValidator(IBormValidationRule rule)
    {
        if (rule.Key is { } key)
        {
            for (var i = 0; i < Validators.Count; i++)
            {
                if (string.Equals(Validators[i].Key, key, StringComparison.Ordinal))
                {
                    Validators[i] = rule;
                    return this;
                }
            }
        }
        Validators.Add(rule);
        return this;
    }

    /// <summary>Removes every rule reporting <paramref name="key"/>. Returns true if any were removed.</summary>
    public bool RemoveValidators(string key)
    {
        var removed = false;
        for (var i = Validators.Count - 1; i >= 0; i--)
        {
            if (string.Equals(Validators[i].Key, key, StringComparison.Ordinal))
            {
                Validators.RemoveAt(i);
                removed = true;
            }
        }
        return removed;
    }

    /// <summary>Fields directly beneath this one: object children, or the array item template.</summary>
    public IEnumerable<BormFieldDefinition> Descendants()
    {
        foreach (var child in Children)
        {
            yield return child;
            foreach (var d in child.Descendants()) yield return d;
        }
        if (ItemTemplate is not null)
        {
            yield return ItemTemplate;
            foreach (var d in ItemTemplate.Descendants()) yield return d;
        }
    }
}
