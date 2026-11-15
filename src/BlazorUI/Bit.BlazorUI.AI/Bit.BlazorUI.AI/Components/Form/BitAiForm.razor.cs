using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiForm renders a form the model proposed, for the user to check and submit, out of the core input components.
/// </summary>
/// <remarks>
/// Validation happens here rather than in the model, because a model asked to validate its own output will sometimes
/// say yes when the answer is no. Fields appear as they stream in, so a form generated field by field fills in rather
/// than blinking into existence.
/// <br />
/// Values are kept flat, keyed by path (<c>address.city</c>, <c>tags[0]</c>, <c>people[1].name</c>), which is what
/// lets a nested object or a repeatable group stream in field by field like a flat form does. The definition turns the
/// flat values back into JSON of the shape of the schema on submit (<see cref="OnSubmitJson"/>).
/// </remarks>
public partial class BitAiForm : BitAiComponentBase
{
    private readonly Dictionary<string, string?> _values = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _errors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BitAiFormField> _seeded = new(StringComparer.Ordinal);
    private readonly ConditionalWeakTable<BitAiFormField, List<BitDropdownItem<string>>> _dropdownItems = new();

    // The buttons of the lists, by the path of the entry (remove) or of the list (add), for the focus to be handed to
    // once a remove button went away with its entry (see FocusAfterRemoveAsync).
    private readonly Dictionary<string, BitButton> _removeButtons = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BitButton> _addButtons = new(StringComparer.Ordinal);
    private (string Path, int Index)? _focusAfterRemove;



    /// <summary>
    /// Gets or sets the cascading parameters for the form component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple form components through
    /// the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiFormParams.ParamName)]
    public BitAiFormParams? CascadingParameters { get; set; }



    /// <summary>
    /// Disables the actions while something else is in flight.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    [Parameter] public bool Busy { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the form.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiFormClassStyles? Classes { get; set; }

    /// <summary>
    /// The form to render.
    /// </summary>
    [Parameter, EditorRequired] public BitAiFormDefinition Definition { get; set; } = default!;

    /// <summary>
    /// The callback raised when the user cancels. The cancel button is shown only while it is set (and the
    /// <see cref="BitAiFormDefinition.CancelLabel"/> is not an empty string).
    /// </summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    /// <summary>
    /// The callback raised with the collected values when the user submits a valid form. The values are flat text,
    /// keyed by field path; see <see cref="OnSubmitJson"/> for the typed, nested shape.
    /// </summary>
    [Parameter] public EventCallback<IReadOnlyDictionary<string, string?>> OnSubmit { get; set; }

    /// <summary>
    /// The callback raised with the values as JSON in the shape of the schema the form came from: nested objects,
    /// arrays, numbers and booleans encoded as such. Raised after <see cref="OnSubmit"/>.
    /// </summary>
    [Parameter] public EventCallback<JsonElement> OnSubmitJson { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the form.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiFormClassStyles? Styles { get; set; }

    /// <summary>
    /// Whether the form has already been submitted, which replaces the actions with a confirmation.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    [Parameter] public bool Submitted { get; set; }



    /// <summary>
    /// The values as they currently stand, keyed by field path.
    /// </summary>
    public IReadOnlyDictionary<string, string?> Values => _values;



    protected override string RootElementClass => "bit-aifrm";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        // Seed each field once, so a field streaming in later never overwrites something the user has already typed.
        // A field whose definition changed (a bound, a choice, a prefill or a nested field streaming in) is followed
        // rather than seeded again, and fields that vanished take their values along.
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in Definition?.Fields ?? [])
        {
            names.Add(field.Name);
            if (_seeded.TryGetValue(field.Name, out var previous))
            {
                if (BitAiFormField.SameShape(previous, field)) continue;

                _seeded[field.Name] = field;
                Reseed(previous, field, field.Name);
                continue;
            }

            _seeded[field.Name] = field;
            Seed(field, field.Name);
        }

        foreach (var stale in _seeded.Keys.Where(k => names.Contains(k) is false).ToList())
        {
            _seeded.Remove(stale);
            Prune(stale);
        }

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        // The focus moves once the entries have been laid out without the removed one (see RemoveItem).
        if (_focusAfterRemove is { } removed)
        {
            _focusAfterRemove = null;

            await FocusAfterRemoveAsync(removed.Path, removed.Index);
        }
    }



    /// <summary>
    /// A DOM id for a field. Paths come from model-written property names, so anything outside the characters an id
    /// selector accepts is replaced, and a stable hash keeps two different names that sanitise alike from colliding.
    /// </summary>
    internal string FieldId(string path)
    {
        var changed = false;
        var builder = new System.Text.StringBuilder(path.Length);
        foreach (var c in path)
        {
            if (char.IsAsciiLetterOrDigit(c) || c is '_' or '-')
            {
                builder.Append(c);
            }
            else
            {
                builder.Append('_');
                changed = true;
            }
        }

        if (changed)
        {
            uint hash = 2166136261;
            foreach (var c in path) hash = (hash ^ c) * 16777619;
            builder.Append('-').Append(hash.ToString("x8", CultureInfo.InvariantCulture));
        }

        return $"{_Id}-{builder}";
    }



    private void Seed(BitAiFormField field, string path)
    {
        switch (field.Type)
        {
            case BitAiFieldType.Object:
                foreach (var child in field.Fields ?? []) Seed(child, BitAiFormDefinition.Path(path, child.Name));
                break;

            case BitAiFieldType.Array:
                if (field.Value is { Length: > 0 } json && TryParseJson(json) is { ValueKind: JsonValueKind.Array } array)
                {
                    SeedFromJson(field, path, array);
                }
                break;

            default:
                _values[path] = SeedValue(field, field.Value);
                break;
        }
    }

    // A checkbox shows unchecked while it has no value, so that is the value it starts with: an untouched one is then
    // submitted as the false it shows rather than left out (or written as null when it is required).
    private static string? SeedValue(BitAiFormField field, string? value) =>
        field.Type is BitAiFieldType.Boolean && string.IsNullOrWhiteSpace(value) ? "false" : value;

    /// <summary>
    /// Follows a field whose definition changed since it was seeded. Only a different control seeds it again: a schema
    /// streaming in hands the same field over with its bounds, its choices, its labels, its nested fields and its
    /// prefill arriving one at a time, and none of those may take away what the user has typed meanwhile.
    /// </summary>
    private void Reseed(BitAiFormField previous, BitAiFormField field, string path)
    {
        // A prefill of an object or a list is a whole set of entries, which nothing typed into one of them could be
        // weighed against, so a new one still seeds them again.
        if (previous.Type != field.Type
            || (previous.Value != field.Value && field.Type is BitAiFieldType.Object or BitAiFieldType.Array))
        {
            Prune(path);
            Seed(field, path);
            return;
        }

        // A prefill streams in after the field it belongs to ("default" comes after "title" in a schema), often while
        // the user is already typing into it. It is followed only while the field still holds the prefill it had, so
        // an untouched field fills in as the model writes and what the user typed wins over what arrives later.
        if (previous.Value != field.Value && _values.GetValueOrDefault(path) == SeedValue(previous, previous.Value))
        {
            _values[path] = SeedValue(field, field.Value);
            _errors.Remove(path);
        }

        switch (field.Type)
        {
            case BitAiFieldType.Object:
                ReseedFields(previous.Fields ?? [], field.Fields ?? [], path);
                break;

            case BitAiFieldType.Array:
                var before = previous.Items ?? new BitAiFormField("item");
                var after = field.Items ?? new BitAiFormField("item");
                if (BitAiFormField.SameShape(before, after)) break;

                for (var i = BitAiFormDefinition.CountItems(_values, path) - 1; i >= 0; i--)
                {
                    var itemPath = BitAiFormDefinition.IndexPath(path, i);
                    Reseed(before, after, itemPath);

                    // An entry seeded again must still leave a trace in the values, or it would stop being counted.
                    if (_values.Keys.Any(k => IsWithin(k, itemPath)) is false) _values[itemPath] = null;
                }
                break;

            // A choice the options no longer offer - one picked while its text was still streaming - goes back to the
            // prefill; any other value stays as the user left it.
            case BitAiFieldType.Select:
                if (_values.GetValueOrDefault(path) is { } chosen && field.Options is { } options && options.Contains(chosen, StringComparer.Ordinal) is false)
                {
                    _values[path] = SeedValue(field, field.Value);
                    _errors.Remove(path);
                }
                break;
        }
    }

    private void ReseedFields(IReadOnlyList<BitAiFormField> previous, IReadOnlyList<BitAiFormField> fields, string path)
    {
        foreach (var field in fields)
        {
            var childPath = BitAiFormDefinition.Path(path, field.Name);
            var before = previous.FirstOrDefault(f => f.Name == field.Name);

            if (before is null) Seed(field, childPath);
            else if (BitAiFormField.SameShape(before, field) is false) Reseed(before, field, childPath);
        }

        foreach (var gone in previous.Where(p => fields.Any(f => f.Name == p.Name) is false))
        {
            Prune(BitAiFormDefinition.Path(path, gone.Name));
        }
    }

    private void SeedFromJson(BitAiFormField field, string path, JsonElement element)
    {
        switch (field.Type)
        {
            case BitAiFieldType.Object:
                foreach (var child in field.Fields ?? [])
                {
                    var childPath = BitAiFormDefinition.Path(path, child.Name);
                    if (element.ValueKind is JsonValueKind.Object && element.TryGetProperty(child.Name, out var value))
                    {
                        SeedFromJson(child, childPath, value);
                    }
                    else
                    {
                        Seed(child, childPath);
                    }
                }
                break;

            case BitAiFieldType.Array:
                if (element.ValueKind is not JsonValueKind.Array) break;
                var template = field.Items ?? new BitAiFormField("item");
                var index = 0;
                foreach (var entry in element.EnumerateArray())
                {
                    SeedFromJson(template, BitAiFormDefinition.IndexPath(path, index++), entry);
                }
                break;

            default:
                _values[path] = SeedValue(field, element.ValueKind switch
                {
                    JsonValueKind.Null or JsonValueKind.Undefined => null,
                    JsonValueKind.String => element.GetString(),
                    _ => element.GetRawText(),
                });
                break;
        }
    }

    private static JsonElement? TryParseJson(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Removes the value and error of a path and of everything nested under it.</summary>
    private void Prune(string path)
    {
        foreach (var key in _values.Keys.Where(k => IsWithin(k, path)).ToList()) _values.Remove(key);
        foreach (var key in _errors.Keys.Where(k => IsWithin(k, path)).ToList()) _errors.Remove(key);
    }

    private static bool IsWithin(string key, string path) =>
        key == path
        || (key.Length > path.Length && key.StartsWith(path, StringComparison.Ordinal) && key[path.Length] is '.' or '[');



    private void Set(string path, string? value)
    {
        _values[path] = value;
        _errors.Remove(path);
    }

    private void AddItem(BitAiFormField field, string path)
    {
        var index = BitAiFormDefinition.CountItems(_values, path);

        // The add button stays where it is at the most entries a list takes, turned off rather than taken away (which
        // would drop the focus it holds), so a press that still reaches it - a screen reader's click - adds nothing.
        if (Disabled || field.ReadOnly || (field.MaxItems is { } max && index >= max)) return;

        var template = field.Items ?? new BitAiFormField("item");
        var itemPath = BitAiFormDefinition.IndexPath(path, index);

        // An entry must leave a trace in the values even before anything is typed, or it would not be counted and the
        // next add would land on the same index.
        if (template.Type is BitAiFieldType.Object)
        {
            foreach (var child in template.Fields ?? []) Seed(child, BitAiFormDefinition.Path(itemPath, child.Name));
            if (_values.Keys.Any(k => IsWithin(k, itemPath)) is false) _values[itemPath] = null;
        }
        else
        {
            _values[itemPath] = SeedValue(template, template.Value);
        }

        _errors.Remove(path);
    }

    private void RemoveItem(string path, int index)
    {
        if (Disabled) return;

        var count = BitAiFormDefinition.CountItems(_values, path);
        Prune(BitAiFormDefinition.IndexPath(path, index));

        // The last slot is the one that goes away (the entries after the removed one move down a slot), and the remove
        // button rendered in it with it.
        _removeButtons.Remove(BitAiFormDefinition.IndexPath(path, count - 1));
        _focusAfterRemove = (path, index);

        // Everything after the removed entry moves down one slot, so the entries stay contiguous.
        for (var i = index + 1; i < count; i++)
        {
            var from = BitAiFormDefinition.IndexPath(path, i);
            var to = BitAiFormDefinition.IndexPath(path, i - 1);
            foreach (var key in _values.Keys.Where(k => IsWithin(k, from)).ToList())
            {
                _values[string.Concat(to, key.AsSpan(from.Length))] = _values[key];
                _values.Remove(key);
            }

            foreach (var key in _errors.Keys.Where(k => IsWithin(k, from)).ToList())
            {
                _errors[string.Concat(to, key.AsSpan(from.Length))] = _errors[key];
                _errors.Remove(key);
            }
        }

        _errors.Remove(path);
    }

    // The remove button that was pressed went away - with its entry when it was the last one, or the entry kept its
    // slot and took the next one's values - so the focus is put where the keyboard can carry on from: on the remove
    // button now in that slot, on the one of the new last entry, or on the add button of a list left empty. A press
    // does not focus a button in every engine (WebKit's), so it is moved there in any case rather than left behind.
    private async Task FocusAfterRemoveAsync(string path, int index)
    {
        var count = BitAiFormDefinition.CountItems(_values, path);
        var target = count > 0
            ? _removeButtons.GetValueOrDefault(BitAiFormDefinition.IndexPath(path, Math.Min(index, count - 1)))
            : _addButtons.GetValueOrDefault(path);

        if (target is null || IsDisposing) return;

        try
        {
            await target.FocusAsync();
        }
        catch (JSDisconnectedException) { } // the circuit is gone, and the page with it
        catch (OperationCanceledException) { }
        catch (JSException) { } // a button taken off the page in the meantime has nothing to focus
        catch (InvalidOperationException) { } // a button not rendered yet has no element to focus
    }

    private async Task HandleOnSubmitClick()
    {
        // A press that reaches a button while it is unavailable - a screen reader's click, a script - submits nothing.
        if (Disabled || Busy || Submitted) return;

        if (Validate() is false) return;

        await OnSubmit.InvokeAsync(new Dictionary<string, string?>(_values, StringComparer.Ordinal));

        if (OnSubmitJson.HasDelegate) await OnSubmitJson.InvokeAsync(Definition.ToJson(_values));
    }

    private async Task HandleOnCancelClick()
    {
        if (Disabled || Busy || Submitted) return;

        await OnCancel.InvokeAsync();
    }



    private bool Validate()
    {
        _errors.Clear();
        ValidateFields(Definition.Fields, null);
        return _errors.Count == 0;
    }

    private void ValidateFields(IReadOnlyList<BitAiFormField> fields, string? parent)
    {
        foreach (var field in fields) ValidateField(field, BitAiFormDefinition.Path(parent, field.Name));
    }

    private void ValidateField(BitAiFormField field, string path)
    {
        switch (field.Type)
        {
            case BitAiFieldType.Object:
                ValidateFields(field.Fields ?? [], path);
                break;

            case BitAiFieldType.Array:
                var count = BitAiFormDefinition.CountItems(_values, path);
                if (count == 0 && field.Required)
                {
                    _errors[path] = Strings.FieldRequired(field.DisplayLabel);
                    break;
                }

                if (field.MinItems is { } minItems && count < minItems)
                {
                    _errors[path] = Strings.FieldTooFewItems(field.DisplayLabel, minItems);
                }
                else if (field.MaxItems is { } maxItems && count > maxItems)
                {
                    _errors[path] = Strings.FieldTooManyItems(field.DisplayLabel, maxItems);
                }

                var template = field.Items ?? new BitAiFormField("item");
                for (var i = 0; i < count; i++)
                {
                    var itemPath = BitAiFormDefinition.IndexPath(path, i);
                    if (template.Type is BitAiFieldType.Object)
                    {
                        ValidateFields(template.Fields ?? [], itemPath);
                    }
                    else
                    {
                        ValidateScalar(template with { Label = Strings.ListItem(field.DisplayLabel, i + 1), Required = true }, itemPath);
                    }
                }
                break;

            default:
                ValidateScalar(field, path);
                break;
        }
    }

    private void ValidateScalar(BitAiFormField field, string path)
    {
        var value = _values.GetValueOrDefault(path);
        var label = field.DisplayLabel;

        if (string.IsNullOrWhiteSpace(value))
        {
            if (field.Required && field.Type is not BitAiFieldType.Boolean) _errors[path] = Strings.FieldRequired(label);
            return;
        }

        string? error = field.Type switch
        {
            BitAiFieldType.Number => ValidateNumber(field, value),
            BitAiFieldType.Email when IsEmail(value) is false => Strings.FieldNotAnEmail(label),
            BitAiFieldType.Url when IsWebAddress(value) is false => Strings.FieldNotAUrl(label),
            BitAiFieldType.Date or BitAiFieldType.DateTime or BitAiFieldType.Time when IsDateOrTime(field.Type, value) is false => Strings.FieldNotADate(label),
            BitAiFieldType.Select when field.Options is { } options && options.Contains(value, StringComparer.Ordinal) is false => Strings.FieldInvalidFormat(label),
            _ => null,
        };

        if (error is null && field.Type is BitAiFieldType.Text or BitAiFieldType.MultilineText or BitAiFieldType.Email or BitAiFieldType.Url or BitAiFieldType.Password)
        {
            if (field.MinLength is { } minLength && value.Length < minLength) error = Strings.FieldTooShort(label, minLength);
            else if (field.MaxLength is { } maxLength && value.Length > maxLength) error = Strings.FieldTooLong(label, maxLength);
        }

        if (error is null && field.Pattern is { Length: > 0 } pattern && IsMatch(value, pattern) is false)
        {
            error = Strings.FieldInvalidFormat(label);
        }

        if (error is not null) _errors[path] = error;
    }

    private string? ValidateNumber(BitAiFormField field, string value)
    {
        var label = field.DisplayLabel;

        // The values are kept in the invariant form ("1.5") whatever the culture of the page, so parsing with the
        // current culture would read it as 15 in de-DE.
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) is false || double.IsFinite(number) is false)
        {
            return Strings.FieldNotANumber(label);
        }

        if (field.Integer && Math.Abs(number - Math.Round(number)) > 0) return Strings.FieldNotAnInteger(label);
        if (field.Minimum is { } min && number < min) return Strings.FieldBelowMinimum(label, min);
        if (field.Maximum is { } max && number > max) return Strings.FieldAboveMaximum(label, max);
        if (field.ExclusiveMinimum is { } exMin && number <= exMin) return Strings.FieldNotAboveMinimum(label, exMin);
        if (field.ExclusiveMaximum is { } exMax && number >= exMax) return Strings.FieldNotBelowMaximum(label, exMax);

        if (field.MultipleOf is { } step && step > 0)
        {
            var quotient = number / step;
            if (Math.Abs(quotient - Math.Round(quotient)) > 1e-9) return Strings.FieldNotAMultipleOf(label, step);
        }

        return null;
    }

    private static bool IsEmail(string value)
    {
        var at = value.IndexOf('@');
        return at > 0 && at < value.Length - 1 && value.IndexOf('@', at + 1) < 0 && value.Any(char.IsWhiteSpace) is false;
    }

    // A web address, as the message says: an absolute http or https URL with a host. Any other absolute URI parses
    // too - javascript:alert(1), mailto:, foo:bar, a Windows path read as file:///c:/ - and none of them is one.
    private static bool IsWebAddress(string value) =>
        Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && uri.Host.Length > 0;

    private static bool IsDateOrTime(BitAiFieldType type, string value) => type switch
    {
        BitAiFieldType.Date => DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
        BitAiFieldType.Time => TimeOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
        _ => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _),
    };

    /// <remarks>
    /// A model can produce a pattern that backtracks catastrophically, so matching runs under a timeout and a failure
    /// to match is treated as a validation failure rather than an exception.
    /// </remarks>
    private static bool IsMatch(string value, string pattern)
    {
        try
        {
            return Regex.IsMatch(value, pattern, RegexOptions.None, TimeSpan.FromMilliseconds(200));
        }
        catch (ArgumentException)
        {
            return true;
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }



    // The values are text in the invariant form a native input would have reported - "4", "1.5", "true",
    // "2024-05-01", "2024-05-01T10:30", "10:30" - which is what validation reads and what the definition encodes
    // into JSON; the core inputs work on typed values, so each control converts at its edge.

    private static string? Invariant(double? value) => value?.ToString("R", CultureInfo.InvariantCulture);

    private static string? NumberStep(BitAiFormField field) =>
        field.MultipleOf is { } step ? Invariant(step) : field.Integer ? "1" : null;

    private static double? ParseNumber(string? value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number) ? number : null;

    private static bool IsTrue(string? value) => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    private static string ToBoolText(bool value) => value ? "true" : "false";

    // The date pickers are pinned to UTC and the values are read and written as UTC wall clock, so a date is the same
    // day and a time the same hour whatever the time zone of the browser.
    private static DateTimeOffset? ParseDate(string? value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date)
            ? date
            : null;

    private static string? FormatDate(BitAiFieldType type, DateTimeOffset? value) => value?.UtcDateTime.ToString(
        type is BitAiFieldType.DateTime ? "yyyy-MM-dd'T'HH:mm" : "yyyy-MM-dd",
        CultureInfo.InvariantCulture);

    private static TimeSpan? ParseTime(string? value) =>
        TimeOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time) ? time.ToTimeSpan() : null;

    private static string? FormatTime(TimeSpan? value) => value?.ToString(
        value.Value.Seconds == 0 ? @"hh\:mm" : @"hh\:mm\:ss",
        CultureInfo.InvariantCulture);

    private static BitInputType? InputType(BitAiFieldType type) => type switch
    {
        BitAiFieldType.Email => BitInputType.Email,
        BitAiFieldType.Url => BitInputType.Url,
        BitAiFieldType.Password => BitInputType.Password,
        _ => null,
    };

    // The items of a select are built once per field definition rather than on every render, so a dropdown re-rendered
    // by a keystroke elsewhere in the form is handed the same list.
    private List<BitDropdownItem<string>> GetDropdownItems(BitAiFormField field) => _dropdownItems.GetValue(field, static f =>
    {
        var options = f.Options ?? [];
        var items = new List<BitDropdownItem<string>>(options.Count);
        for (var i = 0; i < options.Count; i++)
        {
            var label = f.OptionLabels is { } labels && i < labels.Count ? labels[i] : options[i];
            items.Add(new BitDropdownItem<string> { Text = label, Value = options[i] });
        }

        return items;
    });

    // A control without an error message of its own (the checkbox) is marked invalid through its input, and pointed at
    // the message the form renders beside it.
    private static Dictionary<string, object> InvalidInputAttributes => new() { ["aria-invalid"] = "true" };

    private static string? DescribedBy(string id, bool hasHint, bool hasError) => (hasHint, hasError) switch
    {
        (true, true) => $"{id}-hint {id}-error",
        (true, false) => $"{id}-hint",
        (false, true) => $"{id}-error",
        _ => null,
    };
}
