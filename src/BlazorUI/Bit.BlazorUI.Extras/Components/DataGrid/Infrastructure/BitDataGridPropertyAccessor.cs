using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace Bit.BlazorUI;

/// <summary>
/// Builds and caches fast compiled delegates to read and write a property on
/// <typeparamref name="TItem"/> by name, supporting nested paths like "Address.City".
/// </summary>
public sealed class BitDataGridPropertyAccessor<TItem>
{
    // Build() resolves members with BindingFlags.IgnoreCase, so logically identical paths that differ only
    // in casing ("Address.City" vs "address.city") map to the same member. Key the cache case-insensitively
    // so those paths share a single compiled accessor instead of creating duplicate entries.
    private static readonly ConcurrentDictionary<string, BitDataGridPropertyAccessor<TItem>> Cache = new(StringComparer.OrdinalIgnoreCase);

    public string Path { get; }
    public Type PropertyType { get; }
    public Type UnderlyingType { get; }
    public bool CanWrite { get; }

    /// <summary>
    /// The typed property-access lambda (<c>x =&gt; x.A.B</c>) without the compiled getter's object
    /// boxing. Used to translate filters/sorts into expression trees an <see cref="IQueryable{T}"/>
    /// provider (e.g. EF Core) can execute remotely. Nested paths carry the getter's null handling
    /// (a conditional yielding null when an intermediate is null, lifting the leaf to its nullable
    /// form) so in-memory queryables don't throw on rows with a null intermediate; relational
    /// providers translate the conditional to equivalent CASE/NULL semantics.
    /// </summary>
    public LambdaExpression PropertyLambda { get; }

    private readonly Func<TItem, object?> _getter;
    private readonly Action<TItem, object?>? _setter;

    private BitDataGridPropertyAccessor(string path, Type propertyType, bool canWrite,
        Func<TItem, object?> getter, Action<TItem, object?>? setter, LambdaExpression propertyLambda)
    {
        Path = path;
        PropertyType = propertyType;
        UnderlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        CanWrite = canWrite;
        _getter = getter;
        _setter = setter;
        PropertyLambda = propertyLambda;
    }

    public object? GetValue(TItem item) => _getter(item);

    public void SetValue(TItem item, object? value)
    {
        if (_setter is null) return;
        // Only write when the value can actually be coerced to the property's type. Silently
        // substituting the type's default (e.g. 0) for unparseable input would discard the user's
        // entry without any feedback, so reject the conversion failure instead.
        if (TryConvertValue(value, out var converted))
            _setter(item, converted);
    }

    /// <summary>
    /// Coerces an arbitrary value into the property's type, falling back to the type's default on failure.
    /// The name makes the silent-default behavior explicit; prefer <see cref="TryConvertValue"/> when a
    /// conversion failure must be detected rather than masked.
    /// </summary>
    public object? ConvertOrDefault(object? value)
        => TryConvertValue(value, out var result) ? result : DefaultValue();

    /// <summary>
    /// Attempts to coerce an arbitrary value into the property's type. Returns <c>false</c> when the
    /// value cannot be converted, letting callers reject invalid input rather than overwrite it.
    /// </summary>
    public bool TryConvertValue(object? value, out object? result)
    {
        // A cleared edit can arrive as an empty string (e.g. a select/text editor reset to "") rather
        // than null. Normalize it to null up front so the nullable-target handling below clears the
        // value - but only for non-string targets. For a string-typed property, "" is a legitimate
        // user edit (an intentionally emptied text cell) and must be preserved rather than nulled.
        if (value is string es && es.Length == 0 && PropertyType != typeof(string))
            value = null;

        if (value is null)
        {
            // A cleared edit (null) must not silently become the type's default (e.g. 0 / MinValue for
            // a non-nullable value type), which would discard the user's intent. Only let null through
            // for nullable value types and reference types; reject it for non-nullable value targets.
            if (PropertyType.IsValueType && Nullable.GetUnderlyingType(PropertyType) is null)
            {
                result = null;
                return false;
            }

            result = null;
            return true;
        }

        if (PropertyType.IsInstanceOfType(value))
        {
            result = value;
            return true;
        }

        var target = UnderlyingType;
        try
        {
            if (target.IsEnum)
                result = value is string s ? Enum.Parse(target, s, true) : Enum.ToObject(target, value);
            else if (target == typeof(Guid))
                result = value is Guid g ? g : Guid.Parse(value.ToString()!);
            else if (target == typeof(DateOnly))
                result = value is DateOnly d ? d : DateOnly.Parse(value.ToString()!, CultureInfo.InvariantCulture);
            else if (target == typeof(TimeOnly))
                result = value is TimeOnly t ? t : TimeOnly.Parse(value.ToString()!, CultureInfo.InvariantCulture);
            else if (target == typeof(DateTimeOffset))
                // DateTimeOffset is not IConvertible, so Convert.ChangeType below would throw for it;
                // handle it explicitly like the other date/time types above. Parse with the invariant
                // culture to match the ISO 8601 string the editors emit, so conversion is locale-stable.
                result = value is DateTimeOffset dto ? dto : DateTimeOffset.Parse(value.ToString()!, CultureInfo.InvariantCulture);
            else
                // Parse with the invariant culture so editor values are coerced consistently
                // regardless of the current thread culture (e.g. "1.5" must not be misread in a
                // comma-decimal locale where the editor still emits an invariant numeric string).
                result = Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    private object? DefaultValue()
        => PropertyType.IsValueType && Nullable.GetUnderlyingType(PropertyType) is null
            ? Activator.CreateInstance(PropertyType)
            : null;

    /// <summary>
    /// Extracts the property path ("Address.City") from a typed selector expression like
    /// <c>x =&gt; x.Address.City</c>, so a lambda-based column definition can reuse the same
    /// path-keyed accessor cache (and state/export identity) as a string-based one.
    /// Only simple property-access chains are supported; anything else (method calls, indexers,
    /// arithmetic, field access) throws so the invalid selector fails loudly at definition time.
    /// </summary>
    public static string ResolvePath(Expression<Func<TItem, object?>> property)
    {
        Expression node = property.Body;

        // The object-typed lambda makes the compiler wrap value-type (and sometimes reference-type)
        // members in a boxing/upcast Convert node; peel those to reach the member chain.
        while (node is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
            node = unary.Operand;

        var segments = new List<string>();
        while (node is MemberExpression member && member.Expression is not null)
        {
            // ".Value" on a nullable struct intermediate (x => x.Price.Value.Amount) is Nullable<T>
            // plumbing, not a data property - Build() inserts the unwrap itself, so drop the segment
            // to arrive at the same cache key as the equivalent "Price.Amount" string path.
            var isNullableValue = member.Member.Name == nameof(Nullable<int>.Value)
                && Nullable.GetUnderlyingType(member.Expression.Type) is not null;

            if (!isNullableValue)
            {
                if (member.Member is not PropertyInfo)
                    throw new ArgumentException($"The property selector '{property}' is not supported: '{member.Member.Name}' is not a property. Only property-access chains like 'x => x.Property' or 'x => x.Nested.Property' can be used.", nameof(property));
                segments.Add(member.Member.Name);
            }

            node = member.Expression;
        }

        if (node is not ParameterExpression || segments.Count == 0)
            throw new ArgumentException($"The property selector '{property}' is not supported. Only property-access chains like 'x => x.Property' or 'x => x.Nested.Property' can be used.", nameof(property));

        segments.Reverse();
        return string.Join('.', segments);
    }

    public static BitDataGridPropertyAccessor<TItem> For(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Property path must not be null, empty or whitespace.", nameof(path));

        return Cache.GetOrAdd(path, Build);
    }

    private static BitDataGridPropertyAccessor<TItem> Build(string path)
    {
        var param = Expression.Parameter(typeof(TItem), "x");
        Expression body = param;
        PropertyInfo? lastProp = null;
        Expression? nullGuard = null;
        // Tracks whether the path is written through a value-type (struct) intermediate. A property
        // getter returns a *copy* of a struct, so Expression.Assign on e.g. "Address.City" (where
        // Address is a struct) would mutate that throwaway copy and never write back to the item.
        // We detect this and keep such paths read-only rather than compile a silently broken setter.
        var crossesValueTypeIntermediate = false;

        foreach (var segment in path.Split('.'))
        {
            if (string.IsNullOrWhiteSpace(segment))
                throw new ArgumentException($"Property path '{path}' contains an empty or whitespace segment.", nameof(path));

            // The owner of this segment is the previous body. If that owner is a value type (and not the
            // root parameter), assigning to a member off it cannot write back through the parent.
            if (!ReferenceEquals(body, param) && body.Type.IsValueType)
            {
                crossesValueTypeIntermediate = true;
            }

            // If the owner of this segment is an intermediate (nullable) value, guard against it being null.
            if (!ReferenceEquals(body, param) && CanBeNull(body.Type))
            {
                var isNull = Expression.Equal(body, Expression.Constant(null, body.Type));
                nullGuard = nullGuard is null ? isNull : Expression.OrElse(nullGuard, isNull);
            }

            // A nullable value-type intermediate (e.g. Money?) exposes the next segment on its underlying
            // value type, not on Nullable<T> itself, so unwrap via .Value before resolving the property;
            // otherwise GetProperty would look on Nullable<T> (which only has Value/HasValue) and fail for
            // a nested path like "Price.Amount" where Price is Money?. The null guard added above already
            // protects the getter/setter from dereferencing a null nullable here.
            var owner = Nullable.GetUnderlyingType(body.Type) is not null
                ? Expression.Property(body, "Value")
                : body;

            var prop = owner.Type.GetProperty(segment,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                ?? throw new ArgumentException($"Property '{segment}' not found on type '{owner.Type.Name}'.");
            body = Expression.Property(owner, prop);
            lastProp = prop;
        }

        var propertyType = body.Type;

        // Getter: x => (object)x.Path, returning null early if any intermediate property is null.
        Expression getterBody = Expression.Convert(body, typeof(object));
        if (nullGuard is not null)
        {
            getterBody = Expression.Condition(nullGuard, Expression.Constant(null, typeof(object)), getterBody);
        }
        var getter = Expression.Lambda<Func<TItem, object?>>(getterBody, param).Compile();

        // Setter (only for a simple, writable, single-level-or-nested property)
        Action<TItem, object?>? setter = null;
        // A path crossing a struct intermediate cannot be written back through the value-type copy, so
        // leave it read-only rather than emit a setter that compiles but silently drops writes. The same
        // applies when the row root TItem is itself a value type: SetValue(TItem item, ...) receives a
        // by-value copy, so any assignment would mutate that copy and never reach the caller's row. Keep
        // struct-root accessors read-only unless the write path is changed to use by-reference updates.
        var canWrite = lastProp is { CanWrite: true } && !crossesValueTypeIntermediate && !typeof(TItem).IsValueType;
        if (canWrite)
        {
            var valueParam = Expression.Parameter(typeof(object), "v");
            var convertedValue = Expression.Convert(valueParam, propertyType);
            Expression assign = Expression.Assign(body, convertedValue);
            // Mirror the getter's null handling: if any intermediate property in the chain is null,
            // skip the assignment instead of throwing a NullReferenceException.
            if (nullGuard is not null)
            {
                assign = Expression.IfThen(Expression.Not(nullGuard), assign);
            }
            setter = Expression.Lambda<Action<TItem, object?>>(assign, param, valueParam).Compile();
        }

        // The queryable-translation lambda mirrors the getter's null handling for nested paths: a
        // null intermediate yields null instead of dereferencing, so sorting/filtering an in-memory
        // IQueryable (LINQ to Objects evaluates member chains eagerly, unlike SQL) can't throw a
        // NullReferenceException. A non-nullable value-type leaf is lifted to its nullable form so
        // the conditional's null branch is representable; single-segment paths stay untouched.
        Expression lambdaBody = body;
        if (nullGuard is not null)
        {
            var liftedType = CanBeNull(propertyType) ? propertyType : typeof(Nullable<>).MakeGenericType(propertyType);
            lambdaBody = Expression.Condition(
                nullGuard,
                Expression.Constant(null, liftedType),
                liftedType == propertyType ? body : Expression.Convert(body, liftedType));
        }

        return new BitDataGridPropertyAccessor<TItem>(path, propertyType, canWrite, getter, setter,
            Expression.Lambda(lambdaBody, param));
    }

    private static bool CanBeNull(Type type)
        => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
}
