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
    private static readonly ConcurrentDictionary<string, BitDataGridPropertyAccessor<TItem>> Cache = new();

    public string Path { get; }
    public Type PropertyType { get; }
    public Type UnderlyingType { get; }
    public bool CanWrite { get; }

    private readonly Func<TItem, object?> _getter;
    private readonly Action<TItem, object?>? _setter;

    private BitDataGridPropertyAccessor(string path, Type propertyType, bool canWrite,
        Func<TItem, object?> getter, Action<TItem, object?>? setter)
    {
        Path = path;
        PropertyType = propertyType;
        UnderlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        CanWrite = canWrite;
        _getter = getter;
        _setter = setter;
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
        if (value is null)
        {
            result = DefaultValue();
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
                result = Convert.ChangeType(value, target);
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

        foreach (var segment in path.Split('.'))
        {
            if (string.IsNullOrWhiteSpace(segment))
                throw new ArgumentException($"Property path '{path}' contains an empty or whitespace segment.", nameof(path));

            // If the owner of this segment is an intermediate (nullable) value, guard against it being null.
            if (!ReferenceEquals(body, param) && CanBeNull(body.Type))
            {
                var isNull = Expression.Equal(body, Expression.Constant(null, body.Type));
                nullGuard = nullGuard is null ? isNull : Expression.OrElse(nullGuard, isNull);
            }

            var prop = body.Type.GetProperty(segment,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                ?? throw new ArgumentException($"Property '{segment}' not found on type '{body.Type.Name}'.");
            body = Expression.Property(body, prop);
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
        var canWrite = lastProp is { CanWrite: true };
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

        return new BitDataGridPropertyAccessor<TItem>(path, propertyType, canWrite, getter, setter);
    }

    private static bool CanBeNull(Type type)
        => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
}
