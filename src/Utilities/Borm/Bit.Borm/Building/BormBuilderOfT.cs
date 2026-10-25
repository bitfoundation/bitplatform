using System.Linq.Expressions;
using System.Reflection;

namespace Bit.Borm;

/// <summary>
/// Strongly-typed form builder. Fields are selected with lambda expressions so names and value
/// types are inferred and refactor-safe, in the spirit of React Hook Form + Zod.
/// </summary>
public sealed class BormBuilder<TModel> : BormBuilder where TModel : class
{
    public BormBuilder() => Form.ModelType = typeof(TModel);

    public new BormBuilder<TModel> Title(string title) { Form.Title = title; return this; }
    public new BormBuilder<TModel> Description(string description) { Form.Description = description; return this; }
    public new BormBuilder<TModel> Version(string version) { Form.Version = version; return this; }

    /// <inheritdoc cref="BormBuilder.Translate" />
    public new BormBuilder<TModel> Translate(string culture, IEnumerable<KeyValuePair<string, string>> texts) { base.Translate(culture, texts); return this; }
    public new BormBuilder<TModel> Columns(int columns) { base.Columns(columns); return this; }

    /// <inheritdoc cref="BormBuilder.Static" />
    public new BormBuilder<TModel> Static(string name, string? heading = null, string? text = null,
        Action<BormFieldBuilder>? configure = null)
    {
        base.Static(name, heading, text, configure);
        return this;
    }

    /// <inheritdoc cref="BormBuilder.Review" />
    public new BormBuilder<TModel> Review(string name, string? heading = null, string? text = null,
        Action<BormFieldBuilder>? configure = null)
    {
        base.Review(name, heading, text, configure);
        return this;
    }

    /// <summary>
    /// Adds a field selected by expression, inferring its name and type from the model. Nested members
    /// are supported — <c>x =&gt; x.Address.City</c> produces a field bound to the path
    /// <c>Address.City</c>, so a single flat form can reach into an object graph.
    /// </summary>
    public BormBuilder<TModel> Field<TValue>(
        Expression<Func<TModel, TValue>> selector,
        Action<BormFieldBuilder>? configure = null)
    {
        var (path, member) = ResolvePath(selector);
        var valueType = member is PropertyInfo p ? p.PropertyType : typeof(TValue);

        var fieldType = BormFieldTypeResolver.Resolve(valueType);
        var def = new BormFieldDefinition(path, fieldType)
        {
            ValueType = valueType,
            Label = BormFieldBuilder.Humanize(member.Name),
            // As the generator does: a value that can only be changed through a setter it has no public
            // one of is shown, never edited — the accessor would refuse the write anyway. A class-typed
            // group or list is edited in place and stays editable whatever its setter.
            ReadOnly = member is PropertyInfo property && BormModelDataAccessor.IsReadOnlyForForm(property, fieldType)
        };

        var builder = new BormFieldBuilder(def);

        var underlying = Nullable.GetUnderlyingType(valueType) ?? valueType;
        if (underlying.IsEnum)
            builder.OptionsFromEnum(underlying);
        // A collection of enum members: Resolve() made it a multi-select, and the choices are the element
        // type's - as the generator gives them. Without them the field rendered a label over nothing.
        else if (BormFieldTypeResolver.GetEnumElementType(underlying) is { } elementEnum)
            builder.OptionsFromEnum(elementEnum);

        BormDataAnnotationsScanner.Apply(member, def);
        configure?.Invoke(builder);

        // Raw content has no built-in control: a file input produces an IBrowserFile, which cannot be
        // written into a byte[] or a Stream. Left to render, the field failed only then — an exception
        // in the middle of a page, on a server a dropped circuit. Here it fails where it was written.
        if (BormFieldTypeResolver.IsBinaryContent(valueType) && string.IsNullOrEmpty(def.CustomRenderer))
            throw new ArgumentException(
                $"'{path}' holds raw content ({valueType.Name}), which no built-in control can produce. " +
                "Give it a renderer that can — .CustomRenderer(\"key\") registered with " +
                "services.AddBorm(r => r.RegisterCustom(\"key\", typeof(MyComponent))) — or leave the property " +
                "out and bind an IBrowserFile instead, reading it in your submit handler.",
                nameof(selector));

        Add(def);
        return this;
    }

    /// <summary>
    /// Adds a nested object group whose children are configured against the child type, so their
    /// expressions and DataAnnotations work exactly as they do at the top level.
    /// </summary>
    public BormBuilder<TModel> Object<TChild>(
        Expression<Func<TModel, TChild>> selector,
        Action<BormBuilder<TChild>> children,
        Action<BormFieldBuilder>? configure = null)
        where TChild : class
    {
        var (path, member) = ResolvePath(selector);

        var def = new BormFieldDefinition(path, BormFieldType.Object)
        {
            ValueType = typeof(TChild),
            Label = BormFieldBuilder.Humanize(member.Name)
        };

        var childBuilder = new BormBuilder<TChild>();
        children(childBuilder);
        foreach (var c in childBuilder.Build().Fields) def.Children.Add(c);

        BormDataAnnotationsScanner.Apply(member, def);
        configure?.Invoke(new BormFieldBuilder(def));
        Add(def);
        return this;
    }

    /// <summary>
    /// Adds a repeating array whose item schema is configured against the element type. Use the
    /// <see cref="Field{TValue}"/> overloads inside <paramref name="item"/> to describe one element.
    /// </summary>
    public BormBuilder<TModel> Array<TItem>(
        Expression<Func<TModel, IEnumerable<TItem>?>> selector,
        Action<BormBuilder<TItem>> item,
        Action<BormFieldBuilder>? configure = null)
        where TItem : class
    {
        var (path, member) = ResolvePath(selector);

        var def = new BormFieldDefinition(path, BormFieldType.Array)
        {
            ValueType = member is PropertyInfo p ? p.PropertyType : typeof(IEnumerable<TItem>),
            Label = BormFieldBuilder.Humanize(member.Name)
        };

        var itemBuilder = new BormBuilder<TItem>();
        item(itemBuilder);
        var template = new BormFieldDefinition("item", BormFieldType.Object) { ValueType = typeof(TItem) };
        foreach (var c in itemBuilder.Build().Fields) template.Children.Add(c);
        def.ItemTemplate = template;

        BormDataAnnotationsScanner.Apply(member, def);
        configure?.Invoke(new BormFieldBuilder(def));
        Add(def);
        return this;
    }

    /// <summary>
    /// Adds a field whose value is derived from its owning object rather than typed by the user — an
    /// order total, a full name, a line total. The field is read-only by default; pass
    /// <paramref name="configure"/> to change that.
    /// </summary>
    /// <param name="selector">The field the computed value is written to.</param>
    /// <param name="compute">
    /// Receives the <typeparamref name="TModel"/> the selector starts from. At the top level that is the
    /// model; inside an <see cref="Array{TItem}"/> template it is the item, so the same expression works
    /// on a repeater row without knowing its index. A nested selector (<c>x =&gt; x.Summary.Total</c>)
    /// still receives the model, not the <c>Summary</c> the field sits in.
    /// </param>
    /// <param name="dependsOn">
    /// Paths whose changes trigger a recompute, named relative to the object that owns the field, or
    /// from the root. Leave null to recompute on any change.
    /// </param>
    /// <param name="configure">Further configuration for the generated field.</param>
    public BormBuilder<TModel> Computed<TValue>(
        Expression<Func<TModel, TValue>> selector,
        Func<TModel, TValue> compute,
        string[]? dependsOn = null,
        Action<BormFieldBuilder>? configure = null)
    {
        // The field is owned by the object its last member sits on, but the formula is written against
        // TModel, which sits one level above the selector's first member. Casting the field's own scope
        // was right only for x => x.Total: for x => x.Summary.Total the scope is the Summary, the cast
        // always failed, and default(TValue) was written in silence.
        var levels = BormPath.Parse(ResolvePath(selector).Path).Count;
        return Field(selector, f =>
        {
            f.ReadOnly()
             .Computed(ctx => ModelOf(ctx, levels) is TModel owner ? compute(owner) : default(TValue), dependsOn ?? []);
            configure?.Invoke(f);
        });
    }

    /// <summary>
    /// Adds a field whose value is an expression over the form — <c>{Quantity} * {UnitPrice}</c> on a
    /// repeater row, <c>sum({Lines[].LineTotal})</c> at the top. Read-only, with its dependencies taken
    /// from the expression; see <see cref="BormFieldBuilder.Computed(string)"/>.
    /// </summary>
    /// <exception cref="BormExpressionException"><paramref name="expression"/> is not a valid expression.</exception>
    public BormBuilder<TModel> Computed<TValue>(
        Expression<Func<TModel, TValue>> selector,
        string expression,
        Action<BormFieldBuilder>? configure = null)
        => Field(selector, f =>
        {
            f.Computed(expression);
            configure?.Invoke(f);
        });

    /// <summary>The object <paramref name="levels"/> containers above the computed field.</summary>
    private static object? ModelOf(BormComputedContext ctx, int levels)
    {
        var path = ctx.FieldPath;
        for (var i = 0; i < levels && path.Length > 0; i++) path = BormPath.Parent(path);
        return path.Length == 0 ? ctx.Data.Root : ctx.Data.GetValue(path);
    }

    /// <inheritdoc cref="BormBuilder.StepLayout" />
    public new BormBuilder<TModel> StepLayout(BormStepLayout layout) { base.StepLayout(layout); return this; }

    /// <summary>Adds a wizard step.</summary>
    public new BormBuilder<TModel> Step(string id, Action<BormStepBuilder> configure)
    {
        base.Step(id, configure);
        return this;
    }

    /// <summary>Adds a rule validating the whole model.</summary>
    public new BormBuilder<TModel> Validate(IBormValidationRule rule)
    {
        base.Validate(rule);
        return this;
    }

    /// <summary>
    /// Adds a cross-field rule over the whole model, e.g. "end date must be after start date".
    /// <paramref name="fieldPath"/> chooses which field shows the message.
    /// </summary>
    public BormBuilder<TModel> MustAll(Func<TModel, bool> predicate, string message, string? fieldPath = null)
        => Validate(new BormDelegateRule(ctx => ctx.Data.Root is not TModel model || predicate(model)
            ? BormRuleResult.Success()
            : fieldPath is null ? BormRuleResult.Fail(message) : BormRuleResult.FailFor(fieldPath, message)));

    /// <inheritdoc cref="BormBuilder.MustAll(string, string, string?)" />
    public new BormBuilder<TModel> MustAll(string expression, string message, string? fieldPath = null)
    {
        base.MustAll(expression, message, fieldPath);
        return this;
    }

    /// <summary>The path of a member, for use in conditions and step definitions: <c>Path(x =&gt; x.Address.City)</c>.</summary>
    public static string Path<TValue>(Expression<Func<TModel, TValue>> selector)
        => BormExpressionPath.Of(selector);

    private static (string Path, MemberInfo Member) ResolvePath<TValue>(Expression<Func<TModel, TValue>> selector)
        => BormExpressionPath.Resolve(selector);
}
