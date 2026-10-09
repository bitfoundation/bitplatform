using System.Collections;
using System.Globalization;
using System.Reflection;

namespace Bit.BlazorUI;

/// <summary>
/// How a params object writes what it supplies onto the component it cascades to.
/// </summary>
internal static class BitCascadeExtensions
{
    // What two cultures are compared by beyond their names. Whether a culture can still be changed is not part of how
    // it formats anything.
    private static readonly PropertyInfo[] _dateTimeFormatProperties = typeof(DateTimeFormatInfo).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                                                                 .Where(p => p.Name != nameof(DateTimeFormatInfo.IsReadOnly))
                                                                                                 .ToArray();

    private static readonly PropertyInfo[] _numberFormatProperties = typeof(NumberFormatInfo).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                                                             .Where(p => p.Name != nameof(NumberFormatInfo.IsReadOnly))
                                                                                             .ToArray();



    /// <summary>
    /// Supplies a parameter from the cascade, unless the markup has set it. BitComponentBase remembers the value it
    /// held before the cascade first supplied it, and puts it back once the cascade stops giving one.
    /// </summary>
    /// <remarks>
    /// This runs on every render of every component under a BitParams, so a value the cascade supplies again
    /// unchanged is no change: it is not written, and the class and style strings built from it the last time still
    /// hold, so they are only rebuilt when something the cascade gives actually moves. An object is the same only
    /// while it is the same instance and the params object handing it down is the same too: BitParams copies its
    /// params objects shallowly, so a list or a ClassStyles changed in place reaches the component as the very
    /// object it already holds, inside a new params object, and is written again then. Whatever is written runs
    /// the [CallOnSet] hook of the parameter and rebuilds the strings its [ResetClassBuilder] and
    /// [ResetStyleBuilder] name, exactly as when the markup changes it. The accessors are taken as static lambdas
    /// over the component's own type, so applying a cascade allocates nothing.
    /// </remarks>
    /// <returns>
    /// Whether the value was written and changed what the component held.
    /// </returns>
    public static bool TakeFromCascade<TComponent, T>(this TComponent component,
                                                      string name,
                                                      T value,
                                                      Func<TComponent, T> get,
                                                      Action<TComponent, T> set)
        where TComponent : BitComponentBase
    {
        if (component.HasNotBeenSet(name) is false) return false;

        var current = get(component);

        // A culture in another instance is compared by everything it formats with, as it holds it now, so only the
        // very instance the component already holds is one whose change in place cannot be told apart.
        var mayHaveChangedInPlace = component.IsCascadeRenewed
                                    && IsImmutable(value) is false
                                    && (value is not CultureInfo || ReferenceEquals(current, value));

        if (mayHaveChangedInPlace is false && AreSame(current, value)) return false;

        set(component, value);

        // A setter that normalizes what it is given - clamping it, or turning a value out of range into its
        // default - can leave the parameter exactly where it was, which is no change either.
        if (mayHaveChangedInPlace is false && AreSame(current, get(component))) return false;

        component.OnTakenFromCascade(name);

        return true;
    }

    /// <summary>
    /// Supplies a parameter from the cascade, unless the markup has set it or the component has set something that
    /// outranks it - in which case a value the cascade wrote on an earlier render is taken back
    /// (<see cref="ReleaseFromCascade"/>), since it would otherwise go on outranking the component's own choice.
    /// </summary>
    /// <remarks>
    /// This is how the parameters that make up one setting are cascaded. An icon is taken as an XIcon, an XIconName
    /// or an IconUrl, and the first of them that is set wins, so the cascade supplies none of them to a component that
    /// has set any one (<see cref="HasSetAnyOf"/>): a cascaded XIcon filled in beside the component's own XIconName
    /// would replace the icon the component asked for, and a cascaded XIconName beside its own XIcon would leave a
    /// value on it that says something it does not show.
    /// </remarks>
    /// <returns>
    /// Whether the value was written and changed what the component held.
    /// </returns>
    public static bool TakeFromCascade<TComponent, T>(this TComponent component,
                                                      string name,
                                                      T value,
                                                      Func<TComponent, T> get,
                                                      Action<TComponent, T> set,
                                                      bool outranked)
        where TComponent : BitComponentBase
    {
        if (outranked is false) return component.TakeFromCascade(name, value, get, set);

        component.ReleaseFromCascade(name);

        return false;
    }

    /// <summary>
    /// Whether the markup has set any of the named parameters: how a params object tells that a component has made
    /// its own choice of a setting it takes through several parameters, which outranks every one of them the cascade
    /// supplies.
    /// </summary>
    public static bool HasSetAnyOf(this BitComponentBase component, params ReadOnlySpan<string> names)
    {
        foreach (var name in names)
        {
            if (component.HasNotBeenSet(name) is false) return true;
        }

        return false;
    }

    /// <summary>
    /// Takes back a parameter the cascade supplies but the component must not take on this render, because a
    /// parameter the component set itself outranks it: puts back the value it held before the cascade wrote it,
    /// which would otherwise go on outranking the component's own choice for as long as the cascade supplies it.
    /// </summary>
    public static void ReleaseFromCascade(this BitComponentBase component, string name)
    {
        component.ReleaseCascadeParameter(name);
    }

    /// <summary>
    /// Whether a value read off a params object supplies its parameter: anything but null, except a string that is
    /// empty or white space, which every UpdateParameters skips with HasValue. What a params object writes and what
    /// BitComponentBase puts back once it stops writing it are decided by this one rule, so a string cleared to ""
    /// is let go of rather than left behind as the last value written.
    /// </summary>
    public static bool IsSupplied(object? value) => value is string text ? text.HasValue() : value is not null;

    /// <summary>
    /// Whether two values of a parameter are the same value, so that writing one over the other changes nothing.
    /// </summary>
    /// <remarks>
    /// CultureInfo.Equals compares the name and the CompareInfo alone, so two cultures that format dates and
    /// numbers differently - fa-IR with Persian month names and with Fingilish ones - compare equal. Two cultures are
    /// therefore the same only when they also format dates and numbers alike: an equal culture created afresh - one
    /// written inline, carried by a new copy of the params object - changes nothing, exactly as the markup treats it.
    /// </remarks>
    public static bool AreSame<T>(T first, T second)
    {
        if (first is CultureInfo || second is CultureInfo) return AreSameCulture(first as CultureInfo, second as CultureInfo);

        return EqualityComparer<T>.Default.Equals(first, second);
    }



    // A value that cannot be changed in place is the same value whenever it compares equal.
    private static bool IsImmutable<T>(T value) => typeof(T).IsValueType || value is null or string or Delegate;

    private static bool AreSameCulture(CultureInfo? first, CultureInfo? second)
    {
        if (ReferenceEquals(first, second)) return true;
        if (first is null || second is null) return false;
        if (first.Equals(second) is false) return false;

        return AreSameFormat(first.DateTimeFormat, second.DateTimeFormat, _dateTimeFormatProperties)
            && AreSameFormat(first.NumberFormat, second.NumberFormat, _numberFormatProperties);
    }

    private static bool AreSameFormat(object first, object second, PropertyInfo[] properties)
    {
        foreach (var property in properties)
        {
            var firstValue = property.GetValue(first);
            var secondValue = property.GetValue(second);

            // A calendar does not compare by value, and what it holds that formats anything is its kind and the
            // century it reads two-digit years in.
            if (firstValue is Calendar firstCalendar && secondValue is Calendar secondCalendar)
            {
                if (firstCalendar.GetType() != secondCalendar.GetType()) return false;
                if (firstCalendar.TwoDigitYearMax != secondCalendar.TwoDigitYearMax) return false;

                continue;
            }

            // The names of the days and the months, and the sizes of the digit groups, are arrays.
            if (StructuralComparisons.StructuralEqualityComparer.Equals(firstValue, secondValue) is false) return false;
        }

        return true;
    }
}
