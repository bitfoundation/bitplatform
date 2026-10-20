namespace Bit.Borm;

/// <summary>Read access to form data by path.</summary>
public interface IBormDataReader
{
    /// <summary>Reads the value at the given path, or null if absent.</summary>
    object? GetValue(string path);

    /// <summary>The underlying root object (a model instance or a dictionary).</summary>
    object? Root { get; }

    /// <summary>
    /// Reads the value at <paramref name="path"/>, reporting whether the path exists at all — which is
    /// a different question from whether it holds something. A field that is legitimately empty and a
    /// field that is not there both read as null, and code that has to choose between two candidate
    /// paths (see <see cref="BormScopedDataReader"/>) gets the wrong answer if it cannot tell
    /// them apart.
    /// </summary>
    /// <remarks>
    /// The default implementation cannot distinguish the two, so it treats a null as absent. Every
    /// accessor in the box overrides it; a custom reader should too if it can.
    /// </remarks>
    bool TryGetValue(string path, out object? value)
    {
        value = GetValue(path);
        return value is not null;
    }
}
