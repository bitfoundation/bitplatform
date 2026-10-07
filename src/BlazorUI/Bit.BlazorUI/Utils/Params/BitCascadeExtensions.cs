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
    /// hold, so they are only rebuilt when something the cascade gives actually moves. The accessors are taken as
    /// static lambdas over the component's own type, so applying a cascade allocates nothing.
    /// </remarks>
    /// <returns>
    /// Whether the value was written, which is when whatever the component derives from the parameter - what its
    /// [CallOnSet] hook does when the markup changes it - has to be brought up to date as well.
    /// </returns>
    public static bool TakeFromCascade<TComponent, T>(this TComponent component,
                                                      string name,
                                                      T value,
                                                      Func<TComponent, T> get,
                                                      Action<TComponent, T> set)
        where TComponent : BitComponentBase
    {
        if (component.HasNotBeenSet(name) is false) return false;

        if (EqualityComparer<T>.Default.Equals(get(component), value)) return false;

        set(component, value);

        component.ClassBuilder.Reset();
        component.StyleBuilder.Reset();

        return true;
    }
}
