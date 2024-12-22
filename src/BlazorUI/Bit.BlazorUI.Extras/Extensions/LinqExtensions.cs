namespace Bit.BlazorUI;

/// <summary>
/// https://www.codeproject.com/tips/375967/flatten-a-hierarchical-collection-of-objects-with
/// </summary>
public static class LinqExtensions
{
    /// <summary>
    ///   This method extends the LINQ methods to flatten a collection of 
    ///   items that have a property of children of the same type.
    /// </summary>
    /// <typeparam name = "T">Item type.</typeparam>
    /// <param name = "source">Source collection.</param>
    /// <param name = "childPropertySelector">
    ///   Child property selector delegate of each item. 
    ///   IEnumerable'T' childPropertySelector(T itemBeingFlattened)
    /// </param>
    /// <returns>Returns a one level list of elements of type T.</returns>
    public static IEnumerable<T> Flatten<T>(this IEnumerable<T> source,
                                                 Func<T, IEnumerable<T>> childPropertySelector)
    {
        return source
            .Flatten((itemBeingFlattened, objectsBeingFlattened) =>
                     childPropertySelector(itemBeingFlattened));
    }

    /// <summary>
    ///   This method extends the LINQ methods to flatten a collection of 
    ///   items that have a property of children of the same type.
    /// </summary>
    /// <typeparam name = "T">Item type.</typeparam>
    /// <param name = "source">Source collection.</param>
    /// <param name = "childPropertySelector">
    ///   Child property selector delegate of each item. 
    ///   IEnumerable'T' childPropertySelector
    ///   (T itemBeingFlattened, IEnumerable'T' objectsBeingFlattened)
    /// </param>
    /// <returns>Returns a one level list of elements of type T.</returns>
    public static IEnumerable<T> Flatten<T>(this IEnumerable<T> source,
                                                 Func<T, IEnumerable<T>, IEnumerable<T>> childPropertySelector)
    {
        return source
            .Concat(source
                        .Where(item => childPropertySelector(item, source) != null)
                        .SelectMany(itemBeingFlattened =>
                                    childPropertySelector(itemBeingFlattened, source)
                                        .Flatten(childPropertySelector)));
    }
}
