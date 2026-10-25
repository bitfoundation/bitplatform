using System.Text;

namespace Bit.Borm;

/// <summary>
/// Everything a field renderer needs: the field definition, its absolute data path, the owning
/// <see cref="BormState"/>, and helpers to read/write the value and report validation messages.
/// </summary>
public sealed class BormFieldContext
{
    public BormFieldContext(BormState state, BormFieldDefinition field, string path)
    {
        State = state;
        Field = field;
        Path = path;
        IdPrefix = SafePrefix(state.IdPrefix);
        ElementId = MakeElementId(IdPrefix, path);
    }

    /// <summary>
    /// The id-safe form of <see cref="BormState.IdPrefix"/> this context's ids were derived from.
    /// Kept so a view holding on to a context can tell that the prefix has moved since it was built.
    /// </summary>
    internal string IdPrefix { get; }

    public BormState State { get; }
    public BormFieldDefinition Field { get; }

    /// <summary>The absolute path of this field within the form data (e.g. <c>Items[0].Product</c>).</summary>
    public string Path { get; }

    /// <summary>A DOM-safe id derived from the path.</summary>
    public string ElementId { get; }

    /// <summary>Id of the field's label, for controls that need <c>aria-labelledby</c> (radio and checkbox groups).</summary>
    public string LabelId => ElementId + "_label";

    /// <summary>Id of the help text element, referenced by <see cref="DescribedBy"/>.</summary>
    public string HelpId => ElementId + "_help";

    /// <summary>Id of the validation message list, referenced by <see cref="DescribedBy"/>.</summary>
    public string ErrorId => ElementId + "_error";

    /// <summary>
    /// The field's label as the user reads it — translated (see <see cref="IBormSchemaLocalizer"/>), and
    /// falling back to its name. Renderers show this rather than <see cref="BormFieldDefinition.Label"/>.
    /// </summary>
    public string Label => State.LabelOf(Field, Path) ?? Field.Name;

    /// <summary>The field's help text, translated.</summary>
    public string? HelpText => State.HelpOf(Field, Path);

    /// <summary>The field's placeholder, translated.</summary>
    public string? Placeholder => State.PlaceholderOf(Field, Path);

    /// <summary>The text shown before the input, translated.</summary>
    public string? Prefix => State.PrefixOf(Field, Path);

    /// <summary>The text shown after the input, translated.</summary>
    public string? Suffix => State.SuffixOf(Field, Path);

    /// <summary>
    /// The Blazor edit context the form provides (<c>BormView ProvideEditContext="true"</c>), mirroring
    /// its validation — what a component built on <c>InputBase&lt;T&gt;</c> or a component library's input
    /// reads its invalid state from. Null when the form provides none.
    /// </summary>
    public Microsoft.AspNetCore.Components.Forms.EditContext? EditContext => State.EditContextAdapter?.EditContext;

    /// <summary>
    /// The field's identity in an edit context: the object it belongs to and the member's name — the same
    /// identifier <c>InputBase</c> works out from <see cref="ValueExpression{TValue}"/>.
    /// </summary>
    public Microsoft.AspNetCore.Components.Forms.FieldIdentifier FieldIdentifier
        => State.EditContextAdapter?.FieldIdentifier(Path) ?? BormEditContextAdapter.Identify(State, Path);

    /// <summary>
    /// An expression reading the field's value, for a component that needs one to find its field —
    /// <c>InputBase&lt;T&gt;.ValueExpression</c>, <c>ValidationMessage&lt;T&gt;.For</c>. Pass the
    /// property's own type (or <c>object</c> for a dictionary-backed form).
    /// </summary>
    /// <exception cref="InvalidOperationException">The value is not of type <typeparamref name="TValue"/>.</exception>
    public System.Linq.Expressions.Expression<Func<TValue>> ValueExpression<TValue>()
        => BormEditContextAdapter.ValueExpression<TValue>(State, Path);

    /// <summary>Whether the field is disabled by a condition.</summary>
    public bool IsDisabled => State.IsDisabled(Field, Path);

    /// <summary>
    /// Whether the field is read-only — in its own right, because the whole form is, or because an
    /// object or repeater it sits inside is.
    /// </summary>
    /// <remarks>
    /// The containers count because a read-only repeater used to be rendered as a <em>disabled</em>
    /// fieldset, which locked its rows by disabling every control in them — out of the tab order and
    /// unreadable, the opposite of what read-only promises. The fieldset now disables only for
    /// disabled, so the rows have to learn that they are read-only from here instead.
    /// </remarks>
    public bool IsReadOnly => State.IsReadOnly(Field, Path) || InsideReadOnlyContainer();

    private (BormFieldDefinition Field, string Path)[]? _containers;

    private bool InsideReadOnlyContainer()
    {
        // Resolved once: the field and path a context describes never change, and this is read on
        // every render of every field.
        _containers ??= FindContainers();
        foreach (var (field, path) in _containers)
            if (State.IsReadOnly(field, path)) return true;
        return false;
    }

    private (BormFieldDefinition Field, string Path)[] FindContainers()
    {
        List<(BormFieldDefinition, string)>? found = null;
        for (var path = BormPath.Parent(Path); path.Length > 0; path = BormPath.Parent(path))
        {
            if (State.FindField(path) is { IsContainer: true } container)
                (found ??= []).Add((container, path));
        }
        return found?.ToArray() ?? [];
    }

    /// <summary>Whether the field is required right now (including <see cref="BormFieldDefinition.RequiredWhen"/>).</summary>
    public bool IsRequired => State.IsRequired(Field, Path);

    /// <summary>
    /// The form data, scoped to the object that owns this field: reading <c>"Quantity"</c> from a field
    /// inside a repeater row gives that row's quantity. Absolute paths still resolve.
    /// </summary>
    public IBormDataReader Data => BormScopedDataReader.ForOwnerOf(State.Data, Path);

    /// <summary>The choices to render, resolved through the field's options provider when it has one.</summary>
    public IReadOnlyList<BormSelectOption> Options => State.OptionsFor(Field, Path);

    /// <summary>True while an asynchronous options provider is loading this field's choices.</summary>
    public bool IsLoadingOptions => State.IsLoadingOptions(Path);

    /// <summary>
    /// True while an asynchronous rule is checking this field — the "checking that username…" state a
    /// remote lookup deserves a spinner for. False for a field whose rules are all synchronous.
    /// </summary>
    public bool IsValidating => State.IsValidatingField(Path);

    /// <summary>True when this field's options provider failed; the choices are empty for a reason.</summary>
    public bool OptionsFailed => State.OptionsError(Path) is not null;

    public object? Value => State.GetValue(Path);

    /// <summary>
    /// The schema's extra HTML attributes, ready to splat. Null when there are none, so a renderer's
    /// <c>@attributes</c> emits nothing at all rather than an empty dictionary's worth of churn.
    /// </summary>
    /// <remarks>
    /// Built once and reused. It is read on every render of every field, and handing Blazor a fresh
    /// dictionary each time would make an unchanged splat look like a change to diff.
    /// </remarks>
    public IReadOnlyDictionary<string, object>? InputAttributes => _inputAttributes ??= BuildInputAttributes();

    private IReadOnlyDictionary<string, object>? _inputAttributes;
    private bool _inputAttributesBuilt;

    private IReadOnlyDictionary<string, object>? BuildInputAttributes()
    {
        if (_inputAttributesBuilt) return null;
        _inputAttributesBuilt = true;

        if (Field.InputAttributes.Count == 0) return null;

        var map = new Dictionary<string, object>(Field.InputAttributes.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in Field.InputAttributes)
            if (value is not null) map[key] = value;
        return map.Count == 0 ? null : map;
    }

    /// <summary>Resolves a piece of the library's own UI text through the registered message provider.</summary>
    public string Text(string key, params object?[] args) => State.Text(key, args);

    /// <summary>
    /// The name the control posts its value under when the form is rendered to be posted (<c>BormView FormName</c>) -
    /// <c>Model.Lines[0].Product</c>, which the page's <c>[SupplyParameterFromForm]</c> model binds - or null while it is not.
    /// </summary>
    public string? InputName => State.PostedName is { } model ? (model.Length == 0 ? Path : model + "." + Path) : null;

    /// <summary>
    /// The current value formatted for an HTML input. A posted form rendered on the server shows the text the model
    /// refused rather than the value it kept, so the box holds what its message is about; in the browser the control
    /// already holds that text, and the value it is given is left alone.
    /// </summary>
    public string StringValue => State.RendersStatically && State.RefusedEntry(Path) is { } refused
        ? refused
        : BormValueConverter.ToInputString(Value, Field.Type);

    /// <summary>
    /// The value as a text box shows it: <see cref="StringValue"/>, put back into its mask's shape when the
    /// model holds only what filled the slots (<see cref="BormFieldDefinition.MaskStoresRaw"/>).
    /// </summary>
    public string DisplayText => Field.MaskStoresRaw && Field.ParsedMask is { } mask && mask.TryFormat(StringValue, out var shaped)
        ? shaped
        : StringValue;

    public bool BoolValue => Value switch
    {
        bool b => b,
        string s => s is "true" or "True" or "on" or "1",
        _ => false
    };

    public IReadOnlyList<BormValidationMessage> Messages => State.MessagesFor(Path);
    public bool HasError => Messages.Any(m => m.Severity == BormValidationSeverity.Error);

    /// <summary>
    /// Whether validation messages should be shown yet. Errors stay hidden until the user has
    /// interacted with the field or tried to submit, so a blank form does not open covered in red.
    /// </summary>
    public bool ShowMessages => State.IsTouched(Path) || State.IsSubmitted;

    /// <summary>Value for the <c>aria-invalid</c> attribute, or null when the attribute should be omitted.</summary>
    public string? AriaInvalid => HasError && ShowMessages ? "true" : null;

    /// <summary>
    /// The ids an input should point <c>aria-describedby</c> at: its help text and, when they are
    /// showing, its validation messages. Null when there is nothing to describe.
    /// </summary>
    public string? DescribedBy
    {
        get
        {
            var hasHelp = !string.IsNullOrEmpty(HelpText);
            var hasMessages = ShowMessages && Messages.Count > 0;
            if (!hasHelp && !hasMessages) return null;

            var sb = new StringBuilder();
            if (hasHelp) sb.Append(HelpId);
            if (hasMessages)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(ErrorId);
            }
            return sb.ToString();
        }
    }

    /// <summary>Writes a parsed value from raw input text and revalidates according to the form's trigger.</summary>
    /// <param name="raw">The text the control produced.</param>
    /// <param name="includeAsync">
    /// Whether asynchronous rules run too. False on an ordinary keystroke, where a remote lookup per
    /// character is exactly what nobody wants; true once a debounced field's pause has elapsed, which
    /// is the moment a uniqueness check becomes affordable and the whole reason to debounce a field.
    /// </param>
    public async Task SetFromStringAsync(string? raw, bool includeAsync = false)
    {
        // The current value goes along so a timestamp keeps the offset it had: the input shows only
        // its clock time, and the offset is what makes the round trip land on the same instant.
        var parsed = BormValueConverter.FromInputString(raw, Field.ValueType, Field.Type, Value);
        State.SetValue(Path, parsed);
        if (State.ShouldValidate(Path, BormValidationTrigger.OnChange))
            await State.ValidateFieldAsync(Field, Path, includeAsync);
        await State.ValidateDependentsAsync(Path, includeAsync);
    }

    /// <summary>Writes a value directly and revalidates according to the form's trigger.</summary>
    public async Task SetValueAsync(object? value)
    {
        State.SetValue(Path, value);
        if (State.ShouldValidate(Path, BormValidationTrigger.OnChange))
            await State.ValidateFieldAsync(Field, Path, includeAsync: false);
        await State.ValidateDependentsAsync(Path);
    }

    /// <summary>
    /// Handles the field losing focus: marks it touched and runs validation — including async rules,
    /// which are too expensive for every keystroke — when the configured trigger calls for it.
    /// </summary>
    public async Task BlurAsync()
    {
        // Before the field is judged, not after: a rule that rejects " a@b.com " for the space is
        // technically right and useless, and trimming inside the rule would fix the message while
        // leaving the untidy value in the model.
        State.NormalizeField(Field, Path);
        State.MarkTouched(Path);
        if (State.ShouldValidate(Path, BormValidationTrigger.OnBlur))
            await State.ValidateFieldAsync(Field, Path, includeAsync: true);
        // On blur the dependents get the full treatment: leaving a field is exactly when an async
        // cross-field check is affordable, and it is the last chance before submit.
        await State.ValidateDependentsAsync(Path, includeAsync: true);
    }

    /// <summary>Runs every rule for this field, including async ones, regardless of the configured trigger.</summary>
    public Task ValidateAsync() => State.ValidateFieldAsync(Field, Path, includeAsync: true).AsTask();

    /// <summary>Loads the field's options if it has a provider and they are not cached yet.</summary>
    public ValueTask EnsureOptionsAsync() => State.EnsureOptionsAsync(Field, Path);

    /// <summary>
    /// The DOM id of one option inside a radio or multi-select group.
    /// </summary>
    /// <remarks>
    /// The index is part of the id, not decoration. Folding non-id characters to <c>_</c> makes
    /// <c>en-US</c> and <c>en_US</c> the same string, and two options sharing an id leaves one
    /// <c>&lt;label for&gt;</c> pointing at the other's control — so clicking one label ticks the wrong
    /// box, silently.
    /// </remarks>
    public static string OptionId(string elementId, int index, string value)
    {
        var sb = new StringBuilder(elementId.Length + value.Length + 8).Append(elementId).Append('_').Append(index);
        if (value.Length == 0) return sb.ToString();

        sb.Append('_');
        foreach (var c in value)
            sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        return sb.ToString();
    }

    /// <summary>
    /// An id for something that belongs to the form as a whole rather than to one field — the list of
    /// form-level messages, say — carrying the same prefix as every field id, so two forms on one page
    /// keep theirs apart too.
    /// </summary>
    internal static string FormElementId(BormState state, string name) => SafePrefix(state.IdPrefix) + name;

    /// <summary>
    /// Turns a data path into an id that is valid in HTML and safe in a CSS selector:
    /// <c>Items[0].Product</c> becomes <c>borm_Items_0_Product</c> under the default prefix.
    /// </summary>
    private static string MakeElementId(string prefix, string path)
    {
        var sb = new StringBuilder(prefix.Length + path.Length).Append(prefix);
        foreach (var c in path)
        {
            if (c == ']') continue; // the opening bracket already became the separator
            sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
        }
        return sb.ToString();
    }

    /// <summary>
    /// The prefix folded to characters that are safe in an id, a URL fragment and a CSS selector alike.
    /// The prefix is the application's to choose, and one containing a space or a <c>#</c> would
    /// otherwise produce ids that a <c>&lt;label for&gt;</c> or an error-summary link can never match.
    /// </summary>
    internal static string SafePrefix(string? prefix)
    {
        if (string.IsNullOrEmpty(prefix)) return "borm_";

        // A selector cannot start with a digit (or a hyphen and a digit) without escaping, so such a
        // prefix is given a letter to lead with.
        var leadsBadly = char.IsDigit(prefix[0]) || (prefix[0] == '-' && (prefix.Length == 1 || char.IsDigit(prefix[1])));

        var safe = !leadsBadly;
        foreach (var c in prefix)
            if (!char.IsLetterOrDigit(c) && c is not ('_' or '-')) { safe = false; break; }
        if (safe) return prefix;

        var sb = new StringBuilder(prefix.Length + 1);
        if (leadsBadly) sb.Append('f');
        foreach (var c in prefix)
            sb.Append(char.IsLetterOrDigit(c) || c is '_' or '-' ? c : '_');
        return sb.ToString();
    }
}
