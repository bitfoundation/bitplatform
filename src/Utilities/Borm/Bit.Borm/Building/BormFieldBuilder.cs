namespace Bit.Borm;

/// <summary>
/// Fluent configuration for a single <see cref="BormFieldDefinition"/>. Returned by the form builders
/// so fields can be described with chained, readable calls.
/// </summary>
/// <remarks>
/// Constraint methods go through <see cref="BormFieldDefinition.AddValidator"/>, so calling
/// <c>.Required("…")</c> on a field whose model already carries <c>[Required]</c> replaces the generated
/// rule rather than stacking a second identical message on top of it.
/// </remarks>
public sealed class BormFieldBuilder
{
    private readonly BormFieldDefinition _field;

    public BormFieldBuilder(BormFieldDefinition field) => _field = field;

    /// <summary>The field being configured.</summary>
    public BormFieldDefinition Definition => _field;

    // --- Metadata ---
    public BormFieldBuilder Label(string label) { _field.Label = label; return this; }
    public BormFieldBuilder Placeholder(string placeholder) { _field.Placeholder = placeholder; return this; }
    public BormFieldBuilder Help(string help) { _field.HelpText = help; return this; }
    public BormFieldBuilder Order(int order) { _field.Order = order; return this; }
    public BormFieldBuilder Group(string group) { _field.Group = group; return this; }
    public BormFieldBuilder Default(object? value) { _field.DefaultValue = value; return this; }
    public BormFieldBuilder ReadOnly(bool readOnly = true) { _field.ReadOnly = readOnly; return this; }

    /// <summary>
    /// Marks the value as one that must not outlive the page — left out of drafts and of snapshots that
    /// exclude sensitive values. A password is sensitive without asking.
    /// </summary>
    public BormFieldBuilder Sensitive(bool sensitive = true) { _field.Sensitive = sensitive; return this; }
    public BormFieldBuilder Attr(string key, object? value) { _field.Attributes[key] = value; return this; }
    public BormFieldBuilder CustomRenderer(string key) { _field.CustomRenderer = key; _field.Type = BormFieldType.Custom; return this; }

    /// <summary>Sets the HTML <c>autocomplete</c> token, e.g. <c>email</c>, <c>street-address</c>, <c>new-password</c>.</summary>
    public BormFieldBuilder Autocomplete(string value) { _field.Autocomplete = value; return this; }

    /// <summary>Sets the HTML <c>inputmode</c> token, which chooses the on-screen keyboard on mobile.</summary>
    public BormFieldBuilder InputMode(string value) { _field.InputMode = value; return this; }

    /// <summary>Focuses this field when the form first renders.</summary>
    public BormFieldBuilder Autofocus(bool value = true) { _field.Autofocus = value; return this; }

    /// <summary>How many columns the field spans in a multi-column layout.</summary>
    public BormFieldBuilder ColumnSpan(int span) { _field.ColumnSpan = span; return this; }

    /// <summary>
    /// Hides the visible label — for a control the surrounding page already names. The label is still
    /// attached to the input as its accessible name, so the field never becomes anonymous.
    /// </summary>
    public BormFieldBuilder HideLabel(bool hide = true) { _field.ShowLabel = !hide; return this; }

    /// <summary>Static text rendered inside the control before the input — a currency symbol, an <c>@</c>.</summary>
    public BormFieldBuilder Prefix(string prefix) { _field.Prefix = prefix; return this; }

    /// <summary>Static text rendered after the input — a unit such as <c>kg</c> or <c>%</c>.</summary>
    public BormFieldBuilder Suffix(string suffix) { _field.Suffix = suffix; return this; }

    /// <summary>Shows a live "42 / 200" counter beneath a length-limited field.</summary>
    public BormFieldBuilder CharacterCount(bool show = true) { _field.ShowCharacterCount = show; return this; }

    /// <summary>
    /// Writes the value on every keystroke rather than on blur, optionally waiting
    /// <paramref name="debounceMilliseconds"/> after the last one. This is what makes a live preview,
    /// a computed total or as-you-type validation feel immediate.
    /// </summary>
    public BormFieldBuilder UpdateOnInput(int debounceMilliseconds = 0)
    {
        _field.UpdateOn = BormUpdateTrigger.Input;
        _field.DebounceMilliseconds = Math.Max(0, debounceMilliseconds);
        return this;
    }

    /// <summary>Writes the value on the browser's <c>change</c> event — for a text box, on blur.</summary>
    public BormFieldBuilder UpdateOnChange()
    {
        _field.UpdateOn = BormUpdateTrigger.Change;
        _field.DebounceMilliseconds = 0;
        return this;
    }

    /// <summary>
    /// Adds an HTML attribute to the rendered control — a <c>data-</c> hook, a <c>title</c>,
    /// <c>spellcheck="false"</c>. Attributes the renderer sets itself always win.
    /// </summary>
    public BormFieldBuilder InputAttr(string name, object? value)
    {
        _field.InputAttributes[name] = value;
        return this;
    }

    /// <summary>
    /// Offers values as <c>&lt;datalist&gt;</c> suggestions without restricting what may be typed —
    /// "the answers people usually give", as opposed to <see cref="Options(BormSelectOption[])"/>,
    /// which is a closed set.
    /// </summary>
    public BormFieldBuilder Suggest(params string[] suggestions)
    {
        foreach (var s in suggestions) _field.Suggestions.Add(s);
        return this;
    }

    // --- Type overrides ---
    public BormFieldBuilder As(BormFieldType type) { _field.Type = type; return this; }
    public BormFieldBuilder AsTextArea(int rows = 4) { _field.Type = BormFieldType.TextArea; _field.Attributes["rows"] = rows; return this; }
    public BormFieldBuilder AsEmail() => Email();
    public BormFieldBuilder AsPassword() { _field.Type = BormFieldType.Password; _field.Autocomplete ??= "current-password"; return this; }
    public BormFieldBuilder AsUrl() { _field.Type = BormFieldType.Url; return this; }
    public BormFieldBuilder AsTel() { _field.Type = BormFieldType.Tel; return this; }

    /// <summary>
    /// Renders the field as a search box. Browsers give one a clear affordance, a search key on the
    /// on-screen keyboard and history from previous searches; the value is plain text either way.
    /// </summary>
    public BormFieldBuilder AsSearch()
    {
        _field.Type = BormFieldType.Search;
        _field.InputMode ??= "search";
        return this;
    }
    public BormFieldBuilder AsColor() { _field.Type = BormFieldType.Color; return this; }

    /// <summary>
    /// A month picker — a card's expiry, a billing period. A <c>DateOnly</c> or <c>DateTime</c> holds
    /// the month's first day; with no CLR type the value is <c>yyyy-MM</c>. Browsers without a picker of
    /// their own show a text box, where anything else typed is reported rather than stored.
    /// </summary>
    public BormFieldBuilder AsMonth() { _field.Type = BormFieldType.Month; return this; }

    /// <summary>
    /// An ISO 8601 week picker — a timesheet, a delivery slot. A <c>DateOnly</c> or <c>DateTime</c>
    /// holds the week's Monday; with no CLR type the value is <c>yyyy-Www</c>.
    /// </summary>
    public BormFieldBuilder AsWeek() { _field.Type = BormFieldType.Week; return this; }
    public BormFieldBuilder AsRange() { _field.Type = BormFieldType.Range; return this; }
    public BormFieldBuilder AsRadio() { _field.Type = BormFieldType.Radio; return this; }
    public BormFieldBuilder AsMultiSelect() { _field.Type = BormFieldType.MultiSelect; return this; }
    public BormFieldBuilder AsSelect() { _field.Type = BormFieldType.Select; return this; }
    public BormFieldBuilder AsHidden() { _field.Type = BormFieldType.Hidden; return this; }
    public BormFieldBuilder AsFile() { _field.Type = BormFieldType.File; return this; }

    /// <summary>
    /// Renders the field as a combobox: a text box that filters the options as the user types. This is
    /// what a dropdown with more than a screenful of choices should have been all along — a
    /// <c>&lt;select&gt;</c> of 200 countries is navigated by scrolling, and a
    /// <see cref="Suggest(string[])"/> datalist proposes without ever restricting.
    /// </summary>
    /// <param name="allowCustom">
    /// Whether an answer that is on no list is accepted. False (the default) adds a rule reporting it,
    /// so the field is a closed choice; true makes the list a shortcut rather than a constraint, which
    /// is what a "city" box that must also accept somewhere obscure needs.
    /// </param>
    /// <remarks>
    /// The rule can only check options the schema itself declares. Choices that arrive through
    /// <see cref="OptionsFrom(BormOptionsProvider, string[])"/> live in the form's runtime state
    /// rather than in the schema, so they are not checked here — validate those on the server, where
    /// the same lookup lives.
    /// </remarks>
    public BormFieldBuilder AsCombobox(bool allowCustom = false)
    {
        _field.Type = BormFieldType.Combobox;
        if (allowCustom) _field.RemoveValidators("oneOf");
        else _field.AddValidator(new BormOneOfRule());
        return this;
    }

    /// <summary>
    /// Renders a collection of short strings as removable chips with one box to type the next into —
    /// labels, skills, recipients. The same data an <c>ArrayOf(..., Text)</c> holds; the difference is
    /// that a repeater gives every entry its own row with add, remove, duplicate and reorder buttons,
    /// which is the right shape for an invoice line and the wrong one for a word.
    /// </summary>
    /// <param name="max">An optional cap on how many tags may be added.</param>
    public BormFieldBuilder AsTags(int? max = null)
    {
        _field.Type = BormFieldType.Tags;
        if (max is { } limit) Items(_field.MinItems, limit);
        return this;
    }

    /// <summary>
    /// Adds a "show the characters" toggle to a password field. Opt-in, because whether a password may
    /// be revealed at all is a decision about the environment the form runs in — a shared screen, a
    /// recorded session — rather than about the field.
    /// </summary>
    public BormFieldBuilder Revealable(bool value = true)
    {
        _field.Type = BormFieldType.Password;
        _field.Attributes["reveal"] = value;
        return this;
    }

    /// <summary>
    /// Adds a one-click "empty this field" button, shown only while there is something to clear. Worth
    /// it on a touch screen, where select-all-and-delete is real work.
    /// </summary>
    public BormFieldBuilder Clearable(bool value = true)
    {
        _field.Attributes["clearable"] = value;
        return this;
    }

    /// <summary>Renders the checkbox as a switch — the same control, announced as "on/off".</summary>
    public BormFieldBuilder AsSwitch()
    {
        _field.Type = BormFieldType.Checkbox;
        _field.Attributes["switch"] = true;
        return this;
    }

    // --- Options ---
    public BormFieldBuilder Options(params BormSelectOption[] options)
    {
        foreach (var o in options) _field.Options.Add(o);
        EnsureChoiceType();
        return this;
    }

    public BormFieldBuilder Options(params (string Value, string Label)[] options)
        => Options(options.Select(o => new BormSelectOption(o.Value, o.Label)).ToArray());

    /// <summary>Adds options from an enum type's members, honouring <c>[Display]</c> / <c>[Description]</c>.</summary>
    public BormFieldBuilder OptionsFromEnum(Type enumType)
    {
        foreach (var option in BormEnumOptions.For(enumType))
            _field.Options.Add(option);
        EnsureChoiceType();
        return this;
    }

    /// <summary>Adds options from an enum type's members.</summary>
    public BormFieldBuilder OptionsFromEnum<TEnum>() where TEnum : struct, Enum => OptionsFromEnum(typeof(TEnum));

    /// <summary>
    /// Loads the options on demand — from a service, or from another field's current value to build a
    /// cascading select. List <paramref name="dependsOn"/> paths so the options reload (and the stale
    /// selection clears) whenever one of them changes.
    /// </summary>
    public BormFieldBuilder OptionsFrom(BormOptionsProvider provider, params string[] dependsOn)
    {
        _field.OptionsProvider = provider;
        foreach (var d in dependsOn) _field.OptionsDependencies.Add(d);
        EnsureChoiceType();
        return this;
    }

    /// <summary>
    /// Makes the field a combobox that searches as the user types — a customer, a product, an address
    /// out of more than any list could hold. <paramref name="search"/> is given what was typed in
    /// <see cref="BormOptionsContext.SearchText"/> once it is <paramref name="minLength"/> characters
    /// long and the user pauses for <paramref name="debounceMilliseconds"/>; a search the user has typed
    /// past is cancelled, and its answer never lands. When the field opens holding a value, the search
    /// is asked once with no text and that value in <see cref="BormOptionsContext.CurrentValue"/>, so it
    /// can return the option the box should show.
    /// </summary>
    /// <param name="search">Returns the options matching the text (or the one option for the current value).</param>
    /// <param name="minLength">How many characters have to be typed before a search runs.</param>
    /// <param name="debounceMilliseconds">How long the user has to pause before it does.</param>
    /// <param name="allowCustom">
    /// Whether an answer that matches no result is accepted as it was typed. By default it is refused —
    /// left in the box with a message, while the model keeps its last value — because a search field
    /// stands for a record, and text that names none is not an answer.
    /// </param>
    /// <param name="dependsOn">Paths whose change empties the field and starts the search again.</param>
    public BormFieldBuilder SearchFrom(BormOptionsProvider search, int minLength = 1, int debounceMilliseconds = 250,
        bool allowCustom = false, params string[] dependsOn)
    {
        ArgumentNullException.ThrowIfNull(search);
        ArgumentOutOfRangeException.ThrowIfNegative(minLength);
        _field.Type = BormFieldType.Combobox;
        _field.OptionsProvider = search;
        _field.SearchMinLength = minLength;
        _field.SearchDebounceMilliseconds = Math.Max(0, debounceMilliseconds);
        foreach (var d in dependsOn) _field.OptionsDependencies.Add(d);
        if (allowCustom) _field.RemoveValidators("oneOf");
        else _field.AddValidator(new BormOneOfRule());
        return this;
    }

    /// <summary>
    /// <see cref="SearchFrom(BormOptionsProvider, int, int, bool, string[])"/>, projecting the items a
    /// lookup returns: <c>SearchFrom(ctx =&gt; customers.SearchAsync(ctx.SearchText, ctx.CancellationToken), c =&gt; c.Id, c =&gt; c.Name)</c>.
    /// </summary>
    public BormFieldBuilder SearchFrom<TItem>(
        Func<BormOptionsContext, ValueTask<IEnumerable<TItem>>> search,
        Func<TItem, string> value,
        Func<TItem, string> label,
        int minLength = 1,
        int debounceMilliseconds = 250,
        bool allowCustom = false,
        params string[] dependsOn)
        => SearchFrom(async ctx => (await search(ctx)).Select(i => new BormSelectOption(value(i), label(i))).ToList(),
            minLength, debounceMilliseconds, allowCustom, dependsOn);

    /// <summary>
    /// Derives the field's value from the rest of the form — a line total, a full name, a price after
    /// discount. List <paramref name="dependsOn"/> paths so the value refreshes when they change;
    /// leave it empty to recompute on any change. Combine with <see cref="ReadOnly"/> when the user
    /// should not be able to override the result.
    /// </summary>
    public BormFieldBuilder Computed(BormComputedValue compute, params string[] dependsOn)
    {
        _field.Computed = compute;
        foreach (var d in dependsOn) _field.ComputedDependencies.Add(d);
        return this;
    }

    /// <summary>
    /// Derives the field's value from an expression — <c>{Quantity} * {UnitPrice}</c>,
    /// <c>{First} + ' ' + {Last}</c>, <c>sum({Lines[].Total})</c>. The dependencies are read from the
    /// expression itself, so the value refreshes exactly when a field it names changes, and the
    /// field is made read-only, since anything typed into it would be overwritten. Unlike a delegate,
    /// an expression is plain text: it is exported to JSON Schema as <c>x-computed</c> and imported back.
    /// </summary>
    /// <exception cref="BormExpressionException">The text is not a valid expression.</exception>
    public BormFieldBuilder Computed(string expression)
    {
        var parsed = BormExpression.Parse(expression);
        _field.Computed = parsed.ToComputed();
        _field.ComputedDependencies.Clear();
        foreach (var d in parsed.Dependencies) _field.ComputedDependencies.Add(d);
        _field.ReadOnly = true;
        return this;
    }

    /// <summary>
    /// Runs when this field's value changes — the schema's own "and when this changes, do that".
    /// Clearing the city when the country changes, resetting a seat count when the plan does: the
    /// things that fit neither <see cref="Computed(BormComputedValue, string[])"/> (which owns its field's value outright) nor
    /// <see cref="OptionsFrom(BormOptionsProvider, string[])"/> (which reloads choices), because
    /// what the handler writes is still the user's to edit afterwards.
    /// </summary>
    /// <remarks>
    /// Paths inside the handler resolve relative to the object that owns the field, exactly as
    /// conditions and computed dependencies do, so a handler on a repeater's item template means
    /// <em>that row</em>. It is not run while the form is being constructed — applying a default is not
    /// a change the user made.
    /// </remarks>
    public BormFieldBuilder OnChange(BormChangeHandler handler)
    {
        _field.OnChanged = handler;
        return this;
    }

    /// <summary>
    /// Tidies the value once the user has finished entering it — on blur, and once more for every
    /// field on submit. React Hook Form spells this <c>setValueAs</c>.
    /// </summary>
    /// <remarks>
    /// It is the answer to a rule that rejects <c>" a@b.com "</c> for its spaces, which is technically
    /// correct and of no use to anyone: fix the value, not the complaint. Not run per keystroke, which
    /// would eat the space between two words the moment it was typed.
    /// </remarks>
    public BormFieldBuilder Normalize(BormValueNormalizer normalize)
    {
        _field.Normalize = normalize;
        return this;
    }

    /// <summary>
    /// Trims surrounding whitespace from a text value when the user leaves the field. Pasted values
    /// carry it in constantly, and it is invisible in the box and fatal in a lookup key.
    /// </summary>
    /// <param name="emptyBecomesNull">
    /// Whether a value that is nothing but whitespace becomes null rather than <c>""</c>. True by
    /// default: "  " is not an answer, and storing it makes a required field pass on a blank one.
    /// </param>
    public BormFieldBuilder Trim(bool emptyBecomesNull = true)
        => Normalize(value =>
        {
            if (value is not string s) return value;
            var trimmed = s.Trim();
            return trimmed.Length == 0 && emptyBecomesNull ? null : trimmed;
        });

    /// <summary>
    /// Maps a sequence of items to options, for the common "load a list and project it" case.
    /// </summary>
    /// <remarks>
    /// <typeparamref name="TItem"/> is inferred from <paramref name="load"/>, which means the loader
    /// must be typed as <c>IEnumerable&lt;TItem&gt;</c>; a source returning a concrete
    /// <c>List&lt;T&gt;</c> needs an explicit type argument (<c>OptionsFrom&lt;City&gt;(…)</c>), or use
    /// the <see cref="OptionsFrom(BormOptionsProvider, string[])" /> overload and project yourself.
    /// </remarks>
    public BormFieldBuilder OptionsFrom<TItem>(
        Func<BormOptionsContext, ValueTask<IEnumerable<TItem>>> load,
        Func<TItem, string> value,
        Func<TItem, string> label,
        params string[] dependsOn)
        => OptionsFrom(async ctx =>
        {
            var items = await load(ctx);
            return items.Select(i => new BormSelectOption(value(i), label(i))).ToList();
        }, dependsOn);

    // --- Constraints ---
    public BormFieldBuilder Required(string? message = null)
    {
        _field.Required = true;
        _field.AddValidator(new BormRequiredRule(message));
        return this;
    }

    /// <summary>
    /// Makes the field required only while <paramref name="condition"/> holds — the declarative form of
    /// "required when country is US".
    /// </summary>
    public BormFieldBuilder RequiredWhen(IBormCondition condition)
    {
        _field.RequiredWhen = condition;
        return this;
    }

    /// <inheritdoc cref="RequiredWhen(IBormCondition)" />
    public BormFieldBuilder RequiredWhen(string field, BormConditionOperator op, object? value = null)
        => RequiredWhen(new BormFieldCondition(field, op, value));

    /// <summary>Makes the field required while an expression holds — <c>{Country} = 'US' and {Amount} &gt; 1000</c>.</summary>
    /// <exception cref="BormExpressionException">The text is not a valid expression.</exception>
    public BormFieldBuilder RequiredWhen(string expression) => RequiredWhen(new BormExpressionCondition(expression));

    public BormFieldBuilder MinLength(int min, string? message = null)
    {
        _field.MinLength = min;
        _field.AddValidator(new BormMinLengthRule(min, message));
        return this;
    }

    public BormFieldBuilder MaxLength(int max, string? message = null)
    {
        _field.MaxLength = max;
        _field.AddValidator(new BormMaxLengthRule(max, message));
        return this;
    }

    /// <summary>Sets both length bounds in one call.</summary>
    public BormFieldBuilder Length(int min, int max, string? message = null)
        => MinLength(min, message).MaxLength(max, message);

    public BormFieldBuilder Range(double? min, double? max, string? message = null)
    {
        _field.Min = min;
        _field.Max = max;
        _field.AddValidator(new BormRangeRule(min, max, message));
        return this;
    }

    /// <summary>
    /// Restricts a date field to a window — "not before today", "no later than the end of the year".
    /// </summary>
    /// <remarks>
    /// The bounds are stored the way the rest of the library stores date bounds (as OLE automation
    /// numbers), so they are enforced by the same rule and rendered as the input's own <c>min</c> and
    /// <c>max</c> — which is what stops the browser's picker from offering a date the form will then
    /// refuse. This is the readable form of what <c>[Range(typeof(DateTime), …)]</c> expresses on a model.
    /// </remarks>
    public BormFieldBuilder Range(DateTime? min, DateTime? max, string? message = null)
        => Range(min?.ToOADate(), max?.ToOADate(), message);

    /// <inheritdoc cref="Range(DateTime?, DateTime?, string?)" />
    public BormFieldBuilder Range(DateOnly? min, DateOnly? max, string? message = null)
        => Range(min?.ToDateTime(TimeOnly.MinValue), max?.ToDateTime(TimeOnly.MinValue), message);

    /// <summary>
    /// Restricts a time field to a window — "between 09:00 and 17:00". A time of day is stored as the
    /// fraction of a day it stands for, which is the same scale the date bounds use.
    /// </summary>
    public BormFieldBuilder Range(TimeOnly? min, TimeOnly? max, string? message = null)
        => Range(min?.ToTimeSpan().TotalDays, max?.ToTimeSpan().TotalDays, message);

    public BormFieldBuilder Step(double step) { _field.NumericStep = step; return this; }

    /// <summary>Requires the value to be an exact multiple of <paramref name="factor"/> (JSON Schema's <c>multipleOf</c>).</summary>
    public BormFieldBuilder MultipleOf(double factor, string? message = null)
    {
        _field.NumericStep ??= factor;
        _field.AddValidator(new BormMultipleOfRule(factor, message));
        return this;
    }

    public BormFieldBuilder Pattern(string pattern, string? message = null)
    {
        _field.Pattern = pattern;
        _field.AddValidator(new BormPatternRule(pattern, message));
        return this;
    }

    public BormFieldBuilder Email(string? message = null)
    {
        _field.Type = BormFieldType.Email;
        _field.Autocomplete ??= "email";
        _field.AddValidator(new BormEmailRule(message));
        return this;
    }

    /// <summary>Marks the field as a URL and validates that it parses as an absolute one.</summary>
    public BormFieldBuilder Url(string? message = null)
    {
        _field.Type = BormFieldType.Url;
        _field.AddValidator(new BormUrlRule(message));
        return this;
    }

    /// <summary>Requires this field to equal the value at another path — the "confirm password" rule.</summary>
    /// <remarks>
    /// The other path is registered as a revalidation dependency, so fixing the <em>password</em>
    /// clears the confirmation's "these do not match" without the user having to go back and touch a
    /// box that was already right. See <see cref="RevalidateOn"/>.
    /// </remarks>
    public BormFieldBuilder MatchesField(string otherPath, string? otherLabel = null, string? message = null)
    {
        _field.AddValidator(new BormCompareRule(otherPath, otherLabel, message));
        return RevalidateOn(otherPath);
    }

    /// <summary>
    /// Revalidates this field whenever one of <paramref name="paths"/> changes — for a rule that reads
    /// another field's value and would otherwise go stale when <em>that</em> field is the one corrected.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A cross-field rule lives on one of the two fields it compares, so only one of the two changes
    /// ever runs it. Declaring the other here closes the gap: React Hook Form spells it <c>deps</c>,
    /// TanStack Form spells it <c>onChangeListenTo</c>. <see cref="MatchesField"/> registers its own
    /// dependency, so a confirm-password field needs nothing extra.
    /// </para>
    /// <para>
    /// Paths resolve relative to the object that owns the field before falling back to the root, so a
    /// rule on a repeater's item template can name a sibling and mean that row. A dependent that has
    /// not been touched on a form that has not been submitted is left alone, so this can never make a
    /// field the user has never reached start showing errors.
    /// </para>
    /// </remarks>
    public BormFieldBuilder RevalidateOn(params string[] paths)
    {
        foreach (var path in paths)
        {
            if (string.IsNullOrEmpty(path) || _field.RevalidateOn.Contains(path)) continue;
            _field.RevalidateOn.Add(path);
        }
        return this;
    }

    /// <summary>Limits the files that may be chosen to the listed extensions and media types.</summary>
    /// <remarks>
    /// Size and type are checked by one rule, which this rebuilds — keeping the size limit and any
    /// message <see cref="MaxFileSize"/> was given, so the two can be called in either order.
    /// </remarks>
    public BormFieldBuilder Accept(string accept, bool multiple = false, string? message = null)
    {
        _field.Type = BormFieldType.File;
        _field.Accept = accept;
        _field.Multiple = multiple;
        _field.AddValidator(new BormFileRule(_field.MaxFileSize, accept,
            sizeMessage: CurrentFileRule()?.SizeMessage, typeMessage: message));
        return this;
    }

    /// <summary>Limits each selected file's size, in bytes.</summary>
    /// <remarks>
    /// <paramref name="message"/> is said only about a file that is too large; a file of the wrong type
    /// keeps the message <see cref="Accept"/> gave it, whichever of the two was called first.
    /// </remarks>
    public BormFieldBuilder MaxFileSize(long bytes, string? message = null)
    {
        _field.Type = BormFieldType.File;
        _field.MaxFileSize = bytes;
        _field.AddValidator(new BormFileRule(bytes, _field.Accept,
            sizeMessage: message, typeMessage: CurrentFileRule()?.TypeMessage));
        return this;
    }

    private BormFileRule? CurrentFileRule() => _field.Validators.OfType<BormFileRule>().FirstOrDefault();

    public BormFieldBuilder Items(int? min = null, int? max = null, string? message = null)
    {
        _field.MinItems = min;
        _field.MaxItems = max;
        _field.AddValidator(new BormCollectionSizeRule(min, max, message));
        return this;
    }

    /// <summary>
    /// Creates the minimum number of rows up front, so a repeater whose schema says "at least one line"
    /// opens with a line rather than with an error about not having one.
    /// </summary>
    /// <remarks>
    /// Opt-in rather than implied by <see cref="Items"/>: on an edit form loaded from storage, "this
    /// record has no lines" can be the truth, and inventing one would be the library editing the user's
    /// data on their behalf.
    /// </remarks>
    public BormFieldBuilder SeedMinItems(bool value = true)
    {
        _field.Attributes["seedMinItems"] = value;
        return this;
    }

    /// <summary>Requires every item in an array field to be distinct.</summary>
    public BormFieldBuilder UniqueItems(string? message = null)
    {
        _field.AddValidator(new BormUniqueItemsRule(message));
        return this;
    }

    // --- Custom validation ---
    public BormFieldBuilder Validate(IBormValidationRule rule) { _field.AddValidator(rule); return this; }

    /// <summary>
    /// Restricts the rules added by <paramref name="configure"/> to the times
    /// <paramref name="condition"/> holds — FluentValidation's <c>.When(…)</c>, for a field that is
    /// visible either way but only constrained sometimes.
    /// </summary>
    public BormFieldBuilder When(IBormCondition condition, Action<BormFieldBuilder> configure)
        => When(ctx => condition.Evaluate(ctx.Data), configure);

    /// <inheritdoc cref="When(IBormCondition, Action{BormFieldBuilder})" />
    public BormFieldBuilder When(string field, BormConditionOperator op, object? value, Action<BormFieldBuilder> configure)
        => When(new BormFieldCondition(field, op, value), configure);

    /// <inheritdoc cref="When(IBormCondition, Action{BormFieldBuilder})" />
    /// <exception cref="BormExpressionException"><paramref name="expression"/> is not a valid expression.</exception>
    public BormFieldBuilder When(string expression, Action<BormFieldBuilder> configure)
        => When(new BormExpressionCondition(expression), configure);

    /// <inheritdoc cref="When(IBormCondition, Action{BormFieldBuilder})" />
    public BormFieldBuilder When(Func<BormValidationContext, bool> condition, Action<BormFieldBuilder> configure)
    {
        // The rules are collected on a scratch field, then re-added wrapped, so the caller writes the
        // same fluent chain inside `When` as outside it.
        var scratch = new BormFieldDefinition(_field.Name, _field.Type) { ValueType = _field.ValueType };
        configure(new BormFieldBuilder(scratch));

        foreach (var rule in scratch.Validators)
            _field.Validators.Add(new BormConditionalRule(rule, condition));

        return this;
    }

    public BormFieldBuilder Must(Func<object?, bool> predicate, string message)
    {
        _field.Validators.Add(new BormDelegateRule(ctx => predicate(ctx.Value)
            ? BormRuleResult.Success() : BormRuleResult.Fail(message)));
        return this;
    }

    public BormFieldBuilder Must(Func<BormValidationContext, bool> predicate, string message)
    {
        _field.Validators.Add(new BormDelegateRule(ctx => predicate(ctx)
            ? BormRuleResult.Success() : BormRuleResult.Fail(message)));
        return this;
    }

    public BormFieldBuilder MustAsync(Func<BormValidationContext, ValueTask<bool>> predicate, string message)
    {
        _field.Validators.Add(new BormAsyncDelegateRule(async ctx => await predicate(ctx)
            ? BormRuleResult.Success() : BormRuleResult.Fail(message)));
        return this;
    }

    /// <summary>
    /// Requires an expression to hold — <c>{End} &gt;= {Start}</c>, <c>len({Code}) = 6</c>. Paths resolve
    /// against the object that owns the field, so on a repeater's row template the rule means that
    /// row. Every other field the expression names is registered with <see cref="RevalidateOn"/>, so
    /// correcting <em>that</em> field clears this one's message too.
    /// </summary>
    /// <remarks>
    /// The rule fails only when the expression is false: while a value it compares is missing it has
    /// nothing to say, and <see cref="Required"/> is what says a value is missing. Unlike a delegate it
    /// is plain text, exported to JSON Schema as <c>x-rules</c> and imported back.
    /// </remarks>
    /// <exception cref="BormExpressionException"><paramref name="expression"/> is not a valid expression.</exception>
    public BormFieldBuilder Must(string expression, string message, BormValidationSeverity severity = BormValidationSeverity.Error)
    {
        var rule = new BormExpressionRule(expression, message, severity);
        _field.Validators.Add(rule);
        foreach (var dependency in rule.Expression.Dependencies)
            if (!string.Equals(dependency, _field.Name, StringComparison.OrdinalIgnoreCase)) RevalidateOn(dependency);
        return this;
    }

    // --- Conditional behaviour ---
    public BormFieldBuilder VisibleWhen(IBormCondition condition) { _field.VisibleWhen = condition; return this; }

    public BormFieldBuilder VisibleWhen(string field, BormConditionOperator op, object? value = null)
        => VisibleWhen(new BormFieldCondition(field, op, value));

    public BormFieldBuilder VisibleWhen(Func<IBormDataReader, bool> predicate, params string[] dependencies)
        => VisibleWhen(new BormPredicateCondition(predicate, dependencies));

    /// <summary>
    /// Shows the field only while an expression holds — <c>{IsBusiness} and {Employees} &gt; 10</c>. The
    /// fields it reads are taken from the expression, so it is re-evaluated exactly when one changes,
    /// and it survives a round trip through JSON as a string in <c>x-visibleWhen</c>.
    /// </summary>
    /// <exception cref="BormExpressionException">The text is not a valid expression.</exception>
    public BormFieldBuilder VisibleWhen(string expression) => VisibleWhen(new BormExpressionCondition(expression));

    public BormFieldBuilder DisabledWhen(IBormCondition condition) { _field.DisabledWhen = condition; return this; }

    public BormFieldBuilder DisabledWhen(string field, BormConditionOperator op, object? value = null)
        => DisabledWhen(new BormFieldCondition(field, op, value));

    public BormFieldBuilder DisabledWhen(Func<IBormDataReader, bool> predicate, params string[] dependencies)
        => DisabledWhen(new BormPredicateCondition(predicate, dependencies));

    /// <summary>Disables the field while an expression holds.</summary>
    /// <exception cref="BormExpressionException">The text is not a valid expression.</exception>
    public BormFieldBuilder DisabledWhen(string expression) => DisabledWhen(new BormExpressionCondition(expression));

    /// <summary>
    /// Locks the field while <paramref name="condition"/> holds — an answer that is settled rather than
    /// one that does not apply.
    /// </summary>
    /// <remarks>
    /// Reach for this rather than <see cref="DisabledWhen(IBormCondition)"/> whenever the value
    /// still matters to the person reading the form. A disabled control leaves the tab order and is not
    /// announced at all, so "the invoice has been sent, the total is final" turns into a number a
    /// keyboard or screen-reader user cannot get to; read-only keeps it reachable, readable and
    /// copyable, which is what "you may look but not change" means.
    /// </remarks>
    public BormFieldBuilder ReadOnlyWhen(IBormCondition condition) { _field.ReadOnlyWhen = condition; return this; }

    /// <inheritdoc cref="ReadOnlyWhen(IBormCondition)" />
    public BormFieldBuilder ReadOnlyWhen(string field, BormConditionOperator op, object? value = null)
        => ReadOnlyWhen(new BormFieldCondition(field, op, value));

    /// <inheritdoc cref="ReadOnlyWhen(IBormCondition)" />
    public BormFieldBuilder ReadOnlyWhen(Func<IBormDataReader, bool> predicate, params string[] dependencies)
        => ReadOnlyWhen(new BormPredicateCondition(predicate, dependencies));

    /// <inheritdoc cref="ReadOnlyWhen(IBormCondition)" />
    /// <exception cref="BormExpressionException">The text is not a valid expression.</exception>
    public BormFieldBuilder ReadOnlyWhen(string expression) => ReadOnlyWhen(new BormExpressionCondition(expression));

    /// <summary>
    /// Clears the value as soon as the field becomes hidden, so an abandoned branch of the form does
    /// not submit values the user can no longer see.
    /// </summary>
    public BormFieldBuilder ClearOnHide(bool value = true) { _field.ClearOnHide = value; return this; }

    /// <summary>
    /// Gives a field that has just been handed options a control that can show them — unless it already
    /// has one. A combobox is a choice field too: it renders its options as a filtered list, so
    /// declaring options on one must not silently turn it back into a dropdown.
    /// </summary>
    private void EnsureChoiceType()
    {
        if (_field.IsChoice) return;

        // A schema-only field declared as a number and then given choices is still a number: the
        // choices are a closed set of them. Without a value type the selection was stored as the
        // option's text, so `Integer("seats").Options(…)` held "2" rather than 2, and it exported as a
        // string enum that came back as one. A typed model already supplies its own.
        _field.ValueType ??= _field.Type switch
        {
            BormFieldType.Integer => typeof(long?),
            BormFieldType.Number or BormFieldType.Range => typeof(double?),
            BormFieldType.Checkbox => typeof(bool?),
            _ => null
        };
        _field.Type = BormFieldType.Select;
    }

    internal static string Humanize(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var sb = new System.Text.StringBuilder(name.Length + 4);
        sb.Append(char.ToUpperInvariant(name[0]));
        for (var i = 1; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && (char.IsLower(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1]))))
                sb.Append(' ');
            sb.Append(c);
        }
        return sb.ToString();
    }
}
