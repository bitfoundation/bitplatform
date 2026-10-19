namespace Bit.Borm;

/// <summary>
/// Untyped, schema-first form builder. Use this when you have no compiled model (e.g. when
/// assembling a form by hand or augmenting a JSON-imported schema).
/// </summary>
public class BormBuilder
{
    /// <summary>The definition being assembled. Derived builders add to it directly.</summary>
    protected BormDefinition Form { get; } = new();

    public static BormBuilder Create() => new();

    /// <summary>Starts a strongly-typed builder bound to <typeparamref name="TModel"/>.</summary>
    public static BormBuilder<TModel> For<TModel>() where TModel : class => new();

    public BormBuilder Title(string title) { Form.Title = title; return this; }
    public BormBuilder Description(string description) { Form.Description = description; return this; }

    /// <summary>Lays the form out in <paramref name="columns"/> responsive columns.</summary>
    public BormBuilder Columns(int columns) { Form.Columns = Math.Max(1, columns); return this; }

    /// <summary>Adds a field of the given type and configures it.</summary>
    public BormBuilder Field(string name, BormFieldType type, Action<BormFieldBuilder>? configure = null)
    {
        var def = new BormFieldDefinition(name, type) { Label = BormFieldBuilder.Humanize(name) };
        configure?.Invoke(new BormFieldBuilder(def));
        Add(def);
        return this;
    }

    public BormBuilder Text(string name, Action<BormFieldBuilder>? configure = null) => Field(name, BormFieldType.Text, configure);
    public BormBuilder TextArea(string name, Action<BormFieldBuilder>? configure = null) => Field(name, BormFieldType.TextArea, configure);
    public BormBuilder Email(string name, Action<BormFieldBuilder>? configure = null) => Field(name, BormFieldType.Email, configure);
    public BormBuilder Password(string name, Action<BormFieldBuilder>? configure = null) => Field(name, BormFieldType.Password, configure);
    public BormBuilder Number(string name, Action<BormFieldBuilder>? configure = null) => Field(name, BormFieldType.Number, configure);
    public BormBuilder Integer(string name, Action<BormFieldBuilder>? configure = null) => Field(name, BormFieldType.Integer, configure);
    public BormBuilder Checkbox(string name, Action<BormFieldBuilder>? configure = null) => Field(name, BormFieldType.Checkbox, configure);
    public BormBuilder Select(string name, Action<BormFieldBuilder>? configure = null) => Field(name, BormFieldType.Select, configure);
    public BormBuilder Radio(string name, Action<BormFieldBuilder>? configure = null) => Field(name, BormFieldType.Radio, configure);
    public BormBuilder MultiSelect(string name, Action<BormFieldBuilder>? configure = null) => Field(name, BormFieldType.MultiSelect, configure);
    public BormBuilder Date(string name, Action<BormFieldBuilder>? configure = null) => Field(name, BormFieldType.Date, configure);

    /// <summary>A text box that filters its options as the user types. See <see cref="BormFieldBuilder.AsCombobox"/>.</summary>
    public BormBuilder Combobox(string name, Action<BormFieldBuilder>? configure = null)
        => Field(name, BormFieldType.Combobox, configure);

    /// <summary>A list of short values shown as removable chips. See <see cref="BormFieldBuilder.AsTags"/>.</summary>
    public BormBuilder Tags(string name, Action<BormFieldBuilder>? configure = null)
        => Field(name, BormFieldType.Tags, configure);

    public BormBuilder File(string name, Action<BormFieldBuilder>? configure = null) => Field(name, BormFieldType.File, configure);

    /// <summary>
    /// Adds presentational content — a section heading and optional paragraph — that binds to no value
    /// and is never validated. A long form reads as a set of titled sections instead of one
    /// undifferentiated column, without the page having to interleave its own markup with the fields.
    /// </summary>
    /// <param name="name">A unique key. It names nothing in the data; it only has to be distinct.</param>
    /// <param name="heading">Section heading. Pass null for a paragraph or divider on its own.</param>
    /// <param name="text">Explanatory text below the heading.</param>
    /// <param name="configure">Further configuration — a divider, a heading level, a column span.</param>
    public BormBuilder Static(string name, string? heading = null, string? text = null,
        Action<BormFieldBuilder>? configure = null)
        => Field(name, BormFieldType.Static, f =>
        {
            if (heading is not null) f.Label(heading); else f.HideLabel();
            if (text is not null) f.Help(text);
            configure?.Invoke(f);
        });

    /// <summary>Adds an object (nested group) field.</summary>
    public BormBuilder Object(string name, Action<BormBuilder> children, Action<BormFieldBuilder>? configure = null)
    {
        Add(BuildObject(name, children, configure));
        return this;
    }

    /// <summary>Adds a repeating array field whose items are described by <paramref name="item"/>.</summary>
    public BormBuilder Array(string name, Action<BormBuilder> item, Action<BormFieldBuilder>? configure = null)
    {
        Add(BuildArray(name, item, configure));
        return this;
    }

    /// <summary>
    /// Adds an array of scalars (strings, numbers, …) rather than of objects — the shape JSON Schema
    /// writes as <c>{"type":"array","items":{"type":"string"}}</c>.
    /// </summary>
    public BormBuilder ArrayOf(string name, BormFieldType itemType, Action<BormFieldBuilder>? configure = null,
        Action<BormFieldBuilder>? configureItem = null)
    {
        Add(BuildScalarArray(name, itemType, configure, configureItem));
        return this;
    }

    /// <summary>Defines a wizard step.</summary>
    public BormBuilder Step(string id, Action<BormStepBuilder> configure)
    {
        var step = new BormStep(id);
        configure(new BormStepBuilder(step));
        Form.Steps.Add(step);
        return this;
    }

    /// <summary>
    /// Adds a rule validating the form as a whole. Use <see cref="BormRuleResult.FailFor"/> inside
    /// the rule to attach the message to the field the user should fix.
    /// </summary>
    public BormBuilder Validate(IBormValidationRule rule)
    {
        Form.Validators.Add(rule);
        return this;
    }

    /// <summary>
    /// Adds a cross-field rule over the whole form. <paramref name="fieldPath"/> chooses which field
    /// the message appears under; leave it null for a form-level message.
    /// </summary>
    public BormBuilder MustAll(Func<IBormDataReader, bool> predicate, string message, string? fieldPath = null)
        => Validate(new BormDelegateRule(ctx => predicate(ctx.Data)
            ? BormRuleResult.Success()
            : fieldPath is null ? BormRuleResult.Fail(message) : BormRuleResult.FailFor(fieldPath, message)));

    public BormDefinition Build() => Form;

    // ---------------------------------------------------------------- internals

    /// <summary>
    /// Appends a field, rejecting a duplicate name. Two siblings with the same name would resolve to
    /// the same data path and silently overwrite each other's value.
    /// </summary>
    protected void Add(BormFieldDefinition field)
    {
        if (Form.Fields.Any(f => string.Equals(f.Name, field.Name, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"A field named '{field.Name}' has already been added to this form.", nameof(field));
        Form.Fields.Add(field);
    }

    protected static BormFieldDefinition BuildObject(
        string name, Action<BormBuilder> children, Action<BormFieldBuilder>? configure)
    {
        var def = new BormFieldDefinition(name, BormFieldType.Object) { Label = BormFieldBuilder.Humanize(name) };
        var childBuilder = new BormBuilder();
        children(childBuilder);
        foreach (var c in childBuilder.Form.Fields) def.Children.Add(c);
        configure?.Invoke(new BormFieldBuilder(def));
        return def;
    }

    protected static BormFieldDefinition BuildArray(
        string name, Action<BormBuilder> item, Action<BormFieldBuilder>? configure)
    {
        var def = new BormFieldDefinition(name, BormFieldType.Array) { Label = BormFieldBuilder.Humanize(name) };
        var itemBuilder = new BormBuilder();
        item(itemBuilder);
        var template = new BormFieldDefinition("item", BormFieldType.Object);
        foreach (var c in itemBuilder.Form.Fields) template.Children.Add(c);
        def.ItemTemplate = template;
        configure?.Invoke(new BormFieldBuilder(def));
        return def;
    }

    protected static BormFieldDefinition BuildScalarArray(
        string name, BormFieldType itemType,
        Action<BormFieldBuilder>? configure, Action<BormFieldBuilder>? configureItem)
    {
        var def = new BormFieldDefinition(name, BormFieldType.Array) { Label = BormFieldBuilder.Humanize(name) };
        var template = new BormFieldDefinition("item", itemType);
        configureItem?.Invoke(new BormFieldBuilder(template));
        def.ItemTemplate = template;
        configure?.Invoke(new BormFieldBuilder(def));
        return def;
    }
}
