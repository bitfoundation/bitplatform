using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// How a params object writes what it supplies onto the component it cascades to.
/// </summary>
internal static class BitCascadeExtensions
{
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
        var mayHaveChangedInPlace = component.IsCascadeRenewed && IsImmutable(value) is false;

        if (mayHaveChangedInPlace is false && AreSame(current, value)) return false;

        set(component, value);

        // A setter that normalizes what it is given - clamping it, or turning a value out of range into its
        // default - can leave the parameter exactly where it was, which is no change either.
        if (mayHaveChangedInPlace is false && AreSame(current, get(component))) return false;

        component.OnTakenFromCascade(name);

        return true;
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
    /// Whether two values of a parameter are the same value, so that writing one over the other changes nothing.
    /// </summary>
    /// <remarks>
    /// CultureInfo.Equals compares the name and the CompareInfo alone, so two cultures that format dates and
    /// numbers differently - fa-IR with Persian month names and with Fingilish ones - compare equal. A culture is
    /// therefore the same only when it is the same object.
    /// </remarks>
    public static bool AreSame<T>(T first, T second)
    {
        if (first is CultureInfo || second is CultureInfo) return ReferenceEquals(first, second);

        return EqualityComparer<T>.Default.Equals(first, second);
    }



    // A value that cannot be changed in place is the same value whenever it compares equal.
    private static bool IsImmutable<T>(T value) => typeof(T).IsValueType || value is null or string or Delegate;
}
