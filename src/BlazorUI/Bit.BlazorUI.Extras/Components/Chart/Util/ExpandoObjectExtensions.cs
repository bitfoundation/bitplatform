using System.Dynamic;

namespace Bit.BlazorUI;

/// <summary>
/// This class contains extensions for the <see cref="ExpandoObject"/> type which allows for easier access and modification particularly when nested.
/// </summary>
internal static class ExpandoObjectExtensions
{
    /// <summary>
    /// Ensures that a specified path (e.g. "A.B.C") exists on the <see cref="ExpandoObject"/>. Any missing members will be filled with empty <see cref="ExpandoObject"/>s. 
    /// The <paramref name="value"/> is modified and also returned for convenience. If parts of the path already exist, the function attempts to add the missing members. 
    /// If a part of the path (excluding the very last part) doesn't support adding members (doesn't implement <c>IDictionary&lt;string, object&gt;</c>), an exception is thrown.
    /// </summary>
    /// <param name="value">The <see cref="ExpandoObject"/> to modify.</param>
    /// <param name="path">The path you want to ensure exists. This can be something like "A.B.C" which would be the same as accessing <c>value.A.B.C</c>.</param>
    /// <param name="allowForeignLastPart">If true, allows the last part of the path to not implement <c>IDictionary&lt;string, object&gt;</c> and be of a different type (e.g. string).
    /// If you just want to see if the path exists, leave it on true. If you want to make sure you can add members to the last part, set it to false.</param>
    /// <returns></returns>
    public static ExpandoObject EnsurePathExists(this ExpandoObject value, string path, bool allowForeignLastPart = true)
    {
        value ??= new ExpandoObject();

        if (string.IsNullOrWhiteSpace(path)) return value;

        string[] segments = path.Split('.');
        IDictionary<string, object> source = value;
        for (int i = 0; i < segments.Length; i++)
        {
            string segment = segments[i];
            IDictionary<string, object> newSource;
            if (source.TryGetValue(segment, out object val))
            {
                newSource = val as IDictionary<string, object>;
            }
            else
            {
                newSource = new ExpandoObject();
                source[segment] = newSource;
            }

            if (i < segments.Length - 1 || !allowForeignLastPart)
            {
                source = newSource ?? throw new Exception($"The object at path '{string.Join(".", segments.Take(i + 1))}' is already set to a value of type '{source[segment].GetType()}' which doesn't support adding new members.");
            }
        }

        return value;
    }

    /// <summary>
    /// Checks if a path exists without modifying the <paramref name="value"/>. Since the path has to be followed (enumerated)
    /// to check this, you can retrieve all the values using the out parameter <paramref name="values"/>.
    /// </summary>
    /// <param name="value">The <see cref="ExpandoObject"/> on which you want to check a path.</param>
    /// <param name="path">The path to check.</param>
    /// <param name="values">All the values from the path; same as using <c>value.EnumeratePath(path).ToArray()</c>.</param>
    /// <returns></returns>
    public static bool PathExists(this ExpandoObject value, string path, out object[] values)
    {
        values = value.EnumeratePath(path).ToArray();
        return path.Count(c => c == '.') + 1 == values.Length;
    }

    /// <summary>
    /// Checks if a path exists without modifying the <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The <see cref="ExpandoObject"/> on which you want to check a path.</param>
    /// <param name="path">The path to check.</param>
    /// <returns></returns>
    public static bool PathExists(this ExpandoObject value, string path) => PathExists(value, path, out _);

    /// <summary>
    /// Enumerates the objects in a path of the <see cref="ExpandoObject"/>. For a <paramref name="path"/> "a.b.c" the iteration
    /// would be <c>value.a</c>, <c>value.a.b</c>, <c>value.a.b.c</c> (given that all three values exist). If a part of the path doesn't exist
    /// or can't be accessed because the parent doesn't implement <c>IDictionary&lt;string, object&gt;</c>, the enumeration stops.
    /// </summary>
    /// <param name="value">The <see cref="ExpandoObject"/> whose values you want to iterate over.</param>
    /// <param name="path">The path you want to follow.</param>
    /// <returns></returns>
    public static IEnumerable<object> EnumeratePath(this ExpandoObject value, string path)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("The path cannot be null or whitespace.");

        string[] segments = path.Split('.');
        IDictionary<string, object> source = value;
        foreach (string segment in segments)
        {
            object x = null;
            try
            {
                x = source[segment];
            }
            catch (Exception e) when (e is KeyNotFoundException || e is NullReferenceException)
            {
                // x = null but that's already done
            }

            if (x is null) yield break;
            source = x as IDictionary<string, object>;

            yield return x;
        }
    }

    /// <summary>
    /// Enumerates the path and returns the last element. If the path can't be followed (entirely), an exception is thrown.
    /// </summary>
    /// <param name="value">The <see cref="ExpandoObject"/> whose value you want to retrieve.</param>
    /// <param name="path">The path of the value you want to get.</param>
    /// <returns></returns>
    public static object GetValue(this ExpandoObject value, string path)
    {
        if (!value.PathExists(path, out object[] values))
            throw new ArgumentException($"The path '{path}' could not be followed (entirely). {values.Length} parts of the path were followed.");

        return values[values.Length - 1];
    }

    /// <summary>
    /// Sets a value in a certain place (path) of the <see cref="ExpandoObject"/>. The <paramref name="expando"/> is modified and also returned for convenience.
    /// This method calls <see cref="EnsurePathExists(ExpandoObject, string, bool)"/> to ensure the path exists.
    /// </summary>
    /// <param name="expando">The <see cref="ExpandoObject"/> whose value you want to set.</param>
    /// <param name="path">The path leading to the value you want to set.</param>
    /// <param name="value">The actual value you want to set.</param>
    /// <returns></returns>
    public static ExpandoObject SetValue(this ExpandoObject expando, string path, object value)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("The path cannot be null or whitespace.");

        string[] segments = path.Split('.');
        IDictionary<string, object> source = expando.EnsurePathExists(path);
        for (int i = 0; i < segments.Length - 1; i++)
        {
            source = source[segments[i]] as IDictionary<string, object>;
        }

        source[segments[segments.Length - 1]] = value;

        return expando;
    }
}
